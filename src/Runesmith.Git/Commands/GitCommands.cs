using System.Composition;
using Runesmith.Git.Git;
using Runesmith.Git.Repositories;
using Runesmith.Git.Views.Branches;
using Runesmith.Sdk.Commands;
using Runesmith.Sdk.Documents;
using Runesmith.Sdk.Workspace;

namespace Runesmith.Git.Commands;

/// <summary>The Git commands, their keys, the Git menu, and the Git groups of the editor's and the Explorer's context menus.</summary>
[Export(typeof(ICommandContributor))]
[method: ImportingConstructor]
internal sealed class GitCommands(GitOperations operations, IWorkspace workspace, IEditorService editors, Lazy<BranchWidgetProvider> branchWidget) : ICommandContributor
{
    public const string Commit = "git.commit";
    public const string Push = "git.push";
    public const string Pull = "git.pull";
    public const string Fetch = "git.fetch";
    public const string NewBranch = "git.newBranch";
    public const string Branches = "git.branches";
    public const string Checkout = "git.checkout";
    public const string ShowHistory = "git.showHistory";
    public const string FileHistory = "git.fileHistory";
    public const string ShowDiff = "git.showDiff";
    public const string RollbackFile = "git.rollbackFile";
    public const string AddToGitignore = "git.addToGitignore";
    public const string Stash = "git.stash";
    public const string Unstash = "git.unstash";
    public const string Initialize = "git.init";

    /// <summary>The main menu's Git menu.</summary>
    public const string Menu = "Git";

    /// <summary>The group of the Git items in the context menus, which the hosting links join.</summary>
    public const string ContextGroup = "git";

    private const string Category = "Git";

    private RepositoryService Repositories => operations.Repositories;

    public void Contribute(ICommandRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        GitIcons.Register();
        registry.AddMenu(new MenuDefinition(Menu, Menu));
        var order = 0;
        void Add(string id, string title, string? icon, string? keys, string group, Func<object?, Task> run, Func<object?, bool> canRun, string description, bool inMenu = true)
        {
            registry.Add(new CommandDefinition(id, title, Category) { Icon = icon, KeyBinding = keys, Description = description }, run, canRun);
            if (inMenu)
                registry.AddMenuItem(new MenuItemDefinition(Menu, id, group, order++));
        }

        bool InRepository(object? _) => Repositories.Current is not null;

        Add(Commit, Strings.CommandCommit, GitIcons.Commit, "Ctrl+Alt+K", "1-commit", _ => Run(operations.ShowCommitWindow), InRepository,
            Strings.CommandCommitDescription);
        Add(Push, Strings.CommandPush, GitIcons.Push, "Ctrl+Alt+Shift+K", "1-commit", _ => operations.ShowPushAsync(), InRepository,
            Strings.CommandPushDescription);
        Add(Pull, Strings.CommandPull, GitIcons.Pull, "Ctrl+T", "1-commit", _ => operations.PullAsync(), InRepository,
            Strings.CommandPullDescription);
        Add(Fetch, Strings.CommandFetch, GitIcons.Fetch, null, "1-commit", _ => operations.FetchAsync(), InRepository, Strings.CommandFetchDescription);
        Add(NewBranch, Strings.CommandNewBranch, "plus", null, "2-branches", _ => operations.NewBranchAsync(), InRepository, Strings.CommandNewBranchDescription);
        Add(Branches, Strings.CommandBranches, GitIcons.Branch, null, "2-branches", _ => Run(OpenBranches), InRepository,
            Strings.CommandBranchesDescription);
        Add(Checkout, Strings.CommandCheckout, GitIcons.Branch, null, "2-branches", _ => operations.PickCheckoutAsync(), InRepository, Strings.CommandCheckoutDescription);
        Add(ShowHistory, Strings.CommandShowHistory, GitIcons.History, null, "3-history", _ => Run(operations.ShowLog), InRepository, Strings.CommandShowHistoryDescription);
        Add(FileHistory, Strings.CommandFileHistory, GitIcons.History, null, "3-history", argument => Run(() => operations.ShowFileHistory(Target(argument)!.Path)), HasHistory,
            Strings.CommandFileHistoryDescription);
        Add(ShowDiff, Strings.CommandShowDiff, "git-compare", null, "4-file", argument => operations.ShowFileDiffAsync(Target(argument)!.Path),
            argument => Target(argument) is { IsDirectory: false } target && operations.Find(target.Path) is not null, Strings.CommandShowDiffDescription);
        Add(RollbackFile, Strings.CommandRollbackFile, "undo", null, "4-file", argument => Target(argument) is { } target ? operations.RollbackFileAsync(target.Path, target.IsDirectory) : Task.CompletedTask,
            argument => Target(argument) is { } target && operations.ChangesAt(target.Path, target.IsDirectory).Count > 0,
            Strings.CommandRollbackFileDescription);
        Add(AddToGitignore, Strings.CommandAddToGitignore, "eye-off", null, "4-file", argument => operations.AddToGitignoreAsync([operations.Repositories.Repository!.RelativePath(Target(argument)!.Path)]),
            IsUntracked, Strings.CommandAddToGitignoreDescription, inMenu: false);
        Add(Stash, Strings.CommandStash, null, null, "5-stash", _ => operations.StashAsync(), InRepository, Strings.CommandStashDescription);
        Add(Unstash, Strings.CommandUnstash, null, null, "5-stash", _ => operations.UnstashAsync(), InRepository, Strings.CommandUnstashDescription);
        Add(Initialize, Strings.CommandInitialize, GitIcons.Branch, null, "6-repository", _ => operations.InitializeAsync(),
            _ => workspace.RootPath is not null && Repositories.Current is null && Repositories.Opened.IsCompleted, Strings.CommandInitializeDescription);

        // The editor already offers its own Show Changes against the same base.
        string[] editor = [CommandIds.RollbackLines, FileHistory];
        for (var i = 0; i < editor.Length; i++)
            registry.AddMenuItem(new MenuItemDefinition(ContextMenus.Editor, editor[i], ContextGroup, i));
        string[] explorer = [ShowDiff, FileHistory, RollbackFile, AddToGitignore];
        for (var i = 0; i < explorer.Length; i++)
            registry.AddMenuItem(new MenuItemDefinition(ContextMenus.Explorer, explorer[i], ContextGroup, i));
    }

    private void OpenBranches()
    {
        if (branchWidget.Value.Widget?.OpenPopup() != true)
            operations.ShowLog();
    }

    // History needs a path Git may know: a folder, or a file that is not untracked.
    private bool HasHistory(object? argument) =>
        Target(argument) is { } target && operations.Contains(target.Path) && (target.IsDirectory || operations.Find(target.Path)?.Kind != StatusKind.Untracked);

    // A folder counts as untracked when every change in it is an untracked file.
    private bool IsUntracked(object? argument) =>
        Target(argument) is { } target && operations.ChangesAt(target.Path, target.IsDirectory) is { Count: > 0 } changes && changes.All(e => e.Kind == StatusKind.Untracked);

    // A command run from a context menu gets what the menu was opened on; from the palette or a key, it works on the active editor's file.
    private ContextMenuTarget? Target(object? argument) => argument switch
    {
        ContextMenuTarget target => target,
        _ => editors.ActiveEditor?.Document.FilePath is { } path ? new ContextMenuTarget(path) : null,
    };

    private static Task Run(Action action)
    {
        action();
        return Task.CompletedTask;
    }
}
