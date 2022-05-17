using Exiled.API.Features;

namespace DataRecovery
{
    public class Plugin : Plugin<Config>
    {
		internal static Plugin singleton;

		private EventHandlers ev;

		public override void OnEnabled()
		{
			base.OnEnabled();

			singleton = this;

			ev = new EventHandlers();
			Exiled.Events.Handlers.Server.WaitingForPlayers += ev.OnWaitingForPlayers;
			Exiled.Events.Handlers.Player.Dying += ev.OnPlayerDie;
		}

		public override void OnDisabled()
		{
			base.OnDisabled();

			Exiled.Events.Handlers.Server.WaitingForPlayers -= ev.OnWaitingForPlayers;
			Exiled.Events.Handlers.Player.Dying -= ev.OnPlayerDie;
			ev = null;
		}

		public override string Author => "Cyanox";
	}
}
