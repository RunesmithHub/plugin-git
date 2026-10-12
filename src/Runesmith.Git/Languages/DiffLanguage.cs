using System.Composition;
using Runesmith.Sdk.Languages;

namespace Runesmith.Git.Languages;

/// <summary>Diff: its files, highlighting, comments and brackets.</summary>
public sealed class DiffLanguage
{
    [Export(typeof(LanguageDefinition))]
    public LanguageDefinition Diff { get; } = new("diff", "Diff")
    {
        Extensions = [".diff", ".patch", ".rej"],
        ScopeName = "source.diff",
        LineComment = "#",
        BlockComment = ("#", " "),
        Brackets = [('{', '}'), ('[', ']'), ('(', ')')],
        Icon = "git-compare",
    };

    [Export(typeof(GrammarDefinition))]
    public GrammarDefinition DiffGrammar { get; } = GrammarFiles.Find("source.diff", "diff/diff.tmLanguage.json");
}
