using DataRecovery.Commands;
using DataRecovery.Patches;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using PluginAPI.Events;
using static RoundSummary;

namespace DataRecovery
{
	class EventHandlers
	{
		[PluginEvent(ServerEventType.WaitingForPlayers)]
		internal void OnWaitingForPlayers(WaitingForPlayersEvent _)
		{
			Recovery.ResetData();
			RoundEndPatch.LeadingTeamOverride = (LeadingTeam)CustomLeadingTeam.None;
		}

		[PluginEvent(ServerEventType.PlayerDying)]
		internal void OnPlayerDying(PlayerDyingEvent ev) => RemoveCompletedPlayer(ev.Player);

		[PluginEvent(ServerEventType.PlayerChangeRole)]
		internal void OnPlayerChangeRole(PlayerChangeRoleEvent ev) => RemoveCompletedPlayer(ev.Player);

		private void RemoveCompletedPlayer(Player player)
		{
			if (player != null && Recovery.HasCompletedRecovery(player))
			{
				Recovery.RemoveCompletedPlayer(player);
			}
		}
	}
}
