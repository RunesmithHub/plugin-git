using Runesmith.Sdk.Workspace;

namespace Runesmith.Git.Repositories;

/// <summary>Follows a repository's Git folder for the files whose changes change its status: the index, HEAD, the references and the
/// configuration. Changes to the working tree come from the workspace itself.</summary>
/// <remarks>The changes come from Runesmith's file watcher, which watches the open folder once for everyone.</remarks>
internal sealed class RepositoryWatcher : IDisposable
{
    private static readonly HashSet<string> TopFiles = new(StringComparer.Ordinal)
    {
        "index", "HEAD", "packed-refs", "config", "MERGE_HEAD", "CHERRY_PICK_HEAD", "REVERT_HEAD", "ORIG_HEAD", "FETCH_HEAD",
    };

    private static readonly string[] StatusGlobs = [.. TopFiles, "rebase-*", "rebase-*/**", "refs/**"];

    private IFileWatcher? watcher;

    /// <summary>Starts following a Git folder.</summary>
    /// <param name="changed">Called, on a background thread, when something that changes the status changed.</param>
    /// <param name="indexChanged">Called, on a background thread, when the index or HEAD changed.</param>
    public RepositoryWatcher(IWorkspaceFiles files, string gitDirectory, Action changed, Action indexChanged)
    {
        watcher = Watch(files, gitDirectory, StatusGlobs, events =>
        {
            var (status, index) = Classify(gitDirectory, events);
            if (status)
                changed();
            if (index)
                indexChanged();
        });
    }

    private RepositoryWatcher()
    {
    }

    /// <summary>Follows a folder that is not a repository for a <c>.git</c> to appear in it, such as from <c>git init</c> in a terminal.</summary>
    public static RepositoryWatcher ForMissingRepository(IWorkspaceFiles files, string folder, Action created)
    {
        var expected = Path.Combine(folder, ".git");
        return new RepositoryWatcher
        {
            watcher = Watch(files, folder, [".git"], events =>
            {
                if (events.Any(e => e.Kind == FileChangeKind.Rescan || (e.Kind != FileChangeKind.Deleted && SamePath(e.Path, expected))))
                    created();
            }),
        };
    }

    /// <summary>Says whether a batch of changes in a Git folder changes the repository's status, and whether the index or HEAD changed.</summary>
    internal static (bool Status, bool Index) Classify(string gitDirectory, IReadOnlyList<FileChangeEvent> events)
    {
        var (status, index) = (false, false);
        foreach (var change in events)
        {
            if (change.Kind == FileChangeKind.Rescan)
                return (true, true);

            // Git writes each of these files as a .lock file first, which says nothing until it is renamed.
            var name = Path.GetRelativePath(gitDirectory, change.Path).Replace('\\', '/');
            if (name.EndsWith(".lock", StringComparison.Ordinal))
                continue;

            if (TopFiles.Contains(name) || name.StartsWith("rebase-", StringComparison.Ordinal) || name.StartsWith("refs/", StringComparison.Ordinal))
            {
                status = true;
                index |= name is "index" or "HEAD";
            }
        }

        return (status, index);
    }

    public void Dispose()
    {
        watcher?.Dispose();
        watcher = null;
    }

    private static IFileWatcher? Watch(IWorkspaceFiles files, string folder, IReadOnlyList<string> globs, Action<IReadOnlyList<FileChangeEvent>> onChanged)
    {
        try
        {
            // The Git folder is among the files Runesmith leaves out of the workspace.
            return files.Watch(globs, onChanged, new FileWatchOptions { Folder = folder, IncludeExcluded = true });
        }
        catch (Exception exception) when (exception is IOException or ArgumentException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static bool SamePath(string left, string right) =>
        string.Equals(Path.TrimEndingDirectorySeparator(left), Path.TrimEndingDirectorySeparator(right), OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
}
