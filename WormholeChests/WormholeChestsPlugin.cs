using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EquinoxsModUtils;
using HarmonyLib;
using System;
using UnityEngine;
using WormholeChests.Patches;

namespace WormholeChests
{
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class WormholeChestsPlugin : BaseUnityPlugin
    {
        private const string MyGUID = "com.equinox.WormholeChests";
        private const string PluginName = "WormholeChests";
        private const string VersionString = "1.0.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log = new ManualLogSource(PluginName);

        public static Texture2D wormholeTexture;

        #region Config Entries

        public static ConfigEntry<bool> freeWormholeChests;

        public static ConfigEntry<float> channelBoxXOffset;
        public static ConfigEntry<float> channelBoxYOffset;
        public static ConfigEntry<float> channelBoxWidth;

        public static ConfigEntry<float> createButtonXOffset;

        #endregion

        // Unity Functions

        private void Awake() {
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");
            Harmony.PatchAll();

            ModUtils.GameDefinesLoaded += OnGameDefinesLoaded;
            ModUtils.MachineManagerLoaded += OnMachineManagerLoaded;

            CreateConfigEntries();
            ApplyPatches();
            ChestGUI.LoadImages();

            Sprite unlockSprite = Sprite.Create(wormholeTexture, new Rect(0, 0, wormholeTexture.width, wormholeTexture.height), new Vector2(0, 0), 512);
            NewUnlockDetails unlockDetails = new NewUnlockDetails() {
                category = Unlock.TechCategory.Science,
                coreTypeNeeded = ResearchCoreDefinition.CoreType.Green,
                coreCountNeeded = 2000,
                description = "Allow chests on the same channel to share inventories.",
                displayName = "Wormhole Chests",
                numScansNeeded = 0,
                requiredTier = TechTreeState.ResearchTier.Tier1,
                treePosition = 0,
                sprite = unlockSprite
            };
            ModUtils.AddNewUnlock(unlockDetails);

            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
            Log = Logger;
        }

        private void OnGUI() {
            if (ChestGUI.shouldShowGUI) {
                ChestGUI.DrawChestGUI();
            }
        }

        // Events

        private void OnGameDefinesLoaded(object sender, EventArgs e) {
            Unlock coreBoostThresh = ModUtils.GetUnlockByName(UnlockNames.CoreBoostThreshing);
            ModUtils.UpdateUnlockTier("Wormhole Chests", coreBoostThresh.requiredTier);
        }

        private void OnMachineManagerLoaded(object sender, EventArgs e) {
            WormholeManager.LoadData(SaveState.instance.metadata.worldName);
            Log.LogInfo("WormholeChests Loaded");
            MachineInstanceList<ChestInstance, ChestDefinition> chestsList = MachineManager.instance.GetMachineList<ChestInstance, ChestDefinition>(MachineTypeEnum.Chest);
            for(int i = 0; i < chestsList.myArray.Length; i++) {
                ChestInstance chest = chestsList.myArray[i];
                uint id = chest.commonInfo.instanceId;
                if (WormholeManager.chestChannelMap.ContainsKey(id)) {
                    chest.commonInfo.inventories[0] = WormholeManager.GetInventoryForChest(id);
                }
            }
        }

        // Private Functions

        private void CreateConfigEntries() {
            freeWormholeChests = Config.Bind("General", "Free Wormhole Chests", false, new ConfigDescription("Disables the cost of creating Wormhole Chests. Cheat, not recommended."));

            channelBoxXOffset = Config.Bind("GUI Layout", "Channel Box X Offset", 32f, new ConfigDescription("Controls the horizontal position of the Channel box in a Chest's GUI."));
            channelBoxYOffset = Config.Bind("GUI Layout", "Channel Box Y Offset", -355f, new ConfigDescription("Controls the vertical position of the Channel box in a Chest's GUI."));
            channelBoxWidth = Config.Bind("GUI Layout", "Channel Box Width", 240f, new ConfigDescription("Controls the width of the Channel box in a Chest's GUI."));

            createButtonXOffset = Config.Bind("GUI Layout", "Create Button X Offset", 444f, new ConfigDescription("Controls the horizontal position of the Create / Link button in a Chest's GUI."));
        }

        private void ApplyPatches() {
            Harmony.CreateAndPatchAll(typeof(ChestDefinitionPatch));
            Harmony.CreateAndPatchAll(typeof(ChestInstancePatch));
            Harmony.CreateAndPatchAll(typeof(InventoryNavigatorPatch));
            Harmony.CreateAndPatchAll(typeof(SaveStatePatch));
        }
    }
}
