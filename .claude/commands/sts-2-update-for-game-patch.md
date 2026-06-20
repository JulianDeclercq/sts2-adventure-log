Update AdventureLog after a Slay the Spire 2 game patch: rebuild against the new game dll and statically verify every Harmony patch still resolves — without launching the game.

Use this whenever the game updated and the mod might be stale (crash on load, "Assembly DLL failed to initialize", or just after a known StS2 update).

## Why static verification

`dotnet build` compiles patch *bodies* against the current `sts2.dll`, so any renamed/moved member used in a body fails at build — fix those normally. But Harmony resolves two things at **runtime** that the compiler can't see:

1. **Target method resolution** — the `[HarmonyPatch(typeof(X), name, new[]{ types })]` method name + overload `typeof(...)` array. Mismatch → `ArgumentException: Undefined target method`.
2. **Positional arg types** — `__0`, `__1`, … in the Prefix/Postfix must match the target's real parameter types by position. Mismatch → `InvalidProgramException: Common Language Runtime detected an invalid program`.

These are the failures that only show up when the game loads the mod. Verify them by decompiling, not by running.

## Steps

1. **Build** against the current game dll:
   ```
   dotnet build -c Release
   ```
   The build references `sts2.dll` from the auto-discovered Steam game dir (see `Sts2PathDiscovery.props`). If it fails with `error CS...`, a member was renamed/moved/retyped — fix the body (decompile the type to find the new shape, step 3) and rebuild until it compiles.

   Toolchain note: project targets `net9.0`; needs a .NET 9+ SDK (`dotnet --list-sdks`). `ilspycmd` global tool required (`dotnet tool install -g ilspycmd`; lives at `$HOME/.dotnet/tools/`).

2. **List every patch target.** From `AdventureLogCode/Patches/`:
   ```
   grep -rE "HarmonyPatch\(typeof|public static void (Prefix|Postfix)" AdventureLogCode/Patches/
   ```
   This gives each patch's target class/method + its Prefix/Postfix signature.

3. **Bulk-decompile once, then grep.** A full audit touches ~14 game classes, so decompile the whole dll up front and grep offline rather than spawning a lookup per patch:
   ```
   ./Scripts/decompile-sts2.sh          # -> .decompiled/ (~3,300 .cs, ~18s; skips if fresh)
   grep -rn "public .* CardGenerated" .decompiled/   # find a target method's current signature
   ```
   The script auto-picks the newest publicized dll (Debug or Release).

   Fallback for a single one-off lookup: `ilspycmd ".godot/mono/temp/obj/Release/PublicizedAssemblies/sts2.*/sts2.dll" -t "MegaCrit.Sts2.Core.<Namespace>.<Class>"`.

   For each patch, confirm against the decompiled signature:
   - **Method exists** under that name on that class.
   - **`typeof(...)` overload array** (if present) matches the target's parameter types exactly — the usual breakage is the game inserting a leading param (e.g. `PlayerChoiceContext`) which shifts everything.
   - **`__N` positional types** match the real parameter at position N. If a leading param was inserted, every `__N` shifts by one (`__1`→`__2`, etc.) and the captured values must be re-pointed.
   - First param of `CombatHistory` sinks is `ICombatState` (an interface) — patches must type `__0` as `ICombatState`, not a concrete class.

4. **Fix mismatches**, rebuild (step 1), repeat the audit until every patch is verified.

5. **Report** a table: each patch → `ok` / `fixed: <what changed>`. Then `dotnet build` auto-deploys the dll to `<game>/mods/AdventureLog/`.

6. **One confirming launch** (optional, user-driven): load the mod; main menu should read "Loaded 1 mod" with no errors. Crash log at `%appdata%/SlayTheSpire2/logs/godot.log` (Harmony reports one failed patch at a time — if one slips through, the log names it; fix and repeat).

## Then

If this was for a workshop release, the mod still serves the old dll until re-uploaded — offer to bump version (`AdventureLog.json`) and run `/sts-2-release` or the ModUploader flow.
