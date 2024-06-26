using HarmonyLib;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Events;

namespace DataRecovery
{
    public class Plugin
    {
		internal static Plugin Singleton { get; private set; }
		private Harmony Harmony { get; set; }

		[PluginEntryPoint("Data Recovery", "1.1.0", "Implements a custom data recovery minigame.", "Cyanox")]
		void LoadPlugin()
		{
			Singleton = this;

			Harmony = new Harmony(PluginHandler.Get(this).PluginName);
			Harmony.PatchAll();

			EventManager.RegisterAllEvents(this);
		}

		[PluginUnload]
		void UnloadPlugin()
		{
			EventManager.UnregisterAllEvents(this);

			Harmony.UnpatchAll();
		}

		[PluginConfig]
		public Config Config;
	}
}
