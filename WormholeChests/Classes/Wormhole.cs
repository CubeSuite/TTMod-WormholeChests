using EquinoxsModUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace WormholeChests
{
    public static class WormholeManager 
    {
        // Objects & Variables
        public static Dictionary<string, Wormhole> wormholes = new Dictionary<string, Wormhole>();
        public static Dictionary<uint, string> chestChannelMap = new Dictionary<uint, string>();
        private static string dataFolder => $"{Application.persistentDataPath}/WormholeChests";

        // Wormhole Functions

        public static void AddWormhole(Wormhole wormhole) {
            wormholes.Add(wormhole.channel, wormhole);
        }

        public static void UpdateWormhole(Wormhole wormhole) {
            wormholes[wormhole.channel] = wormhole;
        }

        public static Wormhole GetWormhole(string channel) {
            return wormholes[channel];
        }
        
        public static Inventory GetInventoryForChest(uint chestID) {
            return GetWormhole(chestChannelMap[chestID]).inventory;
        }

        public static Inventory GetInventoryForChest(ChestInstance chestInstance) {
            return GetInventoryForChest(chestInstance.commonInfo.instanceId);
        }

        public static List<Wormhole> GetAllWormholes() {
            return wormholes.Values.ToList();
        }

        public static void CheckForEmptyChannels() {
            for(int i = 0; i < wormholes.Count;) {
                string channel = wormholes.Keys.ToList()[i];
                if (GetNumChestsInChannel(channel) == 0) {
                    wormholes.Remove(channel);
                }
                else {
                    i++;
                }
            }
        }

        // General Public Functions

        public static bool DoesChannelExist(string channel) {
            return wormholes.ContainsKey(channel);
        }

        public static ChestInstance GetAimedAtChest() {
            GenericMachineInstanceRef machine = (GenericMachineInstanceRef)ModUtils.GetPrivateField("targetMachineRef", Player.instance.interaction);
            return MachineManager.instance.Get<ChestInstance, ChestDefinition>(machine.index, MachineTypeEnum.Chest);
        }

        public static bool IsChestWormholeChest(ChestInstance chest) {
            return chestChannelMap.ContainsKey(chest.commonInfo.instanceId);
        }

        public static int GetCostToCreateOrLink() {
            return Mathf.CeilToInt(100f * Mathf.Pow(1.05f, chestChannelMap.Count));
        }

        public static int GetNumChestsInChannel(string channel) {
            int count = 0;
            foreach(string savedChannel in chestChannelMap.Values) {
                if (savedChannel.Equals(channel)) {
                    count++;
                }
            }

            return count;
        }

        // Data Functions

        public static void SaveData(string worldName) {
            Directory.CreateDirectory(dataFolder);
            Directory.CreateDirectory($"{dataFolder}/{worldName}");

            string wormholesSaveFile = $"{dataFolder}/{worldName}/Wormholes.txt";
            List<string> lines = new List<string>();
            foreach(Wormhole wormhole in GetAllWormholes()) lines.Add(wormhole.Serialise());
            File.WriteAllLines(wormholesSaveFile, lines);

            string chestChannelsMapSaveFile = $"{dataFolder}/{worldName}/ChestChannelMap.txt";
            List<string> mapLines = new List<string>();
            foreach(KeyValuePair<uint, string> pair in chestChannelMap) {
                mapLines.Add($"{pair.Key}|{pair.Value}");
            }

            File.WriteAllLines(chestChannelsMapSaveFile, mapLines);
        }

        public static void LoadData(string worldName) {
            string wormholesSaveFile = $"{dataFolder}/{worldName}/Wormholes.txt";
            if (!File.Exists(wormholesSaveFile)) return;

            wormholes.Clear();
            string[] lines = File.ReadAllLines(wormholesSaveFile);
            foreach(string line in lines) {
                AddWormhole(new Wormhole(line));
            }

            string chestChannelsMapSaveFile = $"{dataFolder}/{worldName}/ChestChannelMap.txt";
            chestChannelMap.Clear();
            string[] mapLines = File.ReadAllLines(chestChannelsMapSaveFile);
            foreach(string line in mapLines) {
                uint id = uint.Parse(line.Split('|')[0]);
                string channel = line.Split('|')[1];
                chestChannelMap.Add(id, channel);
            }
        }
    }

    public class Wormhole
    {
        public string channel;
        public Inventory inventory = new Inventory() { 
            myStacks = new ResourceStack[56],
            numSlots = 56
        };

        // Public Functions
        
        public string Serialise() {
            string result = channel;
            if(inventory.myStacks != null) {
                foreach (ResourceStack stack in inventory.myStacks) {
                    if (stack.isEmpty) continue;
                    result += $"|{stack.info.uniqueId},{stack.count}";
                }
            }
            else {
                result += "|null,null";
            }

            return result;
        }

        // Constructors

        public Wormhole(){}
        public Wormhole(string serial) {
            for(int i = 0; i < 56; i++) {
                inventory.myStacks[i] = ResourceStack.CreateEmptyStack();
            }

            string[] parts = serial.Split('|');
            channel = parts[0];
            for(int i = 1; i < parts.Count(); i++) {
                string[] subParts = parts[i].Split(',');
                string resIDString = subParts[0];
                string countString = subParts[1];

                if (resIDString == "null" || countString == "null") continue;

                int resID = int.Parse(resIDString);
                int count = int.Parse(countString);
                inventory.AddResources(resID, count);
            }
        }
    }
}
