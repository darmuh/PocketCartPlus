using BepInEx.Configuration;

namespace PocketCartPlus
{
    internal static class ModConfig
    {
        internal static ConfigEntry<bool> DeveloperLogging = null!;


        internal static void Init()
        {
            DeveloperLogging = Plugin.instance.Config.Bind<bool>("Debug", "Developer Logging", false, new ConfigDescription("Enable this to see developer logging output"));
        }
    }
}
