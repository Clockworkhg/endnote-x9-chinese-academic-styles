# v0.5.0 release gate

Status: implementation in progress, NOT approved for stable release.

- [ ] Windows self-contained EXE builds.
- [ ] Real main window, directory dialog, update dialog render and process events.
- [ ] Custom path persistence, corrupt settings fallback, protected path rejection.
- [ ] Install, reinstall backup, managed-only uninstall, user-modified files preserved.
- [ ] SHA-256 mismatch prevents executable replacement.
- [ ] Update download cancellation and offline errors preserve the running version.
- [ ] Successful replacement and GUI health acknowledgement on Windows.
- [ ] Failed new GUI launch restores old executable and restarts it.
- [ ] File-lock failure preserves a usable executable and recovery instructions.
- [ ] 100%, 125%, 150%, 200% display scaling review.
- [ ] EndNote X9 + Word/WPS sample-document regression (not implied by GUI CI).
- [ ] Stable / unverified / experimental style status and source notices reviewed.
- [ ] Release asset is the exact tested executable, checksum and test report attached.

Directory candidates are not proof that EndNote reads a directory. Manual confirmation
does not change EndNote preferences. Existing user-modified and untracked styles are
preserved by uninstall. Updates preserve settings and do not update installed ENS files
without a separate installation action.

The existing locally modified Chicago asset and old ZIP files are unrelated and must
not be included in this change.
