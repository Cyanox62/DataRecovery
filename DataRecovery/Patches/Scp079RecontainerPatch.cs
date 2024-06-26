using HarmonyLib;
using PlayerRoles.PlayableScps.Scp079;

namespace DataRecovery.Patches
{
	[HarmonyPatch(typeof(Scp079Recontainer), nameof(Scp079Recontainer.OnServerRoleChanged))]
	public static class Scp079RecontainerPatch
	{
		public static bool Prefix() => !Plugin.Singleton.Config.StopScp079AutoRecontainment;
	}
}
