using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace WormholeChests.Patches
{
    internal class ChestDefinitionPatch
    {
        [HarmonyPatch(typeof(MachineDefinition<ChestInstance, ChestDefinition>), "OnDeconstruct")]
        [HarmonyPostfix]
        static void UpdateChestMap(ChestDefinition __instance, ref ChestInstance erasedInstance) {
            uint id = erasedInstance.commonInfo.instanceId;
            if (WormholeManager.chestChannelMap.ContainsKey(id)) {
                int numChestsInChannel = WormholeManager.GetNumChestsInChannel(WormholeManager.chestChannelMap[erasedInstance.commonInfo.instanceId]);
                if (numChestsInChannel > 1) {
                    foreach(ResourceStack stack in erasedInstance.GetInventory().myStacks) {
                        if (!stack.isEmpty) {
                            Player.instance.inventory.TryRemoveResources(stack.info.uniqueId, stack.count);
                        }
                    }
                }

                WormholeManager.chestChannelMap.Remove(id);
                WormholeManager.CheckForEmptyChannels();
            }
        }
    }
}
