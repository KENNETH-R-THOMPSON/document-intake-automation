# Document Intake Automation

[![.NET 10 CI](https://github.com/KENNETH-R-THOMPSON/document-intake-automation/actions/workflows/dotnet-ci.yml/badge.svg)](https://github.com/KENNETH-R-THOMPSON/document-intake-automation/actions/workflows/dotnet-ci.yml)

A C# command-line utility that sorts copies of incoming files into folders using extension rules. Preview is read-only. Applying a plan copies files, verifies each copy with SHA-256, and never deletes originals or overwrites existing targets.

This is an independent personal portfolio project. It does not modify the ITAM team project or school assignments.

## Requirements

.NET 10 SDK. No third-party NuGet packages are required. Earlier testing used macOS ARM64. On October 31, 2026, GitHub Actions passed the Release build, all 19 tests, and the sample preview on hosted Ubuntu, Windows, and macOS runners. The current version has not been retested on my own Mac.

## Quick start

Run from this folder:

```sh
dotnet run --project src/DocumentIntake -- preview sample/inbox sample-output
dotnet run --project src/DocumentIntake -- apply sample/inbox sample-output
dotnet run --project src/DocumentIntake -- verify sample/inbox sample-output
dotnet run --project tests/DocumentIntake.Tests
```

The sample has two fictional files. Preview prints a JSON plan without creating the destination. Apply creates `sample-output/documents/readme.txt` and `sample-output/data/inventory.csv`. Running apply a second time reports a conflict, preserving those files. Verify checks the existing copies against the current source contents without writing anything. It reports the checked and matched counts, or exits with code 1 if a copy is missing or differs.

Optional rules replace the defaults:

```sh
dotnet run --project src/DocumentIntake -- preview sample/inbox sample-output sample/rules.json
```

Rules map extensions to single folder names. Extensions are case-insensitive. Unknown extensions use `other`. Rules JSON must be an object with string categories; repeated extension keys, including different capitalization, are rejected. Nested source paths remain nested within their category, so equal names in different source folders stay separate.

Output includes mode, planned/copied counts, source/destination paths, byte counts and SHA-256 fingerprints. Exit code 0 means success, 1 means input or filesystem failure, and 2 means incorrect command syntax. Diagnostics go to stderr.

## Build and test

```sh
dotnet build src/DocumentIntake -c Release
dotnet run --project tests/DocumentIntake.Tests -c Release
```

The test project is a small executable test runner. Use `dotnet run`, not `dotnet test`. It exits nonzero on failure and covers preview side effects, content preservation, classification, nested paths, conflicts, duplicate or invalid JSON rules, missing input, symbolic links, and verification of copied files.

## JetBrains use

Open the project folder in Rider for C# editing and debugging. In IntelliJ IDEA, open the folder and run the commands in its terminal. This package does not assume IntelliJ supplies a C# debugger.

## Design and limits

- Plan all files and reject existing target conflicts before copying any file.
- Stream source data and compute SHA-256 without loading entire files into memory.
- Copy to a unique temporary file beside the target. Verify its digest before renaming without overwrite.
- Refuse source/destination overlap, unsafe categories, and symbolic links/reparse points.
- Work on stable, trusted folders. Checks do not defeat another process maliciously replacing paths between validation and copying.
- Apply is atomic per file, not for the entire batch. Disk errors can leave earlier verified copies and empty directories. The error explains this; source files remain untouched.
- Preview, apply, and verify generate separate plans. Apply may include files added after preview; there is no saved-plan approval mechanism.
- Verify compares destination files against source contents at verification time. It doesn't detect extra destination files or prove that a previous backup snapshot is unchanged.
- Classification uses extensions, not file contents. There is no OCR, malware scanning, document authenticity check, or sensitive-data classification.
- Empty directories, ACLs, extended attributes and timestamps are not preserved. Special filesystem objects such as FIFOs are outside the supported regular-file input scope.
- Source/destination ancestry comparison is conservatively case-insensitive. This can reject paths that are distinct on a case-sensitive filesystem.

See `docs/PORTFOLIO_NOTES.md` for résumé bullets and an interview walkthrough, and `docs/VALIDATION.md` for measured results.
