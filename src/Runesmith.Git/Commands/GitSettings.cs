using System.Composition;
using Runesmith.Git.Git;
using Runesmith.Sdk.Settings;

namespace Runesmith.Git.Commands;

/// <summary>The Git settings: how pulls bring in commits, how often to fetch, and the commit subject's length hint.</summary>
[Export(typeof(ISettingContributor))]
internal sealed class GitSettings : ISettingContributor
{
    /// <summary>How Update Project pulls: <c>gitConfig</c>, <c>rebase</c> or <c>merge</c>.</summary>
    public const string PullStrategy = "git.pullStrategy";

    /// <summary>Minutes between automatic fetches; 0 turns them off.</summary>
    public const string AutoFetchMinutes = "git.autoFetchMinutes";

    /// <summary>The length past which the commit message box points out a long subject.</summary>
    public const string SubjectLength = "git.subjectLengthHint";

    private const string Category = "Git";

    public IEnumerable<SettingDefinition> Settings =>
    [
        new(PullStrategy, Strings.SettingPullStrategy, Category, "gitConfig")
        {
            Description = Strings.SettingPullStrategyDescription,
            Choices = ["gitConfig", "rebase", "merge"],
            ChoiceTitles = [Strings.SettingPullStrategyGitConfig, Strings.SettingPullStrategyRebase, Strings.SettingPullStrategyMerge],
        },
        new(AutoFetchMinutes, Strings.SettingAutoFetch, Category, 0)
        {
            Description = Strings.SettingAutoFetchDescription,
            Minimum = 0,
            Maximum = 1440,
        },
        new(SubjectLength, Strings.SettingSubjectLength, Category, 72)
        {
            Description = Strings.SettingSubjectLengthDescription,
            Minimum = 20,
            Maximum = 200,
        },
    ];

    /// <summary>Reads the pull strategy setting's value.</summary>
    public static PullStrategy ParsePullStrategy(string? value) => value switch
    {
        "rebase" => Git.PullStrategy.Rebase,
        "merge" => Git.PullStrategy.Merge,
        _ => Git.PullStrategy.UserConfig,
    };
}
