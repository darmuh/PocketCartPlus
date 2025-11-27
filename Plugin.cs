using System;
using System.Collections;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Photon.Pun;
using REPOLib;
using UnityEngine;

namespace PocketCartPlus
{
    
    [BepInDependency(MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInAutoPlugin]
    public partial class Plugin : BaseUnityPlugin
    {
        internal static Plugin instance = null!;

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
            Log.LogInfo($"{Name} is loading with version {Version}!");
            Log.LogInfo($"This version of the mod has been compiled for REPO version 0.3.1 :)");
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

            Log.LogInfo($"{Name} load complete!");
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
