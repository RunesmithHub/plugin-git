# The Git plugin's contracts

The Git plugin's public API is in its contracts assembly, `Runesmith.Git.Contracts`. The hub publishes it to its feed with each release
of the Git plugin, as the package `RunesmithHub.Runesmith.Git.Contracts`. A plugin that uses it references that package, without its
runtime assets, and lists the Git plugin in the `dependencies` of its manifest:

```json
"dependencies": [{ "id": "runesmith.git", "range": "^0.2.0" }]
```

The contracts between the Git plugin and the hosting plugins, such as GitHub, are in the namespace `Runesmith.Git.Contracts`, so any
plugin can use them or add a hosting service of its own.

| Contract | Provided by | What it does |
| --- | --- | --- |
| `IRepositoryService` | The Git plugin | The open folder's repository: its top folder, branch, HEAD, upstream, ahead and behind, and remotes, with `Changed` on the UI thread. |
| `IGitService` | The Git plugin | Runs the user's `git` the way the Git plugin does: never waiting for a prompt, with credentials for one command. |
| `IGitCredentialSource` | Each hosting plugin | A signed-in account's token for its server's `https` remotes. The first source that answers wins. |
| `IRepositoryHostProvider` | Each hosting plugin | The service's accounts, and adding one, such as on another server. |
| `IRepositoryHost` | Each hosting plugin | One account: signing in and out, its repositories, web links, the default branch and pull requests. |

A plugin that lists the Git plugin as a dependency loads only when the Git plugin does, so its services are there to import.
`IGitService` returns a `GitResult` with Git's exit code and output rather than throwing; `GitRunOptions.CredentialsFor` names the remote
whose credentials the command gets:

```csharp
var result = await git.RunAsync(repository.Root, ["fetch", "origin"],
    new GitRunOptions { CredentialsFor = repository.Remotes[0].FetchUrl }, cancellationToken);
if (!result.Succeeded)
    notifications.Notify(NotificationKind.Error, "Fetch failed", result.Error);
```

## Write a hosting plugin

A hosting plugin exports a provider of its accounts, one host per account, and a credential source for its servers. The Git plugin then
shows a tab per account in the clone dialog, an **Add Account** entry for the provider, the **Pull Requests** window for repositories the
host owns, and **Open on** and **Copy Link** commands named after the provider. It references only the Git plugin's contracts.

```csharp
[Export(typeof(IRepositoryHostProvider))]
[Shared]
[method: ImportingConstructor]
public sealed class AcmeHostProvider(AcmeHost host) : IRepositoryHostProvider
{
    public string Name => "Acme";

    public string Icon => "globe";

    public IReadOnlyList<IRepositoryHost> Hosts { get; } = [host];

    public event EventHandler? Changed { add { } remove { } }

    public async Task<IRepositoryHost?> AddAccountAsync(CancellationToken cancellationToken) =>
        await host.SignInAsync(cancellationToken) ? host : null;
}

[Export(typeof(IGitCredentialSource))]
[method: ImportingConstructor]
public sealed class AcmeCredentials(AcmeHost host) : IGitCredentialSource
{
    public async Task<GitCredentials?> GetAsync(string remoteUrl, CancellationToken cancellationToken) =>
        host.Owns(remoteUrl) && await host.GetTokenAsync(cancellationToken) is { } token
            ? new GitCredentials(host.Server, host.Account!, token)
            : null;
}
```

`AcmeHost` implements `IRepositoryHost`:

- `Owns` says whether a remote URL is on its server, in any of Git's forms, such as `https://git.acme.dev/team/app.git` or
  `git@git.acme.dev:team/app.git`. When several hosts own a remote, a signed-in one wins.
- `GetWebUrl` turns a `WebTarget` into a page: the repository, a file at a revision with its lines, a folder, a commit or a branch.
- `GetRepositoriesAsync` lists what the account may clone, as `HostedRepository` records; the clone dialog groups them by owner.
- `GetPullRequestsAsync` and `CreatePullRequestAsync` list and open pull requests. Set `PullRequest.HeadRef` to the ref that holds a pull
  request's commits on the base repository, such as `refs/pull/12/head`, so **Checkout** works for pull requests from forks.
- `Account` is null while signed out, and `Changed` tells the Git plugin when it signs in or out, or its access changes; the clone dialog
  then loads the repositories again.
- To offer **+ Organization** in the clone dialog, register a command named `<host id>.addOrganization`, such as
  `github.addOrganization`; the dialog shows the chip while the command can run, and loads the list again when the window is activated.
