# Git for Runesmith

The official Git plugin for [Runesmith](https://github.com/RunesmithHub/Runesmith), `runesmith.git`: the Commit window, branches, fetch,
pull and push, the log and its graph, diffs, change markers in the editor, cloning, links to hosting services and pull requests. It runs
the `git` command line, so your configuration, hooks, signing keys, SSH setup and credential helpers keep working. Hosting services such as
GitHub, Gitea and Forgejo are plugins on top of it.

The [Git documentation](https://docs.runesmith.dev/plugins/git) describes each part and its settings.

## Capabilities

| Capability | Why |
| --- | --- |
| `network` | Shows the avatars of the people behind commits and pull requests, from any server your hosting plugins use. |
| `process` | Runs your `git`, and opens links in your browser. |
| `filesystem` | Watches your repositories, edits `.gitignore` and caches avatars. |
| `environment` | Suggests a folder in your home folder to clone into. |

## Install

Install it from Runesmith's plugin manager, or from [hub.runesmith.dev](https://hub.runesmith.dev). Git must be installed and on the `PATH`.

## Translations

The plugin's text is in English in `src/Runesmith.Git/locales/en/runesmith.git.ftl`, a [Fluent](https://projectfluent.org) file. Text for
a new message goes there, and the code reads it from the generated `Strings` class. A language pack translates the file; see
[Language packs](https://docs.runesmith.dev/plugins/language-packs). Git's own messages are shown as Git writes them.

## Build and test

You need the .NET SDK that `global.json` names. Runesmith's packages come from the plugin hub's feed, which `nuget.config` sets up.

```bash
dotnet build plugin-git.slnx
dotnet test --solution plugin-git.slnx
dotnet build src/Runesmith.Git -t:InstallPlugin
```

`InstallPlugin` copies the built plugin into your Runesmith plugins folder; restart Runesmith to load it. [CONTRIBUTING.md](CONTRIBUTING.md)
has the checks a change must pass, and the [Runesmith documentation](https://docs.runesmith.dev/) covers writing plugins.

## License

[Apache License 2.0](LICENSE).
