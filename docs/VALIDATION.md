# Validation

Verified September 26, 2026 on macOS ARM64 using .NET SDK 10.0.401.

- Release build: succeeded, zero warnings and zero errors.
- Executable regression suite: 16 of 16 tests passed.
- CLI preview: two sample files planned, zero copies.
- CLI apply: two files copied into data and documents categories.
- Repeated CLI apply: exit code 1 and an existing-target conflict; targets were not overwritten.

The first test run encountered the macOS /var temporary-path symlink. The test harness now uses the physical /private/tmp directory on macOS, preserving the application's rejection of symlinks. Subsequent tests all passed.

Not verified: Windows/Linux runtime behavior, hostile concurrent filesystem changes, very large workloads, disk-full recovery and all-or-nothing batch rollback. Source files and destination copies are regular files; special filesystem objects are outside the supported input scope.

The repository was not uploaded to GitHub as part of this repair. The source archive excludes build outputs and local machine state. .NET SDK is required to build it.
