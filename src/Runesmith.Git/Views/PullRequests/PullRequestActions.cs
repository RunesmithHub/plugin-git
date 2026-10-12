using System.Composition;
using HammerUI.Services;
using Runesmith.Git.Contracts;
using Runesmith.Git.Git;
using Runesmith.Sdk.Shell;
using Runesmith.Sdk.Tasks;

namespace Runesmith.Git.Views.PullRequests;

/// <summary>Creating and checking out pull requests, for the commands and the tool window alike.</summary>
[Export]
[Shared]
[method: ImportingConstructor]
internal sealed class PullRequestActions(
    PullRequestService service,
    [Import(AllowDefault = true)] IDialogService? dialogs,
    [Import(AllowDefault = true)] ILauncher? launcher,
    [Import(AllowDefault = true)] INotificationService? notifications,
    [Import(AllowDefault = true)] IBackgroundTasks? tasks)
{
    /// <summary>Raised on the UI thread after a pull request was created, so lists can show it.</summary>
    public event EventHandler? Created;

    /// <summary>Shows the Create Pull Request dialog for the current branch, after signing in to the host when needed.</summary>
    public async Task CreateAsync()
    {
        if (service.Current is not { } context)
        {
            Notify(NotificationKind.Info, Strings.PrNoHostedRepository, Strings.PrNoHostedRepositoryCreate);
            return;
        }

        if (context.Repository.Branch is null)
        {
            Notify(NotificationKind.Info, Strings.PrNoBranch, Strings.PrNoBranchMessage);
            return;
        }

        if (dialogs is null)
        {
            Notify(NotificationKind.Error, Strings.PrNeedsDialogs, Strings.CloneNeedsDialogsMessage);
            return;
        }

        if (!await SignInAsync(context.Host))
            return;

        if (await dialogs.ShowAsync(new CreatePullRequestDialog(service, dialogs, context)) is PullRequest created)
        {
            Notify(NotificationKind.Success, Strings.PrCreated(Number(created.Number)), created.Title, Strings.OpenInBrowser, () => OpenUrl(created.WebUrl));
            Created?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Fetches a pull request into a local branch and switches to it, as a background task.</summary>
    public async Task CheckoutAsync(PullRequest pullRequest)
    {
        ArgumentNullException.ThrowIfNull(pullRequest);
        if (service.Current is not { } context)
            return;

        using var cancel = new CancellationTokenSource();
        using var task = tasks?.Start(Strings.PrCheckingOut(Number(pullRequest.Number)), cancel.Cancel);
        try
        {
            var branch = await service.CheckoutAsync(context, pullRequest, cancel.Token);
            Notify(NotificationKind.Success, Strings.PrSwitched(branch), Strings.PrNumbered(number: Number(pullRequest.Number), title: pullRequest.Title));
        }
        catch (OperationCanceledException) when (cancel.IsCancellationRequested)
        {
        }
        catch (GitException exception)
        {
            Notify(NotificationKind.Error, Strings.PrCheckoutFailed(Number(pullRequest.Number)), exception.Message);
        }
    }

    /// <summary>Asks the user to sign in to a host when it is signed out; returns whether it is signed in afterwards.</summary>
    public static async Task<bool> SignInAsync(IRepositoryHost host)
    {
        ArgumentNullException.ThrowIfNull(host);
        if (host.Account is not null)
            return true;

        try
        {
            return await host.SignInAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    /// <summary>Opens a page in the browser, or shows its address when Runesmith cannot open one.</summary>
    public void OpenUrl(Uri url)
    {
        if (launcher is not null)
            launcher.OpenUrl(url);
        else
            notifications?.Notify(NotificationKind.Info, Strings.OpenAddressInBrowser, url.AbsoluteUri);
    }

    private static string Number(int number) => number.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private void Notify(NotificationKind kind, string title, string? message, string? actionText = null, Action? action = null) =>
        notifications?.Notify(kind, title, message, actionText, action);
}
