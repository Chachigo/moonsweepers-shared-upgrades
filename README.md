# Moon Sweepers – Shared Upgrades

A [BepInEx 5](https://github.com/BepInEx/BepInEx) mod for **Moon Sweepers** co-op: tools, upgrades and vehicle upgrades bought by one player are shared with the whole crew. Nobody has to buy the same thing twice.

**Only the host needs the mod.** Clients can play on the vanilla game.

## What gets shared

| | Shared? | How |
|---|---|---|
| Tools (brooms, etc.) | ✅ | Everyone owns a tool once anyone owns it |
| Tool container tiers | ✅ | Everyone gets the best tier owned by anyone |
| Gear tree (belt slots, gravity boots, jetpack, …) | ✅ | Everyone gets the best level owned by anyone |
| Vehicle upgrades | ✅ | All vehicles of the same model get the best level of each upgrade |
| Money | — | Already a shared team wallet in vanilla |

Purchases made **before** installing the mod are shared too.

## How it works

The host's game is the authority for purchases and periodically sends each player's upgrade state to clients. The mod patches `MoonWorld` on the host with Harmony:

- `ToolLevel`, `ContainerTier` and `TreeLevel` return the best level across all players, so the host validates purchases against the shared state.
- `PlayerUpgradeState`, which is what gets sent to clients, returns the merged state, and `UpgradePlayers` includes every lobby member. Vanilla clients resync to it within a few seconds.
- `OwnedChanged` aligns the upgrades of all vehicles of the same model. Vanilla code then sends the vehicle list to clients.

Tool and gear sharing does not touch the save file: remove the mod and everyone is back to their own purchases. Vehicle upgrade sharing **is** written to the save.

## Install

1. Download [BepInEx 5 (win x64)](https://github.com/BepInEx/BepInEx/releases) and extract it into the game folder (next to `MoonSweepers.exe`).
2. Copy `SharedUpgrades.dll` into `BepInEx/plugins/`.
3. **Linux / Steam Deck (Proton) only:** set the Steam launch options to
   ```
   WINEDLLOVERRIDES="winhttp=n,b" %command%
   ```
4. Start the game. `BepInEx/LogOutput.log` should contain `Loading [Shared Upgrades …]`.

To uninstall, delete `winhttp.dll` from the game folder, or just the plugin DLL.

## Build

You need the .NET SDK (8+) and the game installed.

```sh
dotnet build -c Release -p:GameDir="/path/to/Moon Sweepers"
```

Game assemblies are referenced from `GameDir`, and BepInEx/Harmony from `lib/`. If `GameDir/BepInEx` exists, the build copies the DLL straight into `BepInEx/plugins/`.

`launch.sh` is a personal Linux launcher. It adds the `winhttp` override to the Proton prefix and starts the game through Steam. Its paths are hardcoded, so edit them before you use it.

## Caveats

- Tested against game build Unity 6000.6. Game updates may break the patches.
- Shared items can take a few seconds to show up on clients after a purchase.
- Only a vehicle's owner can buy upgrades for it. The result then applies to every vehicle of that model.

## License

[MIT](LICENSE) © Chachigo.

`lib/` bundles third-party binaries, which are used only as build references:
- `BepInEx.dll` – [BepInEx](https://github.com/BepInEx/BepInEx), LGPL-2.1
- `0Harmony.dll` – [HarmonyX](https://github.com/BepInEx/HarmonyX), MIT

This is an unofficial fan mod. It is not affiliated with the developers of Moon Sweepers.
