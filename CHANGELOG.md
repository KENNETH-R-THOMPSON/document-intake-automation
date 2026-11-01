# Changelog

## 2026-11-01

Extended read-only `verify-exact` to report unexpected empty destination folders as well as extra files. Added two synthetic regression tests. Normal `verify` still checks only expected copies. The checker doesn't remove unexpected entries.

## 2026-10-31

Changed custom rule parsing to reject repeated JSON extension keys and non-string category values. Added three regression cases for duplicate keys, invalid category types, and a non-object root. This prevents a later JSON property from silently replacing an earlier rule.

## 2026-09-26

Rebuilt the previously undelivered Document Intake Automation package as a .NET 10 command-line project. Added preview/apply modes, extension rules, JSON output, streamed SHA-256 verification, overwrite protection, nested-path preservation, and 16 executable regression tests. Added sample data, setup instructions and portfolio notes. This is a reconstruction; no earlier source package was found in the current local portfolio folder.
