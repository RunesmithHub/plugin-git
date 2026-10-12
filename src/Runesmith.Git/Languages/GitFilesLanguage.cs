using System.Composition;
using Runesmith.Sdk.Languages;

namespace Runesmith.Git.Languages;

/// <summary>Ignore: its files, highlighting, comments and brackets.</summary>
public sealed class GitFilesLanguage
{
    [Export(typeof(LanguageDefinition))]
    public LanguageDefinition Ignore { get; } = new("ignore", "Ignore")
    {
        Extensions = [".gitignore_global", ".gitignore"],
        FileNames = [".gitignore", ".dockerignore", ".npmignore"],
        ScopeName = "source.ignore",
        LineComment = "#",
        Brackets = [('(', ')'), ('[', ']'), ('{', '}')],
        Icon = "settings",
    };

    [Export(typeof(GrammarDefinition))]
    public GrammarDefinition IgnoreGrammar { get; } = GrammarFiles.Find("source.ignore", "git/ignore.tmLanguage.json");
}
