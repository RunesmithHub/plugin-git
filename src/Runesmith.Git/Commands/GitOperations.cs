using System.Composition;
using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia.Threading;
using HammerUI.Services;
using Runesmith.Git.Git;
using Runesmith.Git.Repositories;
using Runesmith.Git.Views.History;
using Runesmith.Sdk.Documents;
using Runesmith.Sdk.Settings;
using Runesmith.Sdk.Shell;
using Runesmith.Sdk.Tasks;
using Runesmith.Sdk.ToolWindows;
using Runesmith.Sdk.Workspace;

namespace Runesmith.Git.Commands;

/// <summary>The Git actions as the user sees them: each one runs Git, shows long ones as background tasks with Cancel, asks before anything
/// that loses work, offers a way out of the failures it knows, and ends with a toast.</summary>
[Export]
[Shared]
internal sealed partial class GitOperations : IDisposable
{
    /// <summary>The id of the Commit tool window.</summary>
    public const string CommitWindowId = "git.commit";

    /// <summary>The id of the Git tool window with the log.</summary>
    public const string LogWindowId = "git.log";

    private readonly RepositoryService repositories;
    private readonly IWorkspace workspace;
    private readonly IBackgroundTasks tasks;
    private readonly INotificationService notifications;
    private readonly ISettingsService settings;
    private readonly Lazy<IToolWindowManager> toolWindows;
    private readonly IEditorService editors;
    private readonly HistoryNavigator history;
    private readonly IDialogService? dialogs;
    private readonly IQuickInputService? quickInput;
    private readonly IDiffService? diffs;
    private readonly IOutputChannel? log;
    private readonly Timer autoFetch;
    private int busy;

    [ImportingConstructor]
    public GitOperations(RepositoryService repositories, IWorkspace workspace, IBackgroundTasks tasks, INotificationService notifications, ISettingsService settings,
        Lazy<IToolWindowManager> toolWindows, IEditorService editors, HistoryNavigator history, [Import(AllowDefault = true)] IOutputService? output,
        [Import(AllowDefault = true)] IDialogService? dialogs, [Import(AllowDefault = true)] IDiffService? diffs,
        [Import(AllowDefault = true)] IQuickInputService? quickInput)
    {
        this.quickInput = quickInput;
        this.repositories = repositories;
        this.workspace = workspace;
        this.tasks = tasks;
        this.notifications = notifications;
        this.settings = settings;
        this.toolWindows = toolWindows;
        this.editors = editors;
        this.history = history;
        this.dialogs = dialogs;
        this.diffs = diffs;
        log = output?.GetChannel("Git");
        repositories.CommandRunning += (_, arguments) => log?.AppendLine("git " + string.Join(' ', arguments.Select(Quote)));
        autoFetch = new Timer(_ => Dispatcher.UIThread.Post(() => _ = FetchAsync(quiet: true)), null, Timeout.Infinite, Timeout.Infinite);
        settings.Changed += (_, e) =>
        {
            if (e.Key == GitSettings.AutoFetchMinutes)
                ScheduleAutoFetch();
        };
        ScheduleAutoFetch();
    }

    /// <summary>Gets the repository service the actions refresh after they run.</summary>
    public RepositoryService Repositories => repositories;

    /// <summary>Gets whether a fetch, pull or push runs now.</summary>
    public bool IsSyncing => Volatile.Read(ref busy) > 0;

    /// <summary>Raised on the UI thread when <see cref="IsSyncing"/> changes.</summary>
    public event EventHandler? SyncingChanged;

    private GitRepository? Repository => repositories.Repository;

    private string? CurrentBranch => repositories.Current?.Branch;

    /// <summary>Shows the Commit tool window and puts the keyboard focus in the message.</summary>
    public event EventHandler? FocusMessageRequested;

    /// <summary>Shows the Commit tool window with the message box focused.</summary>
    public void ShowCommitWindow()
    {
        toolWindows.Value.Show(CommitWindowId);
        Dispatcher.UIThread.Post(() => FocusMessageRequested?.Invoke(this, EventArgs.Empty), DispatcherPriority.Background);
    }

    /// <summary>Shows the Git tool window.</summary>
    public void ShowLog() => toolWindows.Value.Show(LogWindowId);

    /// <summary>Shows the Git tool window filtered, such as to a file's history.</summary>
    public void ShowHistory(HistoryRequest request)
    {
        toolWindows.Value.Show(LogWindowId);
        history.Show(request);
    }

    /// <summary>Makes the open folder a Git repository.</summary>
    public async Task InitializeAsync()
    {
        if (workspace.RootPath is not { } root)
            return;

        try
        {
            await GitRepository.InitAsync(root, CancellationToken.None);
            await repositories.ReopenAsync();
            notifications.Notify(NotificationKind.Success, Strings.InitDone, Strings.InitDoneMessage(Path.GetFileName(root)),
                Strings.ShowChanges, ShowCommitWindow);
        }
        catch (GitException exception)
        {
            notifications.Notify(NotificationKind.Error, Strings.InitFailed, exception.Message);
        }
    }

    /// <summary>Fetches every remote, as a background task; a quiet fetch, such as the automatic one, says nothing unless it fails.</summary>
    public async Task FetchAsync(bool quiet = false)
    {
        if (Repository is not { } repository || (quiet && IsSyncing))
            return;
        if (repositories.Current?.Remotes.Count == 0)
        {
            if (!quiet)
                NoRemote(Strings.NoRemoteFetch);
            return;
        }

        var succeeded = await RunAsync(Strings.TaskFetching, (progress, token) => repository.FetchAsync(token, progress), quiet: quiet);
        if (!succeeded || quiet)
            return;

        var info = repositories.Current;
        if (info is { Behind: > 0, Branch: { } branch, Upstream: { } upstream })
            notifications.Notify(NotificationKind.Info, Strings.FetchBehind(info.Behind), Strings.FetchBehindMessage(count: info.Behind, upstream: upstream, branch: branch),
                Strings.UpdateProject, () => _ = PullAsync());
        else
            notifications.Notify(NotificationKind.Success, Strings.FetchDone, info?.Upstream is { } tracked ? Strings.FetchDoneUpToDate(branch: info.Branch ?? "HEAD", upstream: tracked) : Strings.FetchDoneAll);
    }

    /// <summary>Pulls the current branch's upstream with the pull strategy setting, keeping local changes.</summary>
    public async Task<bool> PullAsync()
    {
        if (Repository is not { } repository || repositories.Current is not { } info)
            return false;
        if (info.Branch is not { } branch)
        {
            notifications.Notify(NotificationKind.Warning, Strings.HeadDetached, Strings.HeadDetachedPull);
            return false;
        }

        if (info.Upstream is not { } upstream)
        {
            if (info.Remotes.Count == 0)
                NoRemote(Strings.NoRemotePull);
            else
                notifications.Notify(NotificationKind.Warning, Strings.NoUpstream(branch), Strings.NoUpstreamMessage, Strings.CommandPush, () => _ = ShowPushAsync());
            return false;
        }

        var before = info.Head;
        var strategy = GitSettings.ParsePullStrategy(settings.Get<string>(GitSettings.PullStrategy));
        var succeeded = await RunAsync(Strings.TaskUpdating(branch), (progress, token) => repository.PullAsync(strategy, token, progress), handle: PullFailedAsync);
        if (!succeeded)
            return false;

        var after = repositories.Current?.Head;
        var pulled = before is null || after is null || before == after ? 0 : await CountAsync(repository, $"{before}..{after}");
        notifications.Notify(NotificationKind.Success, pulled > 0 ? Strings.PullDone(branch) : Strings.PullUpToDate(branch),
            pulled > 0 ? Strings.PullDoneMessage(count: pulled, upstream: upstream) : Strings.PullUpToDateMessage(upstream));
        return true;
    }

    /// <summary>Shows the Push dialog with the commits to push and where they go, or pushes at once without a dialog service.</summary>
    public async Task ShowPushAsync()
    {
        if (Repository is not { } repository || repositories.Current is not { } info)
            return;
        if (info.Branch is not { } branch)
        {
            notifications.Notify(NotificationKind.Warning, Strings.HeadDetached, Strings.HeadDetachedPush);
            return;
        }

        var target = await repository.GetPushTargetAsync(branch, CancellationToken.None);
        if (target is null)
        {
            NoRemote(Strings.NoRemotePush);
            return;
        }

        if (dialogs is null)
        {
            await PushAsync();
            return;
        }

        var outgoing = await repository.GetLogAsync(new LogQuery { Revision = target.Value.HasUpstream ? $"{target.Value.Remote}/{target.Value.Branch}..HEAD" : "HEAD --not --remotes", Count = 100 },
            CancellationToken.None);
        var request = await Views.Branches.PushDialog.ShowAsync(dialogs, branch, info.Remotes.Select(r => r.Name).ToList(), target.Value.Remote, target.Value.Branch, outgoing,
            target.Value.HasUpstream, target.Value.HasUpstream ? info.Behind : 0);
        if (request is null)
            return;
        if (request.Force && !await notifications.ConfirmAsync(Strings.ForcePushTitle,
                Strings.ForcePushMessage(destination: $"{request.Remote}/{request.Branch}", branch: branch),
                Strings.ForcePush, isDestructive: true))
            return;

        await PushAsync(request.Remote, request.Branch, request.Force);
    }

    /// <summary>Pushes the current branch to where it pushes, setting the upstream on its first push.</summary>
    public async Task<bool> PushAsync(string? remote = null, string? remoteBranch = null, bool force = false)
    {
        if (Repository is not { } repository || repositories.Current is not { Branch: { } branch } info)
            return false;

        var target = await repository.GetPushTargetAsync(branch, CancellationToken.None);
        if (target is null)
        {
            NoRemote(Strings.NoRemotePush);
            return false;
        }

        remote ??= target.Value.Remote;
        remoteBranch ??= target.Value.Branch;
        var isUpstream = target.Value.HasUpstream && remote == target.Value.Remote && remoteBranch == target.Value.Branch;
        var count = isUpstream ? info.Ahead : -1;
        var destination = $"{remote}/{remoteBranch}";
        var succeeded = await RunAsync(Strings.TaskPushing(branch: branch, destination: destination),
            (progress, token) => repository.PushAsync(branch, remote, remoteBranch, setUpstream: !target.Value.HasUpstream, force, token, progress),
            handle: exception => PushFailedAsync(exception, destination, branch));
        if (succeeded)
            notifications.Notify(NotificationKind.Success, count > 0 ? Strings.PushDoneCommits(count: count, destination: destination) : Strings.PushDone(branch: branch, destination: destination),
                target.Value.HasUpstream ? null : Strings.PushTracks(branch: branch, destination: destination));
        return succeeded;
    }

    /// <summary>Commits the staged changes and pushes when asked; throws <see cref="GitException"/> for the Commit window to show.</summary>
    public async Task CommitAsync(string message, CommitOptions options, bool push)
    {
        if (Repository is not { } repository)
            return;

        var files = repositories.Status?.Staged.Count() ?? 0;
        var sha = await repository.CommitAsync(message, options, CancellationToken.None);
        await repositories.RefreshAsync();
        var subject = message.Split('\n')[0];
        if (!push)
        {
            notifications.Notify(NotificationKind.Success, options.Amend ? Strings.CommitAmended : Strings.CommitDone(files),
                $"{sha[..8]} {subject}");
            return;
        }

        await PushAsync(force: options.Amend && repositories.Current is { Ahead: > 0, Behind: > 0 } && await ConfirmAmendPushAsync());
    }

    /// <summary>Checks out a branch or tag; when local changes would be overwritten, offers a smart checkout or a forced one.</summary>
    public async Task CheckoutAsync(GitRef target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (Repository is not { } repository)
            return;

        if (target.Kind == RefKind.RemoteBranch)
        {
            var refs = await repository.GetRefsAsync(CancellationToken.None);
            if (refs.FirstOrDefault(r => r.Kind == RefKind.LocalBranch && r.Name == target.BranchName) is { } local)
                target = local;
        }

        var name = target.Kind == RefKind.RemoteBranch ? target.BranchName : target.Name;
        await CheckoutCoreAsync(name, (force, progress, token) => repository.CheckoutAsync(target, force, token, progress));
    }

    /// <summary>Checks out a commit with a detached HEAD.</summary>
    public async Task CheckoutCommitAsync(GitCommit commit)
    {
        ArgumentNullException.ThrowIfNull(commit);
        if (Repository is { } repository)
            await CheckoutCoreAsync(commit.ShortSha, (force, _, token) => repository.CheckoutCommitAsync(commit.Sha, force, token));
    }

    /// <summary>Asks for a name and creates a branch at a start point, HEAD when null, checking it out when asked.</summary>
    public async Task NewBranchAsync(string? startPoint = null, string? startLabel = null, string initialName = "")
    {
        if (Repository is not { } repository || dialogs is null)
            return;

        var refs = await repository.GetRefsAsync(CancellationToken.None);
        var answer = await GitDialogs.AskNameAsync(dialogs, Strings.NewBranch, Strings.NewBranchFrom(startLabel ?? CurrentBranch ?? "HEAD"), Strings.NameLabel, initialName,
            name => ValidateNewBranchAsync(repository, refs, name), Strings.Create, Strings.NewBranchCheckout);
        if (answer is null)
            return;

        await RunQuickAsync(Strings.CreateBranchFailed(answer.Name), () => repository.CreateBranchAsync(answer.Name, startPoint, answer.Option, CancellationToken.None));
    }

    /// <summary>Asks for a new name for a local branch and renames it.</summary>
    public async Task RenameBranchAsync(GitRef branch)
    {
        ArgumentNullException.ThrowIfNull(branch);
        if (Repository is not { } repository || quickInput is null)
            return;

        var refs = await repository.GetRefsAsync(CancellationToken.None);
        var answer = await quickInput.InputAsync(new InputBoxOptions
        {
            Title = Strings.RenameBranchTitle(branch.Name),
            Placeholder = Strings.RenameBranchPlaceholder,
            Value = branch.Name,
            Validate = (name, _) => name.Trim() == branch.Name ? Task.FromResult<string?>(Strings.RenameSame) : ValidateNewBranchAsync(repository, refs, name.Trim()),
        });
        if (answer?.Trim() is { Length: > 0 } renamed)
            await RunQuickAsync(Strings.RenameBranchFailed(branch.Name), () => repository.RenameBranchAsync(branch.Name, renamed, CancellationToken.None));
    }

    /// <summary>Lets the user pick a branch or a tag from a list, and checks it out.</summary>
    public async Task PickCheckoutAsync()
    {
        if (Repository is not { } repository || quickInput is null)
            return;

        var refs = await repository.GetRefsAsync(CancellationToken.None);
        var current = CurrentBranch;
        var items = new List<PickItem<GitRef?>>();
        void AddGroup(RefKind kind, string title, string icon)
        {
            var group = refs.Where(r => r.Kind == kind && !(kind == RefKind.LocalBranch && r.Name == current) && !r.Name.EndsWith("/HEAD", StringComparison.Ordinal)).ToList();
            if (group.Count == 0)
                return;

            items.Add(PickItem.Separator<GitRef?>(title));
            items.AddRange(group.Select(r => new PickItem<GitRef?>(r.Name, r) { Icon = icon, Description = kind == RefKind.LocalBranch ? r.Upstream : null }));
        }

        AddGroup(RefKind.LocalBranch, Strings.CheckoutLocal, GitIcons.Branch);
        AddGroup(RefKind.RemoteBranch, Strings.CheckoutRemote, GitIcons.Branch);
        AddGroup(RefKind.Tag, Strings.CheckoutTags, "tag");
        var picked = await quickInput.PickAsync(items, new QuickPickOptions
        {
            Title = current is null ? Strings.CheckoutTitle : Strings.CheckoutTitleOn(current),
            Placeholder = Strings.CheckoutPlaceholder,
            MatchOnDescription = true,
            EmptyText = Strings.CheckoutEmpty,
        });
        if (picked?.Value is { } target)
            await CheckoutAsync(target);
    }

    /// <summary>Deletes a branch after asking; a local one that is not fully merged needs a second answer, and can be restored from the toast.</summary>
    public async Task DeleteBranchAsync(GitRef branch)
    {
        ArgumentNullException.ThrowIfNull(branch);
        if (Repository is not { } repository)
            return;

        if (branch.Kind == RefKind.RemoteBranch)
        {
            if (!await notifications.ConfirmAsync(Strings.DeleteBranchTitle(branch.Name), Strings.DeleteRemoteBranchMessage(branch: branch.BranchName, remote: branch.Remote ?? ""), Strings.Delete, isDestructive: true))
                return;
            if (await RunAsync(Strings.TaskDeleting(branch.Name), (progress, token) => repository.DeleteRemoteBranchAsync(branch.Remote!, branch.BranchName, token, progress)))
                notifications.Notify(NotificationKind.Success, Strings.DeleteBranchDone(branch.Name));
            return;
        }

        if (branch.IsHead)
        {
            notifications.Notify(NotificationKind.Warning, Strings.DeleteCheckedOut(branch.Name), Strings.DeleteCheckedOutMessage);
            return;
        }

        if (!await notifications.ConfirmAsync(Strings.DeleteBranchTitle(branch.Name), Strings.DeleteLocalBranchMessage(branch.Name), Strings.Delete, isDestructive: true))
            return;

        try
        {
            await repository.DeleteBranchAsync(branch.Name, force: false, CancellationToken.None);
        }
        catch (GitException exception) when (exception.Kind == GitErrorKind.BranchNotFullyMerged)
        {
            if (!await notifications.ConfirmAsync(Strings.DeleteUnmerged(branch.Name),
                    Strings.DeleteUnmergedMessage, Strings.DeleteAnyway, isDestructive: true))
                return;
            if (!await RunQuickAsync(Strings.DeleteBranchFailed(branch.Name), () => repository.DeleteBranchAsync(branch.Name, force: true, CancellationToken.None)))
                return;
        }
        catch (GitException exception)
        {
            notifications.Notify(NotificationKind.Error, Strings.DeleteBranchFailed(branch.Name), exception.Message);
            return;
        }

        await repositories.RefreshAsync();
        notifications.Notify(NotificationKind.Success, Strings.DeleteBranchDone(branch.Name), Strings.DeleteBranchPointed(branch.Sha[..Math.Min(8, branch.Sha.Length)]), Strings.Restore,
            () => _ = RunQuickAsync(Strings.RestoreBranchFailed(branch.Name), () => repository.CreateBranchAsync(branch.Name, branch.Sha, checkout: false, CancellationToken.None)));
    }

    /// <summary>Pushes a local branch that is not checked out to where it pushes.</summary>
    public async Task PushBranchAsync(GitRef branch)
    {
        ArgumentNullException.ThrowIfNull(branch);
        if (branch.IsHead)
        {
            await ShowPushAsync();
            return;
        }

        if (Repository is not { } repository || await repository.GetPushTargetAsync(branch.Name, CancellationToken.None) is not { } target)
        {
            NoRemote(Strings.NoRemotePush);
            return;
        }

        var destination = $"{target.Remote}/{target.Branch}";
        if (await RunAsync(Strings.TaskPushing(branch: branch.Name, destination: destination),
                (progress, token) => repository.PushAsync(branch.Name, target.Remote, target.Branch, !target.HasUpstream, false, token, progress),
                handle: exception => PushFailedAsync(exception, destination, branch.Name)))
            notifications.Notify(NotificationKind.Success, Strings.PushDone(branch: branch.Name, destination: destination));
    }

    /// <summary>Makes a local branch track a remote branch with the same name, creating the local branch when there is none.</summary>
    public async Task TrackAsync(GitRef remoteBranch)
    {
        ArgumentNullException.ThrowIfNull(remoteBranch);
        if (Repository is not { } repository)
            return;

        var refs = await repository.GetRefsAsync(CancellationToken.None);
        var exists = refs.Any(r => r.Kind == RefKind.LocalBranch && r.Name == remoteBranch.BranchName);
        if (await RunQuickAsync(Strings.TrackFailed(remoteBranch.Name), () => repository.TrackAsync(remoteBranch.BranchName, remoteBranch.Name, create: !exists, CancellationToken.None)))
            notifications.Notify(NotificationKind.Success, Strings.TrackDone(branch: remoteBranch.BranchName, upstream: remoteBranch.Name));
    }

    /// <summary>Merges a branch into the current one.</summary>
    public async Task MergeAsync(GitRef branch)
    {
        ArgumentNullException.ThrowIfNull(branch);
        if (Repository is { } repository &&
            await RunAsync(Strings.TaskMerging(branch: branch.Name, target: CurrentBranch ?? "HEAD"), (_, token) => repository.MergeAsync(branch.FullName, token), handle: ConflictAsync))
            notifications.Notify(NotificationKind.Success, Strings.MergeDone(branch: branch.Name, target: CurrentBranch ?? "HEAD"));
    }

    /// <summary>Shows the commits that two branches do not share.</summary>
    public void Compare(GitRef branch)
    {
        ArgumentNullException.ThrowIfNull(branch);
        var current = CurrentBranch ?? "HEAD";
        ShowHistory(new HistoryRequest(new LogQuery { Revision = $"HEAD...{branch.FullName}" }, Strings.CompareTitle(current: current, branch: branch.Name)));
    }

    /// <summary>Applies a commit's change on the current branch.</summary>
    public async Task CherryPickAsync(GitCommit commit)
    {
        ArgumentNullException.ThrowIfNull(commit);
        if (Repository is { } repository && await RunAsync(Strings.TaskCherryPicking(commit.ShortSha), (_, token) => repository.CherryPickAsync(commit, token), handle: ConflictAsync))
            notifications.Notify(NotificationKind.Success, Strings.CherryPickDone(commit.ShortSha), commit.Subject);
    }

    /// <summary>Adds a commit that undoes a commit's change.</summary>
    public async Task RevertAsync(GitCommit commit)
    {
        ArgumentNullException.ThrowIfNull(commit);
        if (Repository is { } repository && await RunAsync(Strings.TaskReverting(commit.ShortSha), (_, token) => repository.RevertAsync(commit, token), handle: ConflictAsync))
            notifications.Notify(NotificationKind.Success, Strings.RevertDone(commit.ShortSha), commit.Subject);
    }

    /// <summary>Moves the current branch to a commit, asking how far; a hard reset asks again.</summary>
    public async Task ResetAsync(GitCommit commit, ResetMode? mode = null)
    {
        ArgumentNullException.ThrowIfNull(commit);
        if (Repository is not { } repository)
            return;

        var branch = CurrentBranch ?? "HEAD";
        if (mode is null)
        {
            var choice = await notifications.ChooseAsync(Strings.ResetTitle(branch: branch, commit: commit.ShortSha),
                Strings.ResetMessage,
                [Strings.ResetHard, Strings.ResetSoft, Strings.ResetMixed]);
            if (choice is null)
                return;
            mode = choice.Value switch { 0 => ResetMode.Hard, 1 => ResetMode.Soft, _ => ResetMode.Mixed };
        }

        if (mode == ResetMode.Hard && !await notifications.ConfirmAsync(Strings.ResetHardTitle(branch),
                Strings.ResetHardMessage(commit: commit.ShortSha, branch: branch), Strings.Reset, isDestructive: true))
            return;

        if (await RunQuickAsync(Strings.ResetFailed(branch), () => repository.ResetAsync(commit.Sha, mode.Value, CancellationToken.None)))
            notifications.Notify(NotificationKind.Success, Strings.ResetTitle(branch: branch, commit: commit.ShortSha), commit.Subject);
    }

    /// <summary>Asks for a description and stashes every local change.</summary>
    public async Task StashAsync()
    {
        if (Repository is not { } repository || quickInput is null)
            return;
        if (repositories.Status?.ChangedCount == 0)
        {
            notifications.Notify(NotificationKind.Info, Strings.StashNothing, Strings.StashNothingMessage);
            return;
        }

        var answer = await quickInput.InputAsync(new InputBoxOptions { Title = Strings.StashTitle, Prompt = Strings.StashPrompt, Placeholder = Strings.StashPlaceholder });
        if (answer?.Trim() is { } description && await RunQuickAsync(Strings.StashFailed, () => repository.StashAsync(description, CancellationToken.None)))
            notifications.Notify(NotificationKind.Success, Strings.StashDone, description.Length > 0 ? description : null, Strings.Unstash, () => _ = UnstashAsync());
    }

    /// <summary>Brings back a stash entry, asking which when there are several.</summary>
    public async Task UnstashAsync()
    {
        if (Repository is not { } repository)
            return;

        var stashes = await repository.GetStashesAsync(CancellationToken.None);
        if (stashes.Count == 0)
        {
            notifications.Notify(NotificationKind.Info, Strings.StashEmpty);
            return;
        }

        var index = 0;
        if (stashes.Count > 1)
        {
            if (quickInput is null)
                return;

            var choice = await quickInput.PickAsync(
                [.. stashes.Select((s, i) => new PickItem<int>(s.Message, i) { Description = Relative(s.Date) })],
                new QuickPickOptions { Title = Strings.Unstash, Placeholder = Strings.UnstashPrompt, MatchOnDescription = true });
            if (choice is null)
                return;
            index = choice.Value;
        }

        if (await RunAsync(Strings.TaskUnstashing, (_, token) => repository.UnstashAsync(index, token), handle: ConflictAsync))
            notifications.Notify(NotificationKind.Success, Strings.UnstashDone, stashes[index].Message);
    }

    /// <summary>Stops a merge, rebase, cherry-pick or revert that stopped at conflicts.</summary>
    public async Task AbortAsync()
    {
        if (Repository is { } repository && repositories.Ongoing is var ongoing and not OngoingOperation.None &&
            await notifications.ConfirmAsync(Strings.AbortTitle(Describe(ongoing)), Strings.AbortMessage, Strings.Abort, isDestructive: true))
            await RunQuickAsync(Strings.AbortFailed, () => repository.AbortAsync(ongoing, CancellationToken.None));
    }

    /// <summary>Goes on with a rebase, cherry-pick or revert once its conflicts are resolved and staged.</summary>
    public async Task ContinueAsync()
    {
        if (Repository is { } repository && repositories.Ongoing is var ongoing and not OngoingOperation.None)
            await RunAsync(Strings.TaskContinuing(Describe(ongoing)), (_, token) => repository.ContinueAsync(ongoing, token), handle: ConflictAsync);
    }

    /// <summary>Opens a changed file's diff: HEAD against the staged version, or the staged version against the working copy, which can be
    /// edited.</summary>
    public async Task ShowDiffAsync(StatusEntry entry, bool staged)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (Repository is not { } repository)
            return;

        var full = repository.FullPath(entry.Path);
        if (diffs is null)
        {
            if (File.Exists(full))
                await editors.OpenAsync(full);
            return;
        }

        var name = Path.GetFileName(entry.Path);
        var original = entry.OriginalPath ?? entry.Path;
        var head = await repository.GetFileAsync("HEAD", original, CancellationToken.None) ?? "";
        if (staged)
        {
            var index = await repository.GetFileAsync("", entry.Path, CancellationToken.None) ?? "";
            diffs.Open(Strings.DiffHeadAndStaged(name), new DiffSide("HEAD", head) { FilePath = full }, new DiffSide(Strings.DiffStaged, index) { FilePath = full });
            return;
        }

        var hasStaged = entry.IsStaged;
        var left = hasStaged ? await repository.GetFileAsync("", entry.Path, CancellationToken.None) ?? head : entry.Kind == StatusKind.Untracked ? "" : head;
        var working = File.Exists(full) ? await File.ReadAllTextAsync(full) : "";
        diffs.Open(hasStaged ? Strings.DiffStagedAndWorking(name) : Strings.DiffHeadAndWorking(name), new DiffSide(hasStaged ? Strings.DiffStaged : "HEAD", left) { FilePath = full },
            new DiffSide(Strings.DiffWorkingTree, working) { FilePath = full, IsEditable = File.Exists(full) });
    }

    /// <summary>Opens the diff of a file a commit changed, against the commit's first parent.</summary>
    public async Task ShowCommitDiffAsync(GitCommit commit, FileChange change)
    {
        ArgumentNullException.ThrowIfNull(commit);
        ArgumentNullException.ThrowIfNull(change);
        if (Repository is not { } repository || diffs is null)
            return;

        var parent = commit.Parents.Count > 0 ? commit.Parents[0] : null;
        var before = parent is null || change.Status == 'A' ? "" : await repository.GetFileAsync(parent, change.OldPath ?? change.Path, CancellationToken.None) ?? "";
        var after = change.Status == 'D' ? "" : await repository.GetFileAsync(commit.Sha, change.Path, CancellationToken.None) ?? "";
        var full = repository.FullPath(change.Path);
        diffs.Open($"{Path.GetFileName(change.Path)} ({commit.ShortSha})", new DiffSide(parent is null ? Strings.DiffEmpty : parent[..8], before) { FilePath = full },
            new DiffSide(commit.ShortSha, after) { FilePath = full });
    }

    /// <summary>Opens the diff of a file of the open folder, if it changed.</summary>
    public async Task ShowFileDiffAsync(string fullPath)
    {
        if (Find(fullPath) is not { } entry)
        {
            notifications.Notify(NotificationKind.Info, Strings.NoChanges(Path.GetFileName(fullPath)));
            return;
        }

        await ShowDiffAsync(entry, staged: !entry.IsUnstaged && entry.IsStaged);
    }

    /// <summary>Rolls back files to HEAD after asking.</summary>
    public async Task RollbackAsync(IReadOnlyList<StatusEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (Repository is not { } repository || entries.Count == 0)
            return;

        var names = string.Join(", ", entries.Take(4).Select(e => Path.GetFileName(e.Path)));
        if (entries.Count > 4)
            names = Strings.RollbackNamesMore(names: names, count: entries.Count - 4);
        var untracked = entries.Count(e => e.Kind == StatusKind.Untracked);
        var title = entries.Count == 1 ? Strings.RollbackTitleOne(Path.GetFileName(entries[0].Path)) : Strings.RollbackTitleMany(entries.Count);
        var message = Strings.RollbackMessage(names: names, files: entries.Count) + (untracked > 0 ? " " + Strings.RollbackUntracked : "");
        if (!await notifications.ConfirmAsync(title, message, Strings.RollBack, isDestructive: true))
            return;

        if (await RunQuickAsync(Strings.RollbackFailed, () => repository.DiscardAsync(entries, CancellationToken.None)))
            notifications.Notify(NotificationKind.Success, entries.Count == 1 ? Strings.RollbackDoneOne(Path.GetFileName(entries[0].Path)) : Strings.RollbackDoneMany(entries.Count));
    }

    /// <summary>Rolls back a file of the open folder, or every changed file in a folder, after asking.</summary>
    public Task RollbackFileAsync(string fullPath, bool isDirectory = false) =>
        ChangesAt(fullPath, isDirectory) is { Count: > 0 } entries ? RollbackAsync(entries) : Task.CompletedTask;

    /// <summary>Adds paths to the repository's top <c>.gitignore</c>.</summary>
    public async Task AddToGitignoreAsync(IReadOnlyList<string> relativePaths)
    {
        ArgumentNullException.ThrowIfNull(relativePaths);
        if (Repository is not { } repository || relativePaths.Count == 0)
            return;

        var file = Path.Combine(repository.Root, ".gitignore");
        var existing = File.Exists(file) ? await File.ReadAllTextAsync(file) : "";
        var lines = relativePaths.Select(p => "/" + p).Where(line => !existing.Split('\n').Contains(line, StringComparer.Ordinal)).ToList();
        if (lines.Count == 0)
            return;

        var separator = existing.Length > 0 && !existing.EndsWith('\n') ? "\n" : "";
        await File.AppendAllTextAsync(file, separator + string.Join('\n', lines) + "\n");
        await repositories.RefreshAsync();
    }

    /// <summary>Shows the history of a file of the open folder, following renames.</summary>
    public void ShowFileHistory(string fullPath)
    {
        if (Repository is not { } repository)
            return;
        var relative = repository.RelativePath(fullPath);
        ShowHistory(new HistoryRequest(new LogQuery { Path = relative, Follow = File.Exists(fullPath) }, Strings.HistoryOf(relative)));
    }

    /// <summary>Finds the status of a file of the open folder, or null when it has no changes.</summary>
    public StatusEntry? Find(string fullPath)
    {
        if (Repository is not { } repository || repositories.Status is not { } status)
            return null;
        var relative = repository.RelativePath(fullPath);
        return status.Entries.FirstOrDefault(e => e.Path == relative && e.Kind != StatusKind.Ignored);
    }

    /// <summary>Gets the changes at a path: the file's own, or every changed file in a folder; ignored files are left out.</summary>
    public IReadOnlyList<StatusEntry> ChangesAt(string fullPath, bool isDirectory) =>
        Repository is { } repository && repositories.Status is { } status && Contains(fullPath) ? ChangesAt(status, repository.RelativePath(fullPath), isDirectory) : [];

    /// <summary>Whether a path is in the working tree of the open folder's repository, outside its Git folder.</summary>
    public bool Contains(string fullPath) => Repository is { } repository && IsInWorkingTree(repository.RelativePath(fullPath));

    /// <summary>Gets the changes at a path relative to the repository, as <see cref="GitRepository.RelativePath"/> gives it.</summary>
    internal static IReadOnlyList<StatusEntry> ChangesAt(StatusSnapshot status, string relativePath, bool isDirectory) =>
        [.. status.Entries.Where(e => e.Kind != StatusKind.Ignored && (isDirectory ? e.Path.StartsWith(relativePath + "/", StringComparison.Ordinal) : e.Path == relativePath))];

    /// <summary>Whether a path relative to the repository is in its working tree: not the top folder, outside it, or in the Git folder.</summary>
    internal static bool IsInWorkingTree(string relativePath) =>
        relativePath is not ("." or ".." or ".git") && !Path.IsPathRooted(relativePath)
        && !relativePath.StartsWith("../", StringComparison.Ordinal) && !relativePath.StartsWith(".git/", StringComparison.Ordinal);

    public void Dispose() => autoFetch.Dispose();

    // Commits as a count for people: "1 commit", "3 commits".
    internal static string Commits(int count) => Strings.Commits(count);

    /// <summary>Says how long ago a date was, such as "5 minutes ago" or "Mar 3".</summary>
    internal static string Relative(DateTimeOffset date, DateTimeOffset? now = null)
    {
        var span = (now ?? DateTimeOffset.Now) - date;
        return span.TotalMinutes < 1 ? Strings.TimeJustNow
            : span.TotalHours < 1 ? Strings.TimeMinutesAgo((int)span.TotalMinutes)
            : span.TotalDays < 1 ? Strings.TimeHoursAgo((int)span.TotalHours)
            : span.TotalDays < 7 ? Strings.TimeDaysAgo((int)span.TotalDays)
            : date.ToLocalTime().Year == (now ?? DateTimeOffset.Now).ToLocalTime().Year ? date.ToLocalTime().ToString("MMM d", CultureInfo.CurrentCulture)
            : date.ToLocalTime().ToString("MMM d, yyyy", CultureInfo.CurrentCulture);

    }

    private async Task CheckoutCoreAsync(string name, Func<bool, IProgress<string>, CancellationToken, Task> checkout)
    {
        if (Repository is not { } repository)
            return;

        var succeeded = await RunAsync(Strings.TaskCheckingOut(name), (progress, token) => checkout(false, progress, token), handle: async exception =>
        {
            if (exception.Kind != GitErrorKind.LocalChangesWouldBeOverwritten)
                return false;

            var choice = await notifications.ChooseAsync(Strings.CheckoutOverwrite,
                $"{exception.Message}\n\n{Strings.CheckoutOverwriteMessage(name)}",
                [Strings.ForceCheckout, Strings.SmartCheckout]);
            if (choice == 1)
                await SmartCheckoutAsync(repository, name, checkout);
            else if (choice == 0)
                await RunAsync(Strings.TaskCheckingOut(name), (progress, token) => checkout(true, progress, token));
            return true;
        });
        if (succeeded)
            notifications.Notify(NotificationKind.Success, Strings.CheckoutDone(name));
    }

    private async Task SmartCheckoutAsync(GitRepository repository, string name, Func<bool, IProgress<string>, CancellationToken, Task> checkout)
    {
        if (!await RunQuickAsync(Strings.StashLocalFailed, () => repository.StashAsync($"Smart checkout of {name}", CancellationToken.None)))
            return;
        if (!await RunAsync(Strings.TaskCheckingOut(name), (progress, token) => checkout(false, progress, token)))
        {
            await RunQuickAsync(Strings.UnstashLocalFailed, () => repository.UnstashAsync(0, CancellationToken.None));
            return;
        }

        try
        {
            await repository.UnstashAsync(0, CancellationToken.None);
            notifications.Notify(NotificationKind.Success, Strings.CheckoutDone(name), Strings.CheckoutDoneWithChanges);
        }
        catch (GitException exception)
        {
            notifications.Notify(NotificationKind.Warning, Strings.CheckoutConflicts(name),
                $"{Strings.CheckoutConflictsMessage(name)}\n\n{exception.Message}", Strings.ShowChanges, ShowCommitWindow);
        }
        finally
        {
            await repositories.RefreshAsync();
        }
    }

    private async Task<bool> RunAsync(string title, Func<IProgress<string>, CancellationToken, Task> work, Func<GitException, Task<bool>>? handle = null, bool quiet = false)
    {
        using var cancellation = new CancellationTokenSource();
        var task = quiet ? null : tasks.Start(title, cancellation.Cancel);
        SetBusy(+1);
        try
        {
            await work(new TaskProgress(task), cancellation.Token);
            return true;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            notifications.Notify(NotificationKind.Info, Strings.TaskCancelled(title));
            return false;
        }
        catch (GitException exception)
        {
            log?.AppendLine(exception.Message);
            if (handle is not null && await handle(exception))
                return false;
            if (!quiet)
                ShowError(title, exception);
            return false;
        }
        finally
        {
            task?.Dispose();
            SetBusy(-1);
            await RefreshQuietlyAsync();
        }
    }

    private async Task<bool> RunQuickAsync(string failure, Func<Task> work)
    {
        try
        {
            await work();
            return true;
        }
        catch (GitException exception)
        {
            notifications.Notify(NotificationKind.Error, failure, exception.Message);
            return false;
        }
        catch (IOException exception)
        {
            notifications.Notify(NotificationKind.Error, failure, exception.Message);
            return false;
        }
        finally
        {
            await RefreshQuietlyAsync();
        }
    }

    private async Task RefreshQuietlyAsync()
    {
        try
        {
            await repositories.RefreshAsync();
        }
        catch (GitException)
        {
        }
    }

    private void ShowError(string title, GitException exception)
    {
        var headline = Strings.TaskFailed(title);
        if (exception.Kind == GitErrorKind.AuthenticationFailed)
            notifications.Notify(NotificationKind.Error, headline, $"{Strings.CredentialsRejected}\n\n{exception.Message}");
        else
            notifications.Notify(NotificationKind.Error, headline, exception.Message, Strings.ShowGitOutput, () => log?.Show());
    }

    private Task<bool> PullFailedAsync(GitException exception)
    {
        if (exception.Kind == GitErrorKind.Conflict)
            return ConflictAsync(exception);
        return Task.FromResult(false);
    }

    private Task<bool> PushFailedAsync(GitException exception, string destination, string branch)
    {
        switch (exception.Kind)
        {
            case GitErrorKind.PushRejected:
                notifications.Notify(NotificationKind.Error, Strings.PushRejected,
                    Strings.PushRejectedMessage(destination: destination, branch: branch), Strings.UpdateAndPush, () => _ = PullThenPushAsync());
                return Task.FromResult(true);
            case GitErrorKind.StaleRemote:
                notifications.Notify(NotificationKind.Error, Strings.ForcePushStopped,
                    Strings.ForcePushStoppedMessage(destination), Strings.CommandFetch, () => _ = FetchAsync());
                return Task.FromResult(true);
            default:
                return Task.FromResult(false);
        }
    }

    private async Task PullThenPushAsync()
    {
        if (await PullAsync())
            await PushAsync();
    }

    private Task<bool> ConflictAsync(GitException exception)
    {
        if (exception.Kind != GitErrorKind.Conflict)
            return Task.FromResult(false);

        var count = repositories.Status?.Conflicts.Count() ?? 0;
        notifications.Notify(NotificationKind.Warning, Strings.Conflicts(count),
            Strings.ConflictsMessage, Strings.ShowChanges, ShowCommitWindow);
        return Task.FromResult(true);
    }

    private async Task<bool> ConfirmAmendPushAsync() =>
        await notifications.ConfirmAsync(Strings.ForcePushAmendedTitle,
            Strings.ForcePushAmendedMessage, Strings.ForcePush, isDestructive: true);

    private void NoRemote(string message) =>
        notifications.Notify(NotificationKind.Warning, Strings.NoRemote, message);

    private static async Task<string?> ValidateNewBranchAsync(GitRepository repository, IReadOnlyList<GitRef> refs, string name)
    {
        if (name.Length == 0)
            return Strings.BranchNameEmpty;
        if (refs.Any(r => r.Kind == RefKind.LocalBranch && r.Name == name))
            return Strings.BranchExists(name);
        return await repository.IsValidBranchNameAsync(name, CancellationToken.None) ? null : Strings.BranchNameInvalid(name);
    }

    private static async Task<int> CountAsync(GitRepository repository, string range)
    {
        try
        {
            return int.Parse((await repository.ReadAsync(["rev-list", "--count", range], CancellationToken.None)).Trim(), CultureInfo.InvariantCulture);
        }
        catch (GitException)
        {
            return 0;
        }
    }

    private void ScheduleAutoFetch()
    {
        var minutes = settings.Get<int>(GitSettings.AutoFetchMinutes);
        var period = minutes > 0 ? TimeSpan.FromMinutes(minutes) : Timeout.InfiniteTimeSpan;
        autoFetch.Change(period, period);
    }

    private void SetBusy(int change)
    {
        var was = Volatile.Read(ref busy) > 0;
        var now = Interlocked.Add(ref busy, change) > 0;
        if (was != now)
            SyncingChanged?.Invoke(this, EventArgs.Empty);
    }

    private static string Describe(OngoingOperation operation) => operation switch
    {
        OngoingOperation.CherryPick => "cherry-pick",
        _ => operation.ToString().ToLowerInvariant(),
    };

    private static string Quote(string argument) => argument.Contains(' ', StringComparison.Ordinal) ? $"\"{argument}\"" : argument;

    [GeneratedRegex(@"(\d{1,3})%")]
    private static partial Regex Percent();

    // Passes Git's progress lines to the background task, with the percentage as its fraction.
    private sealed class TaskProgress(IBackgroundTask? task) : IProgress<string>
    {
        public void Report(string value)
        {
            if (task is null)
                return;
            var match = Percent().Match(value);
            task.Report(value.Trim(), match.Success ? double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) / 100 : null);
        }
    }
}
