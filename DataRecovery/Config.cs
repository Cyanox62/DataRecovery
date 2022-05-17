using Exiled.API.Enums;
using Exiled.API.Interfaces;
using System.Collections.Generic;
using System.ComponentModel;

namespace DataRecovery
{
	public class Config : IConfig
	{
		public bool IsEnabled { get; set; } = true;

		[Description("The rooms in which revocery is allowed.")]
		public Dictionary<RoomType, string> RecoveryRooms { get; set; } = new Dictionary<RoomType, string>()
		{
			{ RoomType.HczServers, "HCZ Servers" }
		};

		[Description("The time a full recovery takes.")]
		public float RecoveryTime { get; set; } = 30f;

		[Description("The amount of tickets MTF get from completing a data recovery.")]
		public int RecoveryMtfTickets { get; set; } = 5;

		[Description("The amount of tickets Chaos get from completing a data recovery.")]
		public int RecoveryChaosTickets { get; set; } = 5;

		[Description("The broadcast to display to all players upon a data recovery starting.")]
		public string RecoveryBroadcast { get; set; } = "<i>{player} has started a data recovery in {room}!</i>";

		[Description("The amount of time to display the data recovery broadcast.")]
		public int RecoveryBroadcastTime { get; set; } = 5;

		[Description("The broadcast to display to all players upon a completed data extraction.")]
		public string ExtractionBroadcast { get; set; } = "<i>{player} has extracted data!</i>";

		[Description("The amount of time to display the extraction broadcast.")]
		public int ExtractionBroadcastTime { get; set; } = 5;
	}
}
