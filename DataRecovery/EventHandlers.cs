using DataRecovery.Commands;
using Exiled.API.Features;
using Exiled.Events.EventArgs;

namespace DataRecovery
{
	class EventHandlers
	{
		internal void OnWaitingForPlayers() => Recovery.ResetData();

		internal void OnPlayerDie(DyingEventArgs ev) => RemoveCompletedPlayer(ev.Target);

		internal void OnChangeRole(ChangingRoleEventArgs ev) => RemoveCompletedPlayer(ev.Player);

		private void RemoveCompletedPlayer(Player player)
		{
			if (player != null && Recovery.HasCompletedRecovery(player))
			{
				Recovery.RemoveCompletedPlayer(player);
			}
		}
	}
}
