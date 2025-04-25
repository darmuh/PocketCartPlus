using BepInEx.Logging;
using BepInEx;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using System.IO;
using System.Collections;

namespace PocketCartPlus
{
    [BepInPlugin("com.github.darmuh.PocketCartPlus", "PocketCart Plus", (PluginInfo.PLUGIN_VERSION))]
    [BepInDependency("REPOLib", "2.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin instance = null!;
        public static class PluginInfo
        {
            public const string PLUGIN_GUID = "com.github.darmuh.PocketCartPlus";
            public const string PLUGIN_NAME = "PocketCart Plus";
            public const string PLUGIN_VERSION = "0.3.0";
        }

        internal static ManualLogSource Log = null!;
        internal static bool BundleLoaded = false;
        internal static System.Random Rand = new();
        internal static AssetBundle Bundle = null!;
        internal static AssetBundle PocketDimensionBundle = null!;
        internal static GameObject PocketDimension = null!;

        private void Awake()
        {
            instance = this;
            Log = base.Logger;
            Log.LogInfo($"{PluginInfo.PLUGIN_NAME} is loading with version {PluginInfo.PLUGIN_VERSION}!");
            ModConfig.Init();
            string pluginFolderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string assetBundleFilePath = Path.Combine(pluginFolderPath, "pocketcartplus");
            string pocketDimension = Path.Combine(pluginFolderPath, "pocketdimension");
            REPOLib.BundleLoader.LoadBundle(pocketDimension, BundleLoader, false);
            REPOLib.BundleLoader.LoadBundle(assetBundleFilePath, BundleLoader, true);
            
            //Config.ConfigReloaded += OnConfigReloaded;
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

            Log.LogInfo($"{PluginInfo.PLUGIN_NAME} load complete!");
        }

        private static IEnumerator BundleLoader(AssetBundle mybundle)
        {
            if(mybundle.name == "pocketcartplus")
            {
                Bundle = mybundle;
                yield return null;
                Spam("Asset bundle has been loaded");
                BundleLoaded = true;
            }
            else
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
