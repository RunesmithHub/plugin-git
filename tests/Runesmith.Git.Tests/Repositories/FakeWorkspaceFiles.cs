using Runesmith.Sdk.Workspace;

namespace Runesmith.Git.Tests.Repositories;

/// <summary>A file watcher whose changes the test reports.</summary>
internal sealed class FakeWorkspaceFiles : IWorkspaceFiles
{
    private readonly List<Subscription> subscriptions = [];

    /// <summary>Gets the folders that are watched now.</summary>
    public IReadOnlyList<string> Folders
    {
        get
        {
            lock (subscriptions)
                return [.. subscriptions.Select(s => s.Folder!)];
        }
    }

    /// <summary>Gets the options of the subscriptions that are active now.</summary>
    public IReadOnlyList<FileWatchOptions> Options
    {
        get
        {
            lock (subscriptions)
                return [.. subscriptions.Select(s => s.Options)];
        }
    }

    public IFileWatcher Watch(IReadOnlyList<string> globs, Action<IReadOnlyList<FileChangeEvent>> onChanged, FileWatchOptions? options = null)
    {
        var subscription = new Subscription(this, options ?? new FileWatchOptions(), onChanged);
        lock (subscriptions)
            subscriptions.Add(subscription);
        return subscription;
    }

    /// <summary>Reports changes to every subscription whose folder holds them.</summary>
    public void Report(params FileChangeEvent[] changes)
    {
        Subscription[] active;
        lock (subscriptions)
            active = [.. subscriptions];
        foreach (var subscription in active)
        {
            var inside = changes.Where(c => c.Path.StartsWith(subscription.Folder!, StringComparison.Ordinal)).ToList();
            if (inside.Count > 0)
                subscription.OnChanged(inside);
        }
    }

    public void Changed(string path) => Report(new FileChangeEvent(FileChangeKind.Changed, path));

    private sealed class Subscription(FakeWorkspaceFiles owner, FileWatchOptions options, Action<IReadOnlyList<FileChangeEvent>> onChanged) : IFileWatcher
    {
        public FileWatchOptions Options => options;

        public Action<IReadOnlyList<FileChangeEvent>> OnChanged => onChanged;

        public string? Folder => options.Folder;

        public bool IsWatching { get; private set; } = true;

        public void Dispose()
        {
            IsWatching = false;
            lock (owner.subscriptions)
                owner.subscriptions.Remove(this);
        }
    }
}
