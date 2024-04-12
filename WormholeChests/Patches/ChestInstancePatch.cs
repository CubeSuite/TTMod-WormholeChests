using EquinoxsModUtils;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WormholeChests.Patches
{
    internal class ChestInstancePatch {

        static bool hasLogged = false;

        [HarmonyPatch(typeof(ChestInstance), "GetInventory")]
        [HarmonyPrefix]
        private static void GetWormholeInsteadOfInventory(ChestInstance __instance){
            if (WormholeManager.IsChestWormholeChest(__instance)) {
                __instance.commonInfo.inventories[0] = WormholeManager.GetInventoryForChest(__instance);
            }
        }
    }
}
