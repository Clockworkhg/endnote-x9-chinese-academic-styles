# v0.5.0 release gate

Status: user accepted current application and multilingual trial on 2026-09-05 and explicitly authorized stable publication. Release workflow must pass before publishing.

Unchecked items below remain coverage limitations, not claims of failure. User acceptance supersedes the earlier blanket release hold.

- [x] Windows self-contained EXE builds.
- [x] Real main window, directory dialog, update dialog render and process events.
- [x] Custom path persistence, corrupt settings fallback, protected path rejection.
- [x] Install, reinstall backup, managed-only uninstall, user-modified files preserved.
- [x] SHA-256 mismatch prevents executable replacement.
- [x] Update download cancellation and offline errors preserve the running version.
- [x] Successful replacement and GUI health acknowledgement on Windows.
- [x] Failed new GUI launch restores old executable and restarts it.
- [x] File-lock failure preserves a usable executable and recovery instructions.
- [ ] All 100%, 125%, 150%, 200% scaling combinations: not exhaustively verified.
- [x] User confirmed EndNote X9/WPS trial and multilingual output after importing custom types.
- [ ] Separate Word and all style/type combinations: not exhaustively verified.
- [x] Stable / unverified / experimental style status and source notices reviewed.
- [ ] Release workflow must attach the exact binary it tests, checksum and report.

Directory candidates are not proof that EndNote reads a directory. Manual confirmation
does not change EndNote preferences. Existing user-modified and untracked styles are
preserved by uninstall. Updates preserve settings and do not update installed ENS files
without a separate installation action.

The existing locally modified Chicago asset and old ZIP files are unrelated and must
not be included in this change.
