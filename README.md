# Equip All Tools

Equip All Tools is a BepInEx/Harmony mod for Hollow Knight: Silksong that lets blue defense tools and yellow exploration tools count as equipped without using crest slots.

## Requirements

- BepInEx 5
- Save Scoped Config

## Install

Install both DLLs as BepInEx plugins:

```text
BepInEx/plugins/SaveScopedConfig/SaveScopedConfig.dll
BepInEx/plugins/EquipAllTools/EquipAllTools.dll
```

During development this project may still build `EquipAllTools.dll` into `BepInEx/scripts` for ScriptEngine reloads.

## Config

Settings appear under Equip All Tools in Configuration Manager.

Global settings apply to every save:

```text
Global: General
Global: Defense (Blue)
Global: Exploration (Yellow)
```

Save-specific settings apply only to the active save:

```text
Save 1: General
Save 1: Defense (Blue)
Save 1: Exploration (Yellow)
```

Save-specific settings are provided by Save Scoped Config, so only the currently loaded save is shown.

### General Settings

- `Enabled`: turns Equip All Tools on for the global scope or active save.
- `Bench Requirement`: when enabled, Equip All Tools hotkey toggles require a bench. When disabled, hotkey toggles can be used anywhere. Normal crest management still follows the game's bench rules.
- `Magnetite Brooch Pulls Shards`: global-only option that lets Magnetite Brooch pull shell shards as well as rosaries. Enabled by default.
- `Debug Logging`: global-only troubleshooting logs for hotkey decisions and save config changes.

Global settings and save settings combine. If either global or active-save `Enabled` is on, the mod can force tools. For individual tools, a global toggle applies to every save, while a save toggle applies only to the active save.

## In-Game Hotkeys

Open the inventory tool screen, hold one of these modifiers, and press a supported blue/yellow tool:

- `Left Shift`
- `Right Shift`
- controller `JoystickButton8`

The hotkey toggles the active save's forced setting for that tool. It does not change global settings.

If the tool is already equipped in a crest slot or floating slot, the hotkey will not force-toggle it. Any save-specific force setting for that normally equipped tool is cleared instead.

If the tool is only being forced by the mod, toggling it on or off uses the inventory equip feedback. Tools forced by the mod show the game's blue/yellow equipped indicator in a dimmed state.

## Special Cases

- `Dead Bugs Purse` is only active for Normal saves.
- `Shell Satchel` is only active for Steel Soul and Steel Soul Dead saves.
- If both are enabled globally, only the one valid for the active save counts.
- `Druids Eye` becomes `Druids Eyes` when the upgraded item is unlocked normally.
- `Claw Mirror` becomes `Claw Mirrors` when the upgraded item is unlocked normally.

The upgrade rows keep the base config key, so existing settings carry forward when the upgraded item is collected.

## Build

From the game directory:

```powershell
powershell.exe -ExecutionPolicy Bypass -File .\ModDev\EquipAllTools\build.ps1
```

The default build target is development mode:

```text
BepInEx/scripts/EquipAllTools.dll
```

For a normal BepInEx plugin install:

```powershell
powershell.exe -ExecutionPolicy Bypass -File .\ModDev\EquipAllTools\build.ps1 -Target Plugin
```

That outputs:

```text
BepInEx/plugins/EquipAllTools/EquipAllTools.dll
BepInEx/plugins/SaveScopedConfig/SaveScopedConfig.dll
```

## Save Scoped Config

Equip All Tools depends on Save Scoped Config. The API stores one config set per save slot, while exposing the active save type separately for Normal/Steel Soul-specific behavior.

Use the Save Scoped Config README for API usage when building other mods.
