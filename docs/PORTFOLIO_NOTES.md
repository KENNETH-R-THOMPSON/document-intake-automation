# Portfolio notes

## Resume bullets

- Developed a C# document intake utility with read-only previews, configurable extension rules, and structured JSON results.
- Added SHA-256 copy verification, overwrite protection, path validation, and a regression test harness covering filesystem error cases.

Use these bullets only after reviewing the code and being able to explain the implementation and your contribution.

- Checked custom JSON rules for duplicate extension keys and invalid category types, with automated regression coverage.

## LinkedIn Projects draft (unpublished)

I worked on an AI-assisted C#/.NET 10 command-line project that organizes fictional files into folders. I added read-only copy verification and an optional exact mode that catches unexpected destination files. I wrote tests for changed copies, missing copies and nested extra files. All 25 tests and the sample CLI checks passed on hosted Linux, macOS and Windows. I haven't tested it on my Mac or NAS or used it with private documents.

Skills: C#, .NET, SHA-256, JSON, file handling, regression testing, GitHub Actions.
Public Featured candidate: https://github.com/KENNETH-R-THOMPSON/document-intake-automation . LinkedIn publication remains unverified.

## Five-minute walkthrough

1. Explain the problem: incoming files need repeatable folder classification.
2. Run preview on the fictional sample and show that the output folder does not exist.
3. Run apply and inspect the two categorized copies and unchanged originals.
4. Run verify to compare the copied files with the current sources. Add a fictional extra destination file and show that verify-exact rejects it without deleting anything.
5. Repeat apply to demonstrate overwrite protection.
6. Run the test harness. Explain per-file atomic rename, partial batch failure, and why file extensions do not prove document content.

## Design choices

A command line interface keeps the workflow scriptable. JSON results can feed later reporting tools. Standard-library-only dependencies reduce setup. Source files are copied rather than moved to preserve the intake originals. Original subfolders remain part of the target path to avoid flattening collisions.

## Further work

- Saved plan files with explicit review and stale-plan checks.
- A readable HTML report alongside JSON.
- Batch rollback or resumable operation with a verified journal.
- Test the current version on the owner's Mac and a safe isolated NAS folder.
- File type validation and documented maximum file/count limits.
