using System.Composition;
using Runesmith.Sdk.Workspace;

namespace Runesmith.Git.Repositories;

/// <summary>Leaves the folder Git keeps a repository in out of the Explorer, Quick Open and search.</summary>
[Export(typeof(IFileExcludeContributor))]
internal sealed class GitFolders : IFileExcludeContributor
{
    public IEnumerable<string> Globs => ["**/.git"];
}
