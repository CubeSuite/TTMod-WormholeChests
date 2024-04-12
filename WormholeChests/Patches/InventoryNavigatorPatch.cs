using EquinoxsModUtils;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WormholeChests.Patches
{
    internal class InventoryNavigatorPatch
    {
        [HarmonyPatch(typeof(InventoryNavigator), "OnOpen")]
        [HarmonyPrefix]
        static void ShowGUI(InventoryNavigator __instance) {
            Unlock wormholeChestsUnlock = ModUtils.GetUnlockByName("Wormhole Chests");
            if (!TechTreeState.instance.IsUnlockActive(wormholeChestsUnlock.uniqueId)) return;

            ChestGUI.shouldShowGUI = true;

            ChestInstance chest = WormholeManager.GetAimedAtChest();
            uint id = chest.GetCommonInfo().instanceId;
            ChestGUI.currentChestID = id;

            WormholeChestsPlugin.Log.LogInfo($"Opened Chest {id}");
            if (WormholeManager.chestChannelMap.ContainsKey(id)) {
                ChestGUI.channel = WormholeManager.chestChannelMap[id];
                chest.commonInfo.inventories[0] = WormholeManager.GetWormhole(ChestGUI.channel).inventory;
            }
        }

        [HarmonyPatch(typeof(InventoryNavigator), "OnClose")]
        [HarmonyPrefix]
        static void HideGui() {
            InputHandler.instance.uiInputBlocked = false;
            ChestGUI.shouldShowGUI = false;
            ChestGUI.channel = "";
        }
    }
}
