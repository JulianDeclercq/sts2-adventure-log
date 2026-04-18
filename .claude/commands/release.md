Cut a new release of CombatLog: bump version, commit, tag, build, upload to GitHub Releases.

Argument: $ARGUMENTS
- One of `patch`, `minor`, `major` (semver bump), or an explicit version like `0.2.0` / `v0.2.0`.
- If empty, ask the user to pick.

## Preflight

1. Confirm working tree is clean (`git status` → no uncommitted changes). If dirty, stop and tell the user.
2. Confirm current branch is `main`. If not, stop and tell the user.
3. Confirm `git pull` is up to date with origin (`git fetch && git status -sb`). If behind, pull first.

## Version bump

1. Read current `version` from `CombatLog.json` (e.g. `v0.1.0`).
2. Compute new version:
   - `patch` → `v0.1.0` → `v0.1.1`
   - `minor` → `v0.1.0` → `v0.2.0`
   - `major` → `v0.1.0` → `v1.0.0`
   - Explicit `X.Y.Z` or `vX.Y.Z` → use verbatim (normalize to `vX.Y.Z` with leading `v`).
3. Edit `CombatLog.json`, set `"version"` to the new value.
4. Commit: `git add CombatLog.json && git commit -m "release: <new-version>"`.
5. Tag: `git tag <new-version>`.

## Build and publish

Run `powershell -NoProfile -File Scripts/package-release.ps1 -Publish`. Report its final line (`Zip -> …` / `Release published.`).

The script:
- Builds Release, copies DLL + JSON into `dist/stage/CombatLog/`, zips as `dist/CombatLog-<version>.zip`.
- Calls `gh release create <version> dist/CombatLog-<version>.zip --title "CombatLog <version>" --generate-notes`.

## Push

After the release is published, push the commit and tag:
```
git push origin main
git push origin <new-version>
```

If the direct push to main is blocked by perms, stop and tell the user to merge via a PR branch — don't try workarounds.

## Report

Final one-liner: `Released <new-version> — <zip path> — <release url>`.
If any step fails, stop immediately, report which step + the error, and do NOT try to undo partial state (user can inspect).
