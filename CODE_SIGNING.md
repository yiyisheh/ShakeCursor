# Code signing policy

Free code signing provided by [SignPath.io](https://about.signpath.io/), certificate by [SignPath Foundation](https://signpath.org/).

> Status: ShakeCursor has applied to the SignPath Foundation open-source program. Releases published before the application is approved are not signed.

## What gets signed

- Only `ShakeCursor.exe` and `ShakeCursor.msi`, built from the source code in this repository.
- Every release is built by [GitHub Actions](.github/workflows/build.yml) on GitHub-hosted runners from a tagged commit, and each signing request is approved by hand.
- No third-party binaries are included or signed.

## Team roles

| Role | Members |
|---|---|
| Committers and reviewers | [yiyisheh](https://github.com/yiyisheh) |
| Approvers | [yiyisheh](https://github.com/yiyisheh) |

## Privacy policy

This program will not transfer any information to other networked systems unless specifically requested by the user or the person installing or operating it.

ShakeCursor does not connect to the internet at all. Its settings stay on your PC in `%APPDATA%\ShakeCursor\config.json`.
