using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WormholeChests.Patches
{
    internal class SaveStatePatch
    {
        [HarmonyPatch(typeof(SaveState), "SaveToFile")]
        [HarmonyPostfix]
        private static void SaveWormholes() {
            WormholeManager.SaveData(SaveState.instance.metadata.worldName);
            WormholeChestsPlugin.Log.LogInfo("WormholeChests Saved");
        }
    }
}
