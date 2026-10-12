using System.Composition;
using Runesmith.Git.Git;
using Runesmith.Sdk.Templates;

namespace Runesmith.Git.Projects;

/// <summary>The Create Git repository toggle of the New Project dialog: makes the new project's folder a repository with
/// <c>git init</c>. The dialog offers it while Git is installed.</summary>
[Export(typeof(IProjectCreationStep))]
internal sealed class CreateRepositoryStep : IProjectCreationStep
{
    public string Id => "git.init";

    public string Title => Strings.ProjectCreateRepository;

    public bool IsAvailable => IsOnPath(GitProcess.Executable);

    public async Task RunAsync(ProjectCreationResult project, IProgress<string> progress, CancellationToken cancellationToken)
    {
        progress.Report(Strings.ProjectCreatingRepository);
        try
        {
            await GitProcess.RunCheckedAsync(project.Folder, ["init"], cancellationToken).ConfigureAwait(false);
        }
        catch (GitException exception)
        {
            throw new InvalidOperationException(exception.Message, exception);
        }
    }

    private static bool IsOnPath(string program)
    {
        if (Path.IsPathRooted(program))
            return File.Exists(program);

        string[] names = OperatingSystem.IsWindows() && !Path.HasExtension(program) ? [program + ".exe", program + ".cmd"] : [program];
        return (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Any(folder => names.Any(name => File.Exists(Path.Combine(folder, name))));
    }
}
