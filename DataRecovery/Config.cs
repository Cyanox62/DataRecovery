using MapGeneration;
using System.Collections.Generic;
using System.ComponentModel;

namespace DataRecovery
{
	public class Config
	{
		[Description("The rooms in which revocery is allowed.")]
		public Dictionary<RoomName, string> RecoveryRooms { get; set; } = new Dictionary<RoomName, string>()
		{
			{ RoomName.HczServers, "HCZ Servers" }
		};

		[Description("The time a full recovery takes.")]
		public float RecoveryTime { get; set; } = 30f;

		[Description("The token percentage MTF gain from completing a data recovery.")]
		public int GenericRecoveryMtfPercent { get; set; } = 30;

		[Description("The token percentage Chaos gain get from completing a data recovery.")]
		public int GenericRecoveryChaosPercent { get; set; } = 30;

		[Description("The token percentage MTF gain from completing a data recovery while there are multiple SCps alive, SCP-079 included.")]
		public int MultipleScpsRecoveryMtfPercent { get; set; } = 15;

		[Description("The token percentage Chaos gain get from completing a data recovery while there are multiple SCps alive, SCP-079 included")]
		public int MultipleScpsRecoveryChaosPercent { get; set; } = 15;

		[Description("The broadcast to display to all players upon a data recovery starting.")]
		public string RecoveryBroadcast { get; set; } = "<b><color={teamcolor}>{player}</color> has started a data recovery in {room}!</b>";

		[Description("The amount of time to display the data recovery broadcast.")]
		public int RecoveryBroadcastTime { get; set; } = 5;

		[Description("The broadcast to display to all players upon a completed data extraction.")]
		public string ExtractionBroadcast { get; set; } = "<b><color={teamcolor}>{player}</color> has extracted data!</b>";

		[Description("The amount of time to display the extraction broadcast.")]
		public int ExtractionBroadcastTime { get; set; } = 5;

		[Description("The cassie broadcast to play after MTF successfully extract data with multiple SCPs alive, SCP-079 included.")]
		public string MultipleScpsExtractionMtfCassieMessage { get; set; } = "data";

		[Description("The cassie broadcast to play after Chaos successfully extract data with multiple SCPs alive, SCP-079 included.")]
		public string MultipleScpsExtractionChaosCassieMessage { get; set; } = "data";

		[Description("The cassie broadcast to play after Chaos successfully extract data with multiple SCPs alive, SCP-079 included.")]
		public bool StopScp079AutoRecontainment { get; set; } = true;
	}
}
