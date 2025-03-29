using BepInEx.Logging;
using BepInEx;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using System.IO;

namespace PocketCartPlus
{
    [BepInPlugin("com.github.darmuh.PocketCartPlus", "PocketCart Plus", (PluginInfo.PLUGIN_VERSION))]
    [BepInDependency("REPOLib", "1.5.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin instance = null!;
        public static class PluginInfo
        {
            public const string PLUGIN_GUID = "com.github.darmuh.PocketCartPlus";
            public const string PLUGIN_NAME = "PocketCart Plus";
            public const string PLUGIN_VERSION = "0.2.1";
        }

        internal static ManualLogSource Log = null!;
        internal static System.Random Rand = new();

        private void Awake()
        {
            instance = this;
            Log = base.Logger;
            Log.LogInfo($"{PluginInfo.PLUGIN_NAME} is loading with version {PluginInfo.PLUGIN_VERSION}!");
            ModConfig.Init();
            string pluginFolderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string assetBundleFilePath = Path.Combine(pluginFolderPath, "pocketcartplus");
            REPOLib.BundleLoader.LoadBundle(assetBundleFilePath, Assembly.GetExecutingAssembly().Location);
            
            //Config.ConfigReloaded += OnConfigReloaded;
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

            Log.LogInfo($"{PluginInfo.PLUGIN_NAME} load complete!");
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
