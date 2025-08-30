using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Photon.Pun;
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace PocketCartPlus
{
    [BepInPlugin("com.github.darmuh.PocketCartPlus", "PocketCart Plus", (PluginInfo.PLUGIN_VERSION))]
    [BepInDependency("REPOLib", "2.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin instance = null!;
        public static class PluginInfo
        {
            public const string PLUGIN_GUID = "com.github.darmuh.PocketCartPlus";
            public const string PLUGIN_NAME = "PocketCart Plus";
            public const string PLUGIN_VERSION = "0.4.2";
        }

        internal static ManualLogSource Log = null!;
        internal static bool BundleLoaded = false;
        internal static System.Random Rand = new();
        internal static AssetBundle Bundle = null!;
        internal static AssetBundle PocketDimensionBundle = null!;
        internal static GameObject PocketDimension = null!;
        internal static GameObject HintPrefab = null!;

        private void Awake()
        {
            instance = this;
            Log = base.Logger;
            Log.LogInfo($"{PluginInfo.PLUGIN_NAME} is loading with version {PluginInfo.PLUGIN_VERSION}!");
            Log.LogInfo($"This version of the mod has been compiled for REPO version 0.2.1 :)");
            ModConfig.Init();
            string pluginFolderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string assetBundleFilePath = Path.Combine(pluginFolderPath, "upgrade_cartitems");
            string pocketDimension = Path.Combine(pluginFolderPath, "pocketdimension");
            //string hintUI = Path.Combine(pluginFolderPath, "hintui");
            //REPOLib.BundleLoader.LoadBundle(hintUI, BundleLoader, false);
            REPOLib.BundleLoader.LoadBundle(pocketDimension, BundleLoader, false);
            REPOLib.BundleLoader.LoadBundle(assetBundleFilePath, BundleLoader, true);
            
            Config.ConfigReloaded += OnConfigReloaded;
            Config.SettingChanged += OnSettingChanged;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

            Log.LogInfo($"{PluginInfo.PLUGIN_NAME} load complete!");
        }

        public void OnConfigReloaded(object sender, EventArgs e)
        {
            Log.LogDebug("Config has been reloaded!");
            if(PhotonNetwork.MasterClient != null)
                HostValues.StartGame();
        }

        public void OnSettingChanged(object sender, SettingChangedEventArgs settingChangedArg)
        {
            if (settingChangedArg.ChangedSetting == null)
                return;

            HostValues.UpdateValue(settingChangedArg.ChangedSetting);
        }

        private static IEnumerator BundleLoader(AssetBundle mybundle)
        {
            if(mybundle.name == "upgrade_cartitems")
            {
                Bundle = mybundle;
                yield return null;
                Spam("Asset bundle has been loaded");
                BundleLoaded = true;
            }
            else if(mybundle.name == "pocketdimension")
            {
                PocketDimensionBundle = mybundle;
                PocketDimension = PocketDimensionBundle.LoadAsset<GameObject>("PocketDimension");
                Spam("PocketDimension bundle loaded!");
            }
            
        }

        internal static void Message(string message)
        {
            Log.LogMessage(message);
        }

        internal static void Spam(string message)
        {
            if (ModConfig.DeveloperLogging.Value)
                Log.LogDebug(message);
            else
                return;
        }

        internal static void ERROR(string message)
        {
            Log.LogError(message);
        }

        internal static void WARNING(string message)
        {
            Log.LogWarning(message);
        }
    }
}
