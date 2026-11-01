# Validation

## November 1, 2026: Unexpected empty directory checks

[PR #8](https://github.com/KENNETH-R-THOMPSON/document-intake-automation/pull/8) extends read-only `verify-exact` to detect unexpected empty destination directories as well as files. It also normalizes the destination path for folder comparison. Two synthetic regression tests cover an extra empty folder and an empty source with a leftover destination folder. Ordinary `verify` remains unchanged, and the checker doesn't delete files or folders.

[PR GitHub Actions run 41356956133](https://github.com/KENNETH-R-THOMPSON/document-intake-automation/actions/runs/41356956133) passed the .NET 10 Release build, **27/27 executable tests**, and sample preview, apply and exact-verify commands on hosted Linux, macOS and Windows. The earlier 25-test result is historical. This evidence comes from PR CI; merged-main CI, owner Mac/NAS testing and real-document verification are not yet verified. Current-source comparison doesn't prove an immutable historical backup, and concurrent filesystem replacement remains a limitation.

## November 1, 2026: Exact destination verification

[Pull request #6](https://github.com/KENNETH-R-THOMPSON/document-intake-automation/pull/6) added opt-in `verify-exact`. It checks current source/copy hashes and rejects unexpected destination files, including nested extras, without deleting or changing files. The original `verify` behavior remains available. Three new tests cover clean copies, extra files, and an absent destination for empty input. The mode does not inspect empty extra directories or establish historical backup integrity.

[PR GitHub Actions run 41351914187](https://github.com/KENNETH-R-THOMPSON/document-intake-automation/actions/runs/41351914187) passed the .NET 10 Release build, all **25/25 executable tests**, and fictional preview, apply and exact-verify CLI steps on each hosted Ubuntu, macOS and Windows runner. The exact-verify sample reported two checked and two matched files on each operating system. The 22-test result below is historical. Merge commit: `14d968b56791728412dd623c9f650ad1faa001b6`.

The owner's Mac, NAS, real documents, hostile filesystem races and historical snapshot verification remain untested. The PR run is verified; merged-main CI for this change has not been independently inspected.

## November 1, 2026: Read-only verification of existing copies

[Pull request #4](https://github.com/KENNETH-R-THOMPSON/document-intake-automation/pull/4) added a `verify` command that compares previously copied destination files against the current source files by length and SHA-256. Three new regression checks cover matching copies, missing copies, and changed source or destination content. It doesn't modify files or check for extra destination files.

The .NET 10 Release build, all **22 tests**, and sample read-only preview passed on each GitHub-hosted Ubuntu, macOS and Windows runner in [PR CI](https://github.com/KENNETH-R-THOMPSON/document-intake-automation/actions/runs/41346686317) and [merged-main CI](https://github.com/KENNETH-R-THOMPSON/document-intake-automation/actions/runs/41346830077). The previous 19-test result is historical.

The owner's Mac and NAS remain untested with this version. The check compares with the current source state and doesn't establish an immutable historical backup or protection against concurrent hostile filesystem changes.


## October 31, 2026: Three operating systems in GitHub Actions

The .NET 10 Release build passed with zero warnings and errors on hosted Ubuntu, Windows, and macOS runners. Each runner completed all 19 executable regression tests and the sample read-only preview. The preview reported two planned files and zero copied files on each system.

[GitHub Actions run with three passing jobs](https://github.com/KENNETH-R-THOMPSON/document-intake-automation/actions/runs/41188592659) | [Pull request #3](https://github.com/KENNETH-R-THOMPSON/document-intake-automation/pull/3)

These are GitHub-hosted tests. They don't verify the latest version on my own Mac, Windows hardware, real document collections, or recovery after disk failures.

## Earlier local checks

Verified September 26, 2026 on macOS ARM64 using .NET SDK 10.0.401.

- Release build: succeeded, zero warnings and zero errors.
- Executable regression suite: 16 of 16 tests passed.
- CLI preview: two sample files planned, zero copies.
- CLI apply: two files copied into data and documents categories.
- Repeated CLI apply: exit code 1 and an existing-target conflict; targets were not overwritten.

The first test run encountered the macOS /var temporary-path symlink. The test harness now uses the physical /private/tmp directory on macOS, preserving the application's rejection of symlinks. Subsequent tests all passed.

Not verified: Windows/Linux runtime behavior, hostile concurrent filesystem changes, very large workloads, disk-full recovery and all-or-nothing batch rollback. Source files and destination copies are regular files; special filesystem objects are outside the supported input scope.

The repository was not uploaded to GitHub as part of this repair. The source archive excludes build outputs and local machine state. .NET SDK is required to build it.
