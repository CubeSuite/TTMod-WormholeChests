using BepInEx;
using EquinoxsModUtils;
using HarmonyLib;
using JetBrains.Annotations;
using Microsoft.Win32.SafeHandles;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using static Voxeland5.PosTab;

namespace WormholeChests
{
    public static class ChestGUI
    {
        // Objects & Variables
        public static bool shouldShowGUI = false;
        public static uint currentChestID = 0;
        public static bool showLinkedLabel = false;

        // Field Values
        public static string lastChannel;
        public static string channel;

        // Textures
        private static Texture2D textBoxNormal;
        private static Texture2D textBoxHover;

        // Settings
        public static bool freeChests => WormholeChestsPlugin.freeWormholeChests.Value;
        public static float channelBoxXOffset => WormholeChestsPlugin.channelBoxXOffset.Value;
        public static float channelBoxYOffset => WormholeChestsPlugin.channelBoxYOffset.Value;
        public static float channelBoxWidth => WormholeChestsPlugin.channelBoxWidth.Value;
        public static float createButtonXOffset => WormholeChestsPlugin.createButtonXOffset.Value;

        public static float xPos => (Screen.width / 2f) + channelBoxXOffset;
        public static float yPos => (Screen.height / 2f) + channelBoxYOffset;

        // Public Functions

        public static void LoadImages() {
            LoadImage("WormholeChests.Images.Border240x40.png", ref textBoxNormal);
            LoadImage("WormholeChests.Images.BorderHover240x40.png", ref textBoxHover);
            LoadImage("WormholeChests.Images.Void.png", ref WormholeChestsPlugin.wormholeTexture);
        }

        public static void DrawChestGUI() {
            HandleKeyPresses();
            DrawChannelBox();
            
            if (channel == "") return;
            
            DrawCreateButton();
            DrawLinkedLabel();

            lastChannel = channel;
        }

        // Private Functions

        private static void LoadImage(string path, ref Texture2D output) {
            Assembly assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(path)) {
                if (stream == null) {
                    Debug.LogError($"Could not find button background image");
                    return;
                }

                using (MemoryStream memoryStream = new MemoryStream()) {
                    stream.CopyTo(memoryStream);
                    byte[] fileData = memoryStream.ToArray();

                    output = new Texture2D(2, 2);
                    output.LoadImage(fileData);
                }
            }
        }

        private static void HandleKeyPresses() {
            if ((Event.current.keyCode == KeyCode.Tab || Event.current.character == '\t') &&
                 Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout) {
                 Event.current.Use();
            }

            if (UnityInput.Current.GetKey(KeyCode.Escape) || UnityInput.Current.GetKey(KeyCode.Tab)) {
                UIManager.instance.inventoryAndStorageMenu.Close();
            }

            InputHandler.instance.uiInputBlocked = true;
        }

        private static void DrawChannelBox() {
            GUIStyle textBoxStyle = new GUIStyle() {
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white, background = textBoxNormal },
                hover = { textColor = Color.white, background = textBoxHover }
            };
            GUIStyle hintLabelStyle = new GUIStyle() {
                fontSize = 16,
                padding = new RectOffset(10, 0, 0, 0),
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.gray, background = null }
            };

            channel = GUI.TextField(new Rect(xPos, yPos, channelBoxWidth, 40), channel, textBoxStyle);
            if (channel == "") {
                GUI.Label(new Rect(xPos + 2, yPos, channelBoxWidth - 10, 40), "Channel", hintLabelStyle);
            }
        }

        private static void DrawCreateButton() {
            GUIStyle buttonStyle = new GUIStyle() {
                fontSize = 16,
                padding = new RectOffset(10, 0, 0, 0),
                alignment = freeChests ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft,
                normal = { textColor = Color.white, background = textBoxNormal },
                hover = { textColor = Color.white, background = textBoxHover }
            };

            ChestInstance aimedChest = WormholeManager.GetAimedAtChest();
            bool exists = WormholeManager.DoesChannelExist(channel);
            if (exists && WormholeManager.chestChannelMap.ContainsKey(aimedChest.commonInfo.instanceId)) showLinkedLabel = true;

            string buttonText = exists ? "Link" : "Create";
            if (GUI.Button(new Rect(xPos + createButtonXOffset, yPos, channelBoxWidth, 40), buttonText, buttonStyle)) {
                if(!freeChests) CheckAndRemoveCores();
                
                Inventory aimedInventory = aimedChest.GetInventory();
                if (!exists) {
                    Inventory newInventory = new Inventory();
                    newInventory.CopyFrom(ref aimedInventory);
                    WormholeManager.AddWormhole(new Wormhole() {
                        channel = channel,
                        inventory = newInventory
                    });
                }
                else {
                    Inventory oldInvenry = aimedChest.GetInventory();
                    aimedChest.commonInfo.inventories[0] = WormholeManager.GetWormhole(channel).inventory;
                    foreach(ResourceStack stack in oldInvenry.myStacks) {
                        if (stack.isEmpty) continue;
                        ChestInstance.AddResources(ref aimedChest, stack.info.uniqueId, out int remainder, stack.count);
                    }
                }

                showLinkedLabel = true;
                WormholeManager.chestChannelMap[currentChestID] = channel;

                GUI.SetNextControlName(" ");
                GUI.Label(new Rect(-100, -100, 1, 1), "");
                GUI.FocusControl(" ");
            }
            
            if (!freeChests) DrawCostGUI();
        }

        private static void CheckAndRemoveCores() {
            float cost = WormholeManager.GetCostToCreateOrLink();
            bool canAfford = TechTreeState.instance.NumCoresAvailable(ResearchCoreDefinition.CoreType.Green) >= cost;
            if (!canAfford) {
                Player.instance.audio.buildError.PlayRandomClip(true);
                return;
            }

            Player.instance.audio.buildClick.PlayRandomClip(true);
            TechTreeState.instance.usedResearchCores[(int)ResearchCoreDefinition.CoreType.Green] += WormholeManager.GetCostToCreateOrLink();
        }

        private static void DrawCostGUI() {
            float cost = WormholeManager.GetCostToCreateOrLink();
            bool canAfford = TechTreeState.instance.NumCoresAvailable(ResearchCoreDefinition.CoreType.Green) >= cost;
            string costString = $"Cost: {cost}";

            GUIStyle costLabelStyle = new GUIStyle() {
                fontSize = 16,
                alignment = TextAnchor.MiddleRight,
                normal = { 
                    textColor = canAfford ? Color.green : Color.red, 
                    background = null 
                }
            };

            GUIStyle blueCoresBoxStyle = new GUIStyle() {
                normal = { background = null },
                hover = { background = null },
                active = { background = null },
                focused = { background = null },
                onNormal = { background = null },
                onHover = { background = null },
                onActive = { background = null },
                onFocused = { background = null },
            };

            GUI.Box(new Rect(xPos + createButtonXOffset + channelBoxWidth - 35, yPos + 5, 30, 30), ModUtils.GetImageForResource(ResourceNames.ResearchCore480nmBlue), blueCoresBoxStyle);
            GUI.Label(new Rect(xPos + createButtonXOffset + 10, yPos + 5, channelBoxWidth - 50, 30), costString, costLabelStyle);
        }

        private static void DrawLinkedLabel() {
            if (!string.IsNullOrEmpty(lastChannel) && lastChannel != channel) showLinkedLabel = false;

            if (showLinkedLabel) {
                GUIStyle linkedLabelStyle = new GUIStyle() {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.green, background = textBoxNormal }
                };
                GUI.Box(new Rect(xPos + createButtonXOffset, yPos, channelBoxWidth, 40), "Linked!", linkedLabelStyle);
            }
        }
    }
}
