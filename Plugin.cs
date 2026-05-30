using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GlobalEnums;
using GlobalSettings;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using WheresWolfgang.SaveScopedConfig;

namespace EquipAllTools
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(SaveScopedConfigPlugin.PluginGuid)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "whereswolfgang.equipalltools";
        public const string PluginName = "Equip All Tools";
        public const string PluginVersion = "1.2.2";

        private const int FirstBlueYellowToolIndex = 29;
        private const float ForcedInventoryCueAlpha = 0.5f;
        private static bool isCleaningNormallyEquippedTools;
        private static readonly HashSet<string> FracturedMaskVirtualRefills = new HashSet<string>();

        private static readonly Harmony Harmony = new Harmony(PluginGuid);
        private static readonly System.Reflection.MethodInfo ToolItemManagerIsCustomToolOverrideGetter = AccessTools.PropertyGetter(typeof(ToolItemManager), "IsCustomToolOverride");
        private static readonly System.Reflection.MethodInfo ShouldSuppressCustomToolOverrideForForcedQuickSlingMethod = AccessTools.Method(typeof(Plugin), "ShouldSuppressCustomToolOverrideForForcedQuickSling");
        private static readonly System.Reflection.MethodInfo ShouldSuppressCustomToolOverrideForForcedPollipPouchMethod = AccessTools.Method(typeof(Plugin), "ShouldSuppressCustomToolOverrideForForcedPollipPouch");
        private static readonly System.Reflection.MethodInfo ClearToolCacheMethod = AccessTools.Method(typeof(ToolItemManager), "ClearToolCache");
        private static readonly System.Reflection.MethodInfo InventoryToolUpdateEquippedDisplayMethod = AccessTools.Method(typeof(InventoryItemTool), "UpdateEquippedDisplay");
        private static readonly System.Reflection.MethodInfo InventoryToolManagerPlayMoveSoundMethod = AccessTools.Method(typeof(InventoryItemToolManager), "PlayMoveSound");
        private static readonly System.Reflection.MethodInfo InventoryToolManagerCanChangeEquipsMethod = AccessTools.Method(typeof(InventoryItemToolManager), "CanChangeEquips", new System.Type[0]);
        private static readonly System.Reflection.MethodInfo InventoryToolManagerGetToolTypeColorMethod = AccessTools.Method(typeof(InventoryItemToolManager), "GetToolTypeColor");
        private static readonly System.Reflection.MethodInfo InventoryToolCrestListGetSlotsMethod = AccessTools.Method(typeof(InventoryToolCrestList), "GetSlots");
        private static readonly System.Reflection.MethodInfo InventoryFloatingToolSlotsGetSlotsMethod = AccessTools.Method(typeof(InventoryFloatingToolSlots), "GetSlots");
        private static readonly System.Reflection.FieldInfo InventoryToolManagerField = AccessTools.Field(typeof(InventoryItemTool), "manager");
        private static readonly System.Reflection.FieldInfo InventoryToolManagerCrestListField = AccessTools.Field(typeof(InventoryItemToolManager), "crestList");
        private static readonly System.Reflection.FieldInfo InventoryToolManagerExtraSlotsField = AccessTools.Field(typeof(InventoryItemToolManager), "extraSlots");
        private static readonly System.Reflection.FieldInfo InventoryToolSelectedIndicatorField = AccessTools.Field(typeof(InventoryItemTool), "selectedIndicator");
        private static readonly System.Reflection.FieldInfo InventoryToolManagerCrestEquipMsgField = AccessTools.Field(typeof(InventoryItemToolManager), "crestEquipMsg");
        private static readonly System.Type TextMeshProType = AccessTools.TypeByName("TMProOld.TextMeshPro");
        private static readonly System.Type NestedFadeGroupSpriteRendererType = AccessTools.TypeByName("TeamCherry.NestedFadeGroup.NestedFadeGroupSpriteRenderer");
        private static readonly System.Reflection.PropertyInfo NestedFadeGroupSpriteRendererColorProperty = AccessTools.Property(NestedFadeGroupSpriteRendererType, "Color");
        private static readonly System.Reflection.FieldInfo CurrencyObjectPopupNameField = AccessTools.Field(typeof(CurrencyObjectBase), "popupName");
        private static readonly System.Reflection.FieldInfo ToolStatusToolField = AccessTools.Field(typeof(ToolItemManager.ToolStatus), "tool");
        private static readonly System.Reflection.FieldInfo InventoryToolManagerCurrentToolCountField = AccessTools.Field(typeof(InventoryItemToolManager), "currentToolCount");
        private const string ToolBenchMessage = "Equip Tools while resting at a bench.";
        private static readonly ToolDefinition[] Tools =
        {
            new ToolDefinition("Defense (Blue)", "Druids Eye", 0, SaveAvailability.Any, "Druids Eyes", 1),
            new ToolDefinition("Defense (Blue)", "Magma Bell", 2, SaveAvailability.Any),
            new ToolDefinition("Defense (Blue)", "Warding Bell", 3, SaveAvailability.Any),
            new ToolDefinition("Defense (Blue)", "Pollip Pouch", 4, SaveAvailability.Any),
            new ToolDefinition("Defense (Blue)", "Fractured Mask", 5, SaveAvailability.Any),
            new ToolDefinition("Defense (Blue)", "Multibinder", 6, SaveAvailability.Any),
            new ToolDefinition("Defense (Blue)", "Weavelight", 7, SaveAvailability.Any),
            new ToolDefinition("Defense (Blue)", "Sawtooth Circlet", 8, SaveAvailability.Any),
            new ToolDefinition("Defense (Blue)", "Injector Band", 9, SaveAvailability.Any),
            new ToolDefinition("Defense (Blue)", "Spool Extender", 10, SaveAvailability.Any),
            new ToolDefinition("Defense (Blue)", "Reserve Bind", 11, SaveAvailability.Any),
            new ToolDefinition("Defense (Blue)", "Claw Mirror", 12, SaveAvailability.Any, "Claw Mirrors", 13),
            new ToolDefinition("Defense (Blue)", "Memory Crystal", 14, SaveAvailability.Any),
            new ToolDefinition("Defense (Blue)", "Snitch Pick", 15, SaveAvailability.Any),
            new ToolDefinition("Defense (Blue)", "Volt Filament", 16, SaveAvailability.Any),
            new ToolDefinition("Defense (Blue)", "Quick Sling", 17, SaveAvailability.Any),
            new ToolDefinition("Defense (Blue)", "Wreath of Purity", 18, SaveAvailability.Any),
            new ToolDefinition("Defense (Blue)", "Longclaw", 19, SaveAvailability.Any),
            new ToolDefinition("Defense (Blue)", "Wispfire Lantern", 20, SaveAvailability.Any),
            new ToolDefinition("Defense (Blue)", "Egg of Flealia", 21, SaveAvailability.Any),
            new ToolDefinition("Defense (Blue)", "Pin Badge", 22, SaveAvailability.Any),
            new ToolDefinition("Exploration (Yellow)", "Compass", 23, SaveAvailability.Any),
            new ToolDefinition("Exploration (Yellow)", "Shard Pendant", 24, SaveAvailability.Any),
            new ToolDefinition("Exploration (Yellow)", "Magnetite Brooch", 25, SaveAvailability.Any),
            new ToolDefinition("Exploration (Yellow)", "Weighted Belt", 26, SaveAvailability.Any),
            new ToolDefinition("Exploration (Yellow)", "Barbed Bracelet", 27, SaveAvailability.Any),
            new ToolDefinition("Exploration (Yellow)", "Dead Bugs Purse", 28, SaveAvailability.NormalOnly),
            new ToolDefinition("Exploration (Yellow)", "Shell Satchel", 29, SaveAvailability.SteelSoulOnly),
            new ToolDefinition("Exploration (Yellow)", "Magnetite Dice", 30, SaveAvailability.Any),
            new ToolDefinition("Exploration (Yellow)", "Scuttlebrace", 31, SaveAvailability.Any),
            new ToolDefinition("Exploration (Yellow)", "Ascendants Grip", 32, SaveAvailability.Any),
            new ToolDefinition("Exploration (Yellow)", "Spider Strings", 33, SaveAvailability.Any),
            new ToolDefinition("Exploration (Yellow)", "Silkspeed Anklets", 34, SaveAvailability.Any),
            new ToolDefinition("Exploration (Yellow)", "Thiefs Mark", 35, SaveAvailability.Any),
        };

        private ConfigEntry<bool> globalModEnabled;
        private ConfigEntry<bool> globalBenchRequirement;
        private ConfigEntry<bool> magnetiteBroochPullsShards;
        private ConfigEntry<bool> silkspeedAnkletsCostNoSilk;
        private ConfigEntry<bool> debugLogging;
        private SaveScopedConfigEntry<bool> modEnabled;
        private SaveScopedConfigEntry<bool> benchRequirement;
        private SaveScopedConfigEntry<bool> saveMagnetiteBroochPullsShards;
        private SaveScopedConfigEntry<bool> saveSilkspeedAnkletsCostNoSilk;
        private SaveScopedConfigFile saveConfig;

        internal static Plugin Instance { get; private set; }
        internal static ManualLogSource Log { get; private set; }

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            saveConfig = SaveScopedConfig.For(this);
            RemoveObsoleteConfigEntries();

            globalModEnabled = Config.Bind(
                "Global: General",
                "Enabled",
                false,
                new ConfigDescription(
                    "Enable Equip All Tools for every save.",
                    null,
                    new ConfigurationManagerAttributes { Order = GetGlobalGeneralOrder() + 100 }));
            globalBenchRequirement = Config.Bind(
                "Global: General",
                "Bench Requirement",
                false,
                new ConfigDescription(
                    "Require a bench for Equip All Tools inventory hotkeys for every save. Normal crest management always follows the game's bench rules.",
                    null,
                    new ConfigurationManagerAttributes { Order = GetGlobalGeneralOrder() + 90 }));
            magnetiteBroochPullsShards = Config.Bind(
                "Global: General",
                "Magnetite Brooch Pulls Shards",
                false,
                new ConfigDescription(
                    "Allow Magnetite Brooch to pull shell shards as well as rosaries.",
                    null,
                    new ConfigurationManagerAttributes { Order = GetGlobalGeneralOrder() + 80 }));
            silkspeedAnkletsCostNoSilk = Config.Bind(
                "Global: General",
                "Silkspeed Anklets Cost No Silk",
                false,
                new ConfigDescription(
                    "Prevent Silkspeed Anklets from spending silk while active.",
                    null,
                    new ConfigurationManagerAttributes { Order = GetGlobalGeneralOrder() + 70 }));
            debugLogging = Config.Bind(
                "Global: General",
                "Debug Logging",
                false,
                new ConfigDescription(
                    "Log Equip All Tools hotkey decisions and config changes. Useful for troubleshooting.",
                    null,
                    new ConfigurationManagerAttributes { Order = GetGlobalGeneralOrder() + 60 }));
            for (int i = 0; i < Tools.Length; i++)
            {
                ToolDefinition tool = Tools[i];
                tool.GlobalDisplayAttributes = new ConfigurationManagerAttributes { Order = GetToolOrder(i, true) };
                tool.GlobalEnabled = Config.Bind(
                    "Global: " + tool.Category,
                    tool.ConfigKey,
                    false,
                    new ConfigDescription(
                        "Force " + tool.ConfigKey + " to count as equipped for every save.",
                        null,
                        tool.GlobalDisplayAttributes));
                tool.GlobalEnabled.SettingChanged += OnConfigChanged;
                Tools[i] = tool;
            }

            modEnabled = saveConfig.Bind(
                "General",
                "Enabled",
                true,
                new SaveScopedConfigDescription("Enable Equip All Tools for this save.")
                {
                    Order = () => GetSaveGeneralOrder() + 100,
                });
            modEnabled.SettingChanged += OnConfigChanged;
            benchRequirement = saveConfig.Bind(
                "General",
                "Bench Requirement",
                true,
                new SaveScopedConfigDescription("Require a bench for Equip All Tools inventory hotkeys for this save. Normal crest management always follows the game's bench rules.")
                {
                    Order = () => GetSaveGeneralOrder() + 90,
                });
            benchRequirement.SettingChanged += OnConfigChanged;
            saveMagnetiteBroochPullsShards = saveConfig.Bind(
                "General",
                "Magnetite Brooch Pulls Shards",
                true,
                new SaveScopedConfigDescription("Allow Magnetite Brooch to pull shell shards as well as rosaries for this save.")
                {
                    Order = () => GetSaveGeneralOrder() + 80,
                });
            saveMagnetiteBroochPullsShards.SettingChanged += OnConfigChanged;
            saveSilkspeedAnkletsCostNoSilk = saveConfig.Bind(
                "General",
                "Silkspeed Anklets Cost No Silk",
                true,
                new SaveScopedConfigDescription("Prevent Silkspeed Anklets from spending silk while active for this save.")
                {
                    Order = () => GetSaveGeneralOrder() + 70,
                });
            saveSilkspeedAnkletsCostNoSilk.SettingChanged += OnConfigChanged;

            for (int i = 0; i < Tools.Length; i++)
            {
                int toolIndex = i;
                ToolDefinition tool = Tools[i];
                tool.SaveEnabled = saveConfig.Bind(
                    tool.Category,
                    tool.ConfigKey,
                    false,
                    new SaveScopedConfigDescription("Force " + tool.ConfigKey + " to count as equipped for this save.")
                    {
                        VisibleWhen = () => Tools[toolIndex].IsAvailableForActiveSave,
                        DisplayName = () => Tools[toolIndex].CurrentDisplayName,
                        Order = () => GetToolOrder(toolIndex, false),
                    });
                tool.SaveEnabled.SettingChanged += OnConfigChanged;
                Tools[i] = tool;
            }

            SaveScopedConfig.ActiveSaveChanged += OnActiveSaveChanged;
            UpdateSaveToolVisibility(true);
            SyncForcedToolSideEffects();

            Harmony.PatchAll(typeof(Plugin).Assembly);
            RefreshToolHud();

            Logger.LogInfo(PluginName + " loaded.");
        }

        private void OnDestroy()
        {
            SaveScopedConfig.ActiveSaveChanged -= OnActiveSaveChanged;
            if (globalModEnabled != null)
            {
                globalModEnabled.SettingChanged -= OnConfigChanged;
            }

            if (globalBenchRequirement != null)
            {
                globalBenchRequirement.SettingChanged -= OnConfigChanged;
            }

            if (magnetiteBroochPullsShards != null)
            {
                magnetiteBroochPullsShards.SettingChanged -= OnConfigChanged;
            }

            if (silkspeedAnkletsCostNoSilk != null)
            {
                silkspeedAnkletsCostNoSilk.SettingChanged -= OnConfigChanged;
            }

            if (debugLogging != null)
            {
                debugLogging.SettingChanged -= OnConfigChanged;
            }

            if (modEnabled != null)
            {
                modEnabled.SettingChanged -= OnConfigChanged;
            }

            if (benchRequirement != null)
            {
                benchRequirement.SettingChanged -= OnConfigChanged;
            }

            if (saveMagnetiteBroochPullsShards != null)
            {
                saveMagnetiteBroochPullsShards.SettingChanged -= OnConfigChanged;
            }

            if (saveSilkspeedAnkletsCostNoSilk != null)
            {
                saveSilkspeedAnkletsCostNoSilk.SettingChanged -= OnConfigChanged;
            }

            for (int i = 0; i < Tools.Length; i++)
            {
                ToolDefinition definition = Tools[i];
                if (definition.GlobalEnabled != null)
                {
                    definition.GlobalEnabled.SettingChanged -= OnConfigChanged;
                }

                if (definition.SaveEnabled != null)
                {
                    definition.SaveEnabled.SettingChanged -= OnConfigChanged;
                }
            }

            if (saveConfig != null)
            {
                saveConfig.Dispose();
                saveConfig = null;
            }

            Harmony.UnpatchSelf();
            ClearMappedTools();
            Instance = null;
            Log = null;
        }

        private void OnActiveSaveChanged()
        {
            UpdateSaveToolVisibility(true);
            SyncForcedToolSideEffects();
            RefreshToolHud();
        }

        private void OnConfigChanged(object sender, System.EventArgs args)
        {
            SyncForcedToolSideEffects();
            RefreshToolHud();
        }

        private void RemoveObsoleteConfigEntries()
        {
            bool removedAny = false;
            removedAny |= RemoveConfigEntry("Global: General", "Allow Hotkey Changes Away From Bench");

            for (int saveSlot = 1; saveSlot <= 4; saveSlot++)
            {
                string saveKey = "Save " + saveSlot;
                removedAny |= RemoveConfigEntry(saveKey + ": General", "Allow Hotkey Changes Away From Bench");

                string[] oldSaveKeys =
                {
                    saveKey + " - Normal",
                    saveKey + " - Steel Soul",
                    saveKey + " - Steel Soul Dead",
                };

                for (int i = 0; i < oldSaveKeys.Length; i++)
                {
                    removedAny |= RemoveConfigEntry(oldSaveKeys[i] + ": General", "Enabled");
                    removedAny |= RemoveConfigEntry(oldSaveKeys[i] + ": General", "Allow Hotkey Changes Away From Bench");

                    for (int toolIndex = 0; toolIndex < Tools.Length; toolIndex++)
                    {
                        removedAny |= RemoveConfigEntry(oldSaveKeys[i] + ": " + Tools[toolIndex].Category, Tools[toolIndex].ConfigKey);
                    }

                    removedAny |= RemoveConfigEntry(oldSaveKeys[i] + ": Defense (Blue)", "Druids Eyes");
                    removedAny |= RemoveConfigEntry(oldSaveKeys[i] + ": Defense (Blue)", "Claw Mirrors");
                }
            }

            if (removedAny)
            {
                Config.Save();
            }
        }

        private bool RemoveConfigEntry(string section, string key)
        {
            return Config.Remove(new ConfigDefinition(section, key));
        }

        private static void UpdateSaveToolVisibility(bool refreshConfigManager)
        {
            for (int i = 0; i < Tools.Length; i++)
            {
                ToolDefinition definition = Tools[i];
                if (definition.GlobalDisplayAttributes != null)
                {
                    definition.GlobalDisplayAttributes.DispName = definition.CurrentDisplayName;
                }
            }

            if (refreshConfigManager)
            {
                SaveScopedConfig.RefreshConfigManager(true);
            }
        }

        private static bool IsToolForceEquipped(ToolItem tool)
        {
            if (Instance == null || tool == null)
            {
                return false;
            }

            bool isModEnabled = Instance.globalModEnabled.Value || Instance.modEnabled.Value;
            if (!isModEnabled)
            {
                return false;
            }

            for (int i = 0; i < Tools.Length; i++)
            {
                ToolDefinition definition = Tools[i];
                if (IsDefinitionEnabled(i) && ReferenceEquals(tool, GetActiveTool(definition, i)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ShouldSuppressCustomToolOverrideForForcedQuickSling()
        {
            return ToolItemManager.IsCustomToolOverride && !IsToolForceEquipped(Gameplay.QuickSlingTool);
        }

        private static bool ShouldSuppressCustomToolOverrideForForcedPollipPouch()
        {
            return ToolItemManager.IsCustomToolOverride && !IsToolForceEquipped(Gameplay.PoisonPouchTool);
        }

        private static IEnumerable<CodeInstruction> ReplaceCustomToolOverrideCheck(
            IEnumerable<CodeInstruction> instructions,
            System.Reflection.MethodInfo replacementMethod)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (ToolItemManagerIsCustomToolOverrideGetter != null &&
                    replacementMethod != null &&
                    instruction.Calls(ToolItemManagerIsCustomToolOverrideGetter))
                {
                    yield return new CodeInstruction(OpCodes.Call, replacementMethod);
                    continue;
                }

                yield return instruction;
            }
        }

        private static void SyncForcedToolSideEffects()
        {
            EnsureForcedFracturedMaskHasCharge();
        }

        private static void EnsureForcedFracturedMaskHasCharge()
        {
            ToolItem fracturedMaskTool = Gameplay.FracturedMaskTool;
            if (fracturedMaskTool == null)
            {
                return;
            }

            string refillKey = SaveScopedConfig.ActiveSaveKey;
            if (!IsToolForceEquipped(fracturedMaskTool) || string.IsNullOrEmpty(refillKey))
            {
                FracturedMaskVirtualRefills.Remove(refillKey);
                return;
            }

            if (FracturedMaskVirtualRefills.Contains(refillKey) || fracturedMaskTool.SavedData.AmountLeft > 0)
            {
                return;
            }

            ToolItemsData.Data savedData = fracturedMaskTool.SavedData;
            savedData.AmountLeft = Mathf.Max(1, ToolItemManager.GetToolStorageAmount(fracturedMaskTool));
            fracturedMaskTool.SavedData = savedData;
            FracturedMaskVirtualRefills.Add(refillKey);
            DebugLog("Gave forced Fractured Mask a virtual charge for " + refillKey + ".");
        }

        private static void RefreshToolHud()
        {
            try
            {
                if (ClearToolCacheMethod != null)
                {
                    ClearToolCacheMethod.Invoke(null, null);
                }

                ToolItemManager.RefreshEquippedState();
                ToolItemManager.SendEquippedChangedEvent(true);
                ToolItemManager.ReportAllBoundAttackToolsUpdated();
            }
            catch (System.Exception ex)
            {
                if (Log != null)
                {
                    Log.LogDebug("Could not refresh tool HUD yet: " + ex.Message);
                }
            }
        }

        private static ToolItem GetToolStatusTool(ToolItemManager.ToolStatus toolStatus)
        {
            return ToolStatusToolField == null ? null : ToolStatusToolField.GetValue(toolStatus) as ToolItem;
        }

        private static bool ToggleSaveToolFromInventory(InventoryItemTool inventoryTool)
        {
            if (Instance == null || inventoryTool == null || !SaveScopedConfig.HasActiveSave)
            {
                DebugLog("Ignored inventory hotkey because there is no active save or inventory item.");
                return false;
            }

            object manager = GetInventoryToolManager(inventoryTool);
            DisableForcedToolsNormallyEquipped(manager);

            if (!CanUseInventoryHotkeyHere(inventoryTool))
            {
                DebugLog("Ignored inventory hotkey because the bench requirement is active.");
                ShowToolBenchMessage(manager);
                return true;
            }

            int definitionIndex;
            if (!TryGetDefinitionIndexForTool(inventoryTool.ItemData, out definitionIndex))
            {
                DebugLog("Ignored inventory hotkey because the selected item is not a supported tool.");
                return false;
            }

            ToolDefinition definition = Tools[definitionIndex];
            if (!definition.IsAvailableForActiveSave || definition.SaveEnabled == null)
            {
                DebugLog("Ignored inventory hotkey because " + definition.ConfigKey + " is not available for this save.");
                return false;
            }

            bool isNormallyEquipped = IsInventoryToolNormallyEquipped(inventoryTool, manager);
            if (isNormallyEquipped)
            {
                SetSaveToolForced(definitionIndex, false);
                UpdateInventoryToolDisplay(inventoryTool, true);
                RefreshToolHud();
                DebugLog("Ignored inventory hotkey because " + definition.CurrentDisplayName + " is already equipped normally.");
                return true;
            }

            bool enabled = !definition.SaveEnabled.Value;
            SetSaveToolForced(definitionIndex, enabled);
            UpdateInventoryToolDisplay(inventoryTool, !enabled);
            PlayInventoryToggleSound(inventoryTool);
            RefreshToolHud();
            DebugLog("Hotkey toggled " + definition.CurrentDisplayName + " " + (enabled ? "on" : "off") + " for " + SaveScopedConfig.ActiveSaveKey + ".");
            return true;
        }

        private static bool CanUseInventoryHotkeyHere(InventoryItemTool inventoryTool)
        {
            if (Instance == null)
            {
                return false;
            }

            if (!IsBenchRequiredForHotkeys())
            {
                return true;
            }

            object manager = GetInventoryToolManager(inventoryTool);
            if (manager == null || InventoryToolManagerCanChangeEquipsMethod == null)
            {
                return false;
            }

            return (bool)InventoryToolManagerCanChangeEquipsMethod.Invoke(manager, null);
        }

        private static void ShowToolBenchMessage(object manager)
        {
            InventoryItemToolManager inventoryManager = manager as InventoryItemToolManager;
            if (inventoryManager == null)
            {
                return;
            }

            inventoryManager.ShowCrestEquipMsg();
            SetToolBenchMessageText(inventoryManager);
        }

        private static void SetToolBenchMessageText(InventoryItemToolManager manager)
        {
            object crestEquipMsg = InventoryToolManagerCrestEquipMsgField == null || manager == null
                ? null
                : InventoryToolManagerCrestEquipMsgField.GetValue(manager);
            Component component = crestEquipMsg as Component;
            if (component == null)
            {
                return;
            }

            if (TextMeshProType == null)
            {
                return;
            }

            Component[] texts = component.GetComponentsInChildren(TextMeshProType, true);
            System.Reflection.PropertyInfo textProperty = AccessTools.Property(TextMeshProType, "text");
            for (int i = 0; i < texts.Length; i++)
            {
                Component text = texts[i];
                if (text != null && textProperty != null && !string.IsNullOrEmpty(textProperty.GetValue(text, null) as string))
                {
                    textProperty.SetValue(text, ToolBenchMessage, null);
                }
            }
        }

        private static bool ShouldBypassBenchRequirementForHotkey(InventoryItemToolManager manager)
        {
            if (Instance == null || manager == null || !SaveScopedConfig.HasActiveSave || !IsInventoryToggleModifierHeld())
            {
                return false;
            }

            if (IsBenchRequiredForHotkeys())
            {
                return false;
            }

            InventoryItemTool inventoryTool = manager.HoveringTool ?? manager.CurrentSelected as InventoryItemTool;
            int definitionIndex;
            return inventoryTool != null &&
                TryGetDefinitionIndexForTool(inventoryTool.ItemData, out definitionIndex) &&
                Tools[definitionIndex].IsAvailableForActiveSave;
        }

        private static bool IsBenchRequiredForHotkeys()
        {
            if (Instance == null)
            {
                return true;
            }

            return Instance.globalBenchRequirement.Value || Instance.benchRequirement.Value;
        }

        private static bool SetSaveToolForced(ToolItem tool, bool enabled)
        {
            int definitionIndex;
            if (!TryGetDefinitionIndexForTool(tool, out definitionIndex))
            {
                return false;
            }

            return SetSaveToolForced(definitionIndex, enabled);
        }

        private static bool SetSaveToolForced(int definitionIndex, bool enabled)
        {
            if (Instance == null || !SaveScopedConfig.HasActiveSave)
            {
                return false;
            }

            ToolDefinition definition = Tools[definitionIndex];
            if (!definition.IsAvailableForActiveSave || definition.SaveEnabled == null || definition.SaveEnabled.Value == enabled)
            {
                return false;
            }

            definition.SaveEnabled.Value = enabled;
            DebugLog("Set save-scoped " + definition.CurrentDisplayName + " to " + enabled + ".");
            return true;
        }

        private static void DebugLog(string message)
        {
            if (Instance != null && Instance.debugLogging != null && Instance.debugLogging.Value && Log != null)
            {
                Log.LogInfo("[Debug] " + message);
            }
        }

        private static bool IsInventoryToggleModifierHeld()
        {
            return Input.GetKey(KeyCode.LeftShift) ||
                Input.GetKey(KeyCode.RightShift) ||
                Input.GetKey(KeyCode.JoystickButton8);
        }

        private static bool IsInventoryToolNormallyEquipped(InventoryItemTool inventoryTool)
        {
            object manager = GetInventoryToolManager(inventoryTool);
            return IsInventoryToolNormallyEquipped(inventoryTool, manager);
        }

        private static bool IsInventoryToolNormallyEquipped(InventoryItemTool inventoryTool, object manager)
        {
            if (manager == null)
            {
                return false;
            }

            return IsToolNormallyEquippedInManager(manager, inventoryTool.ItemData);
        }

        private static bool IsToolNormallyEquippedInManager(object manager, ToolItem tool)
        {
            if (manager == null || tool == null)
            {
                return false;
            }

            object crestList = InventoryToolManagerCrestListField == null ? null : InventoryToolManagerCrestListField.GetValue(manager);
            if (SlotCollectionContainsTool(InventoryToolCrestListGetSlotsMethod, crestList, tool))
            {
                return true;
            }

            object extraSlots = InventoryToolManagerExtraSlotsField == null ? null : InventoryToolManagerExtraSlotsField.GetValue(manager);
            return SlotCollectionContainsTool(InventoryFloatingToolSlotsGetSlotsMethod, extraSlots, tool);
        }

        private static bool IsDefinitionNormallyEquippedInManager(object manager, ToolDefinition definition, int definitionIndex)
        {
            if (IsToolNormallyEquippedInManager(manager, GetBaseTool(definition, definitionIndex)))
            {
                return true;
            }

            return definition.HasUpgrade && IsToolNormallyEquippedInManager(manager, GetUpgradeTool(definition, definitionIndex));
        }

        private static void DisableForcedToolsNormallyEquipped(object manager)
        {
            if (isCleaningNormallyEquippedTools || Instance == null || !SaveScopedConfig.HasActiveSave || manager == null)
            {
                return;
            }

            isCleaningNormallyEquippedTools = true;
            try
            {
                for (int i = 0; i < Tools.Length; i++)
                {
                    ToolDefinition definition = Tools[i];
                    if (definition.SaveEnabled == null || !definition.SaveEnabled.Value)
                    {
                        continue;
                    }

                    if (IsDefinitionNormallyEquippedInManager(manager, definition, i))
                    {
                        SetSaveToolForced(i, false);
                    }
                }
            }
            finally
            {
                isCleaningNormallyEquippedTools = false;
            }
        }

        private static bool SlotCollectionContainsTool(System.Reflection.MethodInfo getSlotsMethod, object slotContainer, ToolItem tool)
        {
            if (getSlotsMethod == null || slotContainer == null || tool == null)
            {
                return false;
            }

            System.Collections.IEnumerable slots = getSlotsMethod.Invoke(slotContainer, null) as System.Collections.IEnumerable;
            if (slots == null)
            {
                return false;
            }

            foreach (object slotObject in slots)
            {
                InventoryToolCrestSlot slot = slotObject as InventoryToolCrestSlot;
                if (slot != null && ReferenceEquals(slot.EquippedItem, tool))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetForcedInventoryCueColor(object manager, ToolItemType toolType, out Color color)
        {
            if (manager == null || InventoryToolManagerGetToolTypeColorMethod == null)
            {
                color = Color.white;
                return false;
            }

            color = (Color)InventoryToolManagerGetToolTypeColorMethod.Invoke(manager, new object[] { toolType });
            color.a = ForcedInventoryCueAlpha;
            return true;
        }

        private static object GetInventoryToolManager(InventoryItemTool inventoryTool)
        {
            if (inventoryTool == null || InventoryToolManagerField == null)
            {
                return null;
            }

            return InventoryToolManagerField.GetValue(inventoryTool);
        }

        private static bool IsInventoryToolForcedOnly(InventoryItemTool inventoryTool)
        {
            return inventoryTool != null &&
                IsToolForceEquipped(inventoryTool.ItemData) &&
                !IsInventoryToolNormallyEquipped(inventoryTool);
        }

        private static void ApplyForcedInventoryCue(InventoryItemTool inventoryTool)
        {
            if (inventoryTool == null || !IsInventoryToolForcedOnly(inventoryTool) || NestedFadeGroupSpriteRendererColorProperty == null)
            {
                return;
            }

            object manager = GetInventoryToolManager(inventoryTool);
            if (manager == null)
            {
                return;
            }

            Color color;
            if (!TryGetForcedInventoryCueColor(manager, inventoryTool.ToolType, out color))
            {
                return;
            }

            object selectedIndicator = InventoryToolSelectedIndicatorField == null ? null : InventoryToolSelectedIndicatorField.GetValue(inventoryTool);
            if (selectedIndicator != null)
            {
                NestedFadeGroupSpriteRendererColorProperty.SetValue(selectedIndicator, color, null);
            }
        }

        private static void UpdateInventoryToolDisplay(InventoryItemTool inventoryTool, bool isInstant = false)
        {
            if (InventoryToolUpdateEquippedDisplayMethod != null)
            {
                InventoryToolUpdateEquippedDisplayMethod.Invoke(inventoryTool, new object[] { isInstant });
            }

            ApplyForcedInventoryCue(inventoryTool);
        }

        private static void PlayInventoryToggleSound(InventoryItemTool inventoryTool)
        {
            if (InventoryToolManagerPlayMoveSoundMethod == null || InventoryToolManagerField == null)
            {
                return;
            }

            object manager = InventoryToolManagerField.GetValue(inventoryTool);
            if (manager != null)
            {
                InventoryToolManagerPlayMoveSoundMethod.Invoke(manager, null);
            }
        }

        private static bool TryGetDefinitionIndexForTool(ToolItem tool, out int definitionIndex)
        {
            definitionIndex = -1;
            if (tool == null)
            {
                return false;
            }

            for (int i = 0; i < Tools.Length; i++)
            {
                ToolDefinition definition = Tools[i];
                if (ReferenceEquals(tool, GetBaseTool(definition, i)) || ReferenceEquals(tool, GetUpgradeTool(definition, i)))
                {
                    definitionIndex = i;
                    return true;
                }
            }

            return false;
        }

        private static ToolItem GetActiveTool(ToolDefinition definition, int index)
        {
            ToolItem upgradedTool = GetUpgradeTool(definition, index);
            if (upgradedTool != null && upgradedTool.IsUnlocked)
            {
                return upgradedTool;
            }

            return GetBaseTool(definition, index);
        }

        private static ToolItem GetBaseTool(ToolDefinition definition, int index)
        {
            if (definition.Tool != null)
            {
                return definition.Tool;
            }

            List<ToolItem> tools = ToolItemManager.GetAllTools().ToList();
            int toolIndex = FirstBlueYellowToolIndex + definition.IndexInBlueYellowTools;
            if (tools.Count <= toolIndex)
            {
                Log.LogWarning("Could not map " + definition.ConfigKey + ". Expected tool index " + toolIndex + ", but only found " + tools.Count + " tools.");
                return null;
            }

            definition.Tool = tools[toolIndex];
            Tools[index] = definition;

            return definition.Tool;
        }

        private static ToolItem GetUpgradeTool(ToolDefinition definition, int index)
        {
            if (!definition.HasUpgrade)
            {
                return null;
            }

            if (definition.UpgradeTool != null)
            {
                return definition.UpgradeTool;
            }

            List<ToolItem> tools = ToolItemManager.GetAllTools().ToList();
            int toolIndex = FirstBlueYellowToolIndex + definition.UpgradeIndexInBlueYellowTools;
            if (tools.Count <= toolIndex)
            {
                Log.LogWarning("Could not map " + definition.UpgradeDisplayName + ". Expected tool index " + toolIndex + ", but only found " + tools.Count + " tools.");
                return null;
            }

            definition.UpgradeTool = tools[toolIndex];
            Tools[index] = definition;
            return definition.UpgradeTool;
        }

        private static ToolItem ResolveUpgradeTool(ToolDefinition definition)
        {
            if (!definition.HasUpgrade)
            {
                return null;
            }

            List<ToolItem> tools = ToolItemManager.GetAllTools().ToList();
            int toolIndex = FirstBlueYellowToolIndex + definition.UpgradeIndexInBlueYellowTools;
            return tools.Count > toolIndex ? tools[toolIndex] : null;
        }

        private static void AddForceEquippedTools(List<ToolItem> result)
        {
            if (Instance == null || result == null)
            {
                return;
            }

            bool isModEnabled = Instance.globalModEnabled.Value || Instance.modEnabled.Value;
            if (!isModEnabled)
            {
                return;
            }

            for (int i = 0; i < Tools.Length; i++)
            {
                ToolDefinition definition = Tools[i];
                ToolItem tool = GetActiveTool(definition, i);
                if (IsDefinitionEnabled(i) && tool != null && !result.Contains(tool))
                {
                    result.Add(tool);
                }
            }
        }

        private static void AddForceVisibleTools(List<ToolItem> result)
        {
            if (Instance == null || result == null)
            {
                return;
            }

            bool isModEnabled = Instance.globalModEnabled.Value || Instance.modEnabled.Value;
            if (!isModEnabled)
            {
                return;
            }

            List<ToolItem> allTools = ToolItemManager.GetAllTools().ToList();
            for (int i = 0; i < allTools.Count; i++)
            {
                ToolItem tool = allTools[i];
                if (IsToolForceEquipped(tool) && !result.Contains(tool))
                {
                    result.Add(tool);
                }
            }
        }

        private static void UpdateInventoryToolCount(InventoryItemToolManager manager, List<ToolItem> tools)
        {
            if (manager == null || tools == null || InventoryToolManagerCurrentToolCountField == null)
            {
                return;
            }

            int count = 0;
            for (int i = 0; i < tools.Count; i++)
            {
                ToolItem tool = tools[i];
                if (tool != null && tool.IsUnlockedNotHidden)
                {
                    count++;
                }
            }

            InventoryToolManagerCurrentToolCountField.SetValue(manager, count);
        }

        private static bool IsDefinitionEnabled(int index)
        {
            ToolDefinition definition = Tools[index];
            if (!definition.IsRawEnabled)
            {
                return false;
            }

            return true;
        }

        private static bool IsMagnetiteBroochEquipped()
        {
            for (int i = 0; i < Tools.Length; i++)
            {
                ToolDefinition definition = Tools[i];
                if (definition.ConfigKey != "Magnetite Brooch")
                {
                    continue;
                }

                ToolItem tool = GetActiveTool(definition, i);
                return tool != null && tool.IsEquipped;
            }

            return false;
        }

        private static bool ShouldMagnetPullShard(CurrencyObjectBase currencyObject)
        {
            return Instance != null &&
                IsMagnetiteBroochShardPullEnabled() &&
                currencyObject != null &&
                GetCurrencyPopupNameKey(currencyObject) == "INV_NAME_SHARD" &&
                IsMagnetiteBroochEquipped();
        }

        private static bool IsMagnetiteBroochShardPullEnabled()
        {
            return Instance != null &&
                ((Instance.magnetiteBroochPullsShards != null && Instance.magnetiteBroochPullsShards.Value) ||
                (Instance.saveMagnetiteBroochPullsShards != null && Instance.saveMagnetiteBroochPullsShards.Value));
        }

        private static string GetCurrencyPopupNameKey(CurrencyObjectBase currencyObject)
        {
            if (CurrencyObjectPopupNameField == null || currencyObject == null)
            {
                return null;
            }

            object popupName = CurrencyObjectPopupNameField.GetValue(currencyObject);
            if (popupName == null)
            {
                return null;
            }

            System.Reflection.PropertyInfo keyProperty = AccessTools.Property(popupName.GetType(), "Key");
            return keyProperty == null ? null : keyProperty.GetValue(popupName, null) as string;
        }

        private static bool ShouldPreventSilkspeedAnkletsSilkCost(int amount)
        {
            if (Instance == null ||
                !IsSilkspeedAnkletsCostNoSilkEnabled() ||
                amount != 1)
            {
                return false;
            }

            HeroController heroController = Object.FindFirstObjectByType<HeroController>();
            return heroController != null && heroController.IsSprintMasterActive;
        }

        private static bool IsSilkspeedAnkletsCostNoSilkEnabled()
        {
            return Instance != null &&
                ((Instance.silkspeedAnkletsCostNoSilk != null && Instance.silkspeedAnkletsCostNoSilk.Value) ||
                (Instance.saveSilkspeedAnkletsCostNoSilk != null && Instance.saveSilkspeedAnkletsCostNoSilk.Value));
        }

        private static int GetToolOrder(int index, bool global)
        {
            ToolDefinition definition = Tools[index];
            int categoryBase;
            if (global)
            {
                categoryBase = definition.Category == "Defense (Blue)" ? 1000 : 2000;
            }
            else
            {
                categoryBase = GetSaveSlotOrderBase() + (definition.Category == "Defense (Blue)" ? 1000 : 2000);
            }

            return categoryBase + index;
        }

        private static int GetSaveSlotOrderBase()
        {
            int saveSlot = SaveScopedConfig.ActiveSaveSlot;
            if (saveSlot < 1 || saveSlot > 4)
            {
                saveSlot = 4;
            }

            return saveSlot * 10000;
        }

        private static int GetGlobalGeneralOrder()
        {
            return 0;
        }

        private static int GetSaveGeneralOrder()
        {
            return GetSaveSlotOrderBase();
        }

        private static void ClearMappedTools()
        {
            for (int i = 0; i < Tools.Length; i++)
            {
                ToolDefinition definition = Tools[i];
                definition.Tool = null;
                definition.UpgradeTool = null;
                Tools[i] = definition;
            }
        }

        private struct ToolDefinition
        {
            public readonly string Category;
            public readonly string ConfigKey;
            public readonly int IndexInBlueYellowTools;
            public readonly SaveAvailability Availability;
            public readonly string UpgradeDisplayName;
            public readonly int UpgradeIndexInBlueYellowTools;
            public ConfigurationManagerAttributes GlobalDisplayAttributes;
            public ConfigEntry<bool> GlobalEnabled;
            public SaveScopedConfigEntry<bool> SaveEnabled;
            public ToolItem Tool;
            public ToolItem UpgradeTool;

            public ToolDefinition(string category, string configKey, int indexInBlueYellowTools, SaveAvailability availability)
                : this(category, configKey, indexInBlueYellowTools, availability, null, -1)
            {
            }

            public ToolDefinition(string category, string configKey, int indexInBlueYellowTools, SaveAvailability availability, string upgradeDisplayName, int upgradeIndexInBlueYellowTools)
            {
                Category = category;
                ConfigKey = configKey;
                IndexInBlueYellowTools = indexInBlueYellowTools;
                Availability = availability;
                UpgradeDisplayName = upgradeDisplayName;
                UpgradeIndexInBlueYellowTools = upgradeIndexInBlueYellowTools;
                GlobalDisplayAttributes = null;
                GlobalEnabled = null;
                SaveEnabled = null;
                Tool = null;
                UpgradeTool = null;
            }

            public bool IsRawEnabled
            {
                get
                {
                    bool globalEnabled = IsAvailableForActiveSave && GlobalEnabled != null && GlobalEnabled.Value;
                    bool saveEnabled = IsAvailableForActiveSave && SaveEnabled != null && SaveEnabled.Value;
                    return globalEnabled || saveEnabled;
                }
            }

            public bool IsAvailableForActiveSave
            {
                get
                {
                    if (!SaveScopedConfig.HasActiveSave)
                    {
                        return false;
                    }

                    if (Availability == SaveAvailability.Any)
                    {
                        return true;
                    }

                    bool isSteelSoul = SaveScopedConfig.ActiveSaveMode == PermadeathModes.On || SaveScopedConfig.ActiveSaveMode == PermadeathModes.Dead;
                    if (Availability == SaveAvailability.SteelSoulOnly)
                    {
                        return isSteelSoul;
                    }

                    return !isSteelSoul;
                }
            }

            public bool HasUpgrade
            {
                get { return UpgradeIndexInBlueYellowTools >= 0; }
            }

            public string CurrentDisplayName
            {
                get
                {
                    ToolItem upgrade = UpgradeTool;
                    if (HasUpgrade && upgrade == null)
                    {
                        upgrade = ResolveUpgradeTool(this);
                    }

                    return upgrade != null && upgrade.IsUnlocked ? UpgradeDisplayName : ConfigKey;
                }
            }
        }

        private enum SaveAvailability
        {
            Any,
            NormalOnly,
            SteelSoulOnly,
        }

        [HarmonyPatch(typeof(ToolItem), "IsEquipped", MethodType.Getter)]
        private static class ToolItemIsEquippedPatch
        {
            private static bool Prefix(ToolItem __instance, ref bool __result)
            {
                if (!IsToolForceEquipped(__instance))
                {
                    return true;
                }

                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(ToolItem), "IsEquippedHud", MethodType.Getter)]
        private static class ToolItemIsEquippedHudPatch
        {
            private static bool Prefix(ToolItem __instance, ref bool __result)
            {
                if (!IsToolForceEquipped(__instance))
                {
                    return true;
                }

                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(ToolItem), "IsUnlocked", MethodType.Getter)]
        private static class ToolItemIsUnlockedPatch
        {
            private static void Postfix(ToolItem __instance, ref bool __result)
            {
                if (!__result && IsToolForceEquipped(__instance))
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(ToolItem), "IsUnlockedNotHidden", MethodType.Getter)]
        private static class ToolItemIsUnlockedNotHiddenPatch
        {
            private static void Postfix(ToolItem __instance, ref bool __result)
            {
                if (!__result && IsToolForceEquipped(__instance))
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(ToolItemManager.ToolStatus), "IsEquipped", MethodType.Getter)]
        private static class ToolStatusIsEquippedPatch
        {
            private static bool Prefix(ToolItemManager.ToolStatus __instance, ref bool __result)
            {
                if (!IsToolForceEquipped(GetToolStatusTool(__instance)))
                {
                    return true;
                }

                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(ToolItemManager), "IsToolEquipped", new System.Type[] { typeof(ToolItem), typeof(ToolEquippedReadSource) })]
        private static class ToolItemManagerIsToolEquippedPatch
        {
            private static bool Prefix(ToolItem tool, ToolEquippedReadSource readSource, ref bool __result)
            {
                if (!IsToolForceEquipped(tool))
                {
                    return true;
                }

                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(ToolItemManager), "GetCurrentEquippedTools")]
        private static class GetCurrentEquippedToolsPatch
        {
            private static void Postfix(List<ToolItem> __result)
            {
                AddForceEquippedTools(__result);
            }
        }

        [HarmonyPatch(typeof(InventoryItemToolManager), "GetItems")]
        private static class InventoryItemToolManagerGetItemsPatch
        {
            private static void Postfix(InventoryItemToolManager __instance, List<ToolItem> __result)
            {
                AddForceVisibleTools(__result);
                UpdateInventoryToolCount(__instance, __result);
            }
        }

        [HarmonyPatch(typeof(HeroController), "ThrowTool")]
        private static class HeroControllerThrowToolPatch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return ReplaceCustomToolOverrideCheck(instructions, ShouldSuppressCustomToolOverrideForForcedQuickSlingMethod);
            }
        }

        [HarmonyPatch(typeof(ClockworkHatchling), "RecordPoisonStatus")]
        private static class ClockworkHatchlingRecordPoisonStatusPatch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return ReplaceCustomToolOverrideCheck(instructions, ShouldSuppressCustomToolOverrideForForcedPollipPouchMethod);
            }
        }

        [HarmonyPatch(typeof(ClockworkHatchlingDummy), "CheckPoison")]
        private static class ClockworkHatchlingDummyCheckPoisonPatch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return ReplaceCustomToolOverrideCheck(instructions, ShouldSuppressCustomToolOverrideForForcedPollipPouchMethod);
            }
        }

        [HarmonyPatch(typeof(CurrencyObjectBase), "MagnetToolIsEquipped")]
        private static class CurrencyObjectBaseMagnetToolIsEquippedPatch
        {
            private static bool Prefix(CurrencyObjectBase __instance, ref bool __result)
            {
                if (!ShouldMagnetPullShard(__instance))
                {
                    return true;
                }

                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(PlayerData), "TakeSilk")]
        private static class PlayerDataTakeSilkPatch
        {
            private static bool Prefix(ref int amount)
            {
                return !ShouldPreventSilkspeedAnkletsSilkCost(amount);
            }
        }

        [HarmonyPatch(typeof(InventoryItemTool), "DoPress")]
        private static class InventoryItemToolDoPressPatch
        {
            private static bool Prefix(InventoryItemTool __instance)
            {
                if (!IsInventoryToggleModifierHeld())
                {
                    return true;
                }

                return !ToggleSaveToolFromInventory(__instance);
            }
        }

        [HarmonyPatch(typeof(InventoryItemTool), "UpdateEquippedDisplay")]
        private static class InventoryItemToolUpdateEquippedDisplayPatch
        {
            private static void Postfix(InventoryItemTool __instance)
            {
                ApplyForcedInventoryCue(__instance);
            }
        }

        [HarmonyPatch(typeof(InventoryItemToolManager), "RefreshTools", new System.Type[0])]
        private static class InventoryItemToolManagerRefreshToolsPatch
        {
            private static void Postfix(InventoryItemToolManager __instance)
            {
                DisableForcedToolsNormallyEquipped(__instance);
            }
        }

        [HarmonyPatch(typeof(InventoryItemToolManager), "CanChangeEquips", new System.Type[0])]
        private static class InventoryItemToolManagerCanChangeEquipsPatch
        {
            private static bool Prefix(InventoryItemToolManager __instance, ref bool __result)
            {
                if (!ShouldBypassBenchRequirementForHotkey(__instance))
                {
                    return true;
                }

                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(InventoryItemToolManager), "CanChangeEquips")]
        private static class InventoryItemToolManagerCanChangeEquipsTypedPatch
        {
            private static System.Reflection.MethodBase TargetMethod()
            {
                List<System.Reflection.MethodInfo> methods = AccessTools.GetDeclaredMethods(typeof(InventoryItemToolManager));
                for (int i = 0; i < methods.Count; i++)
                {
                    System.Reflection.MethodInfo method = methods[i];
                    if (method.Name == "CanChangeEquips" && method.GetParameters().Length == 2)
                    {
                        return method;
                    }
                }

                return null;
            }

            private static bool Prefix(InventoryItemToolManager __instance, ref bool __result)
            {
                if (!ShouldBypassBenchRequirementForHotkey(__instance))
                {
                    return true;
                }

                __result = true;
                return false;
            }
        }

        [HarmonyPatch]
        private static class InventoryItemToolManagerUnequipToolPatch
        {
            private static System.Reflection.MethodBase TargetMethod()
            {
                return AccessTools.Method(
                    typeof(InventoryItemToolManager),
                    "UnequipTool",
                    new[] { typeof(ToolItem), typeof(InventoryToolCrestSlot) });
            }

            private static void Prefix(ToolItem toolItem)
            {
                SetSaveToolForced(toolItem, false);
            }
        }

        [HarmonyPatch(typeof(InventoryToolCrestSlot), "set_EquippedItem")]
        private static class InventoryToolCrestSlotEquippedItemSetterPatch
        {
            private static void Prefix(InventoryToolCrestSlot __instance, ToolItem value)
            {
                if (__instance == null || __instance.EquippedItem == null || ReferenceEquals(__instance.EquippedItem, value))
                {
                    return;
                }

                SetSaveToolForced(__instance.EquippedItem, false);
            }
        }

        [HarmonyPatch(typeof(InventoryToolCrestSlot), "SetEquipped")]
        private static class InventoryToolCrestSlotSetEquippedPatch
        {
            private static void Prefix(InventoryToolCrestSlot __instance, ToolItem __0)
            {
                if (__instance == null || __instance.EquippedItem == null || ReferenceEquals(__instance.EquippedItem, __0))
                {
                    return;
                }

                SetSaveToolForced(__instance.EquippedItem, false);
            }
        }

    }
}
