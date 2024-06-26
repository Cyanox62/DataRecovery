using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using CommandSystem;
using DataRecovery.Patches;
using MapGeneration;
using MEC;
using PlayerRoles;
using PluginAPI.Core;
using RemoteAdmin;
using Respawning;
using static RoundSummary;

namespace DataRecovery.Commands
{
	[CommandHandler(typeof(ClientCommandHandler))]
	class Recovery : ICommand
	{
		public string[] Aliases { get; set; } = Array.Empty<string>();

		public string Description { get; set; } = "Initiates a data recovery";

		public string Command { get; } = "recover";

		bool ICommand.SanitizeResponse => true;

		internal static LeadingTeam OverrideEndConditions = (LeadingTeam)CustomLeadingTeam.None;

		private static Player recoveryPlayer = null;
		private static CoroutineHandle recoveryCoroutine;
		private static CoroutineHandle completedCoroutine;
		private static List<Player> completedRecoveries = new List<Player>();
		private static bool hasExtracted = false;

		private bool canDisplayBroadcast = true;

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			RoundEndPatch.LeadingTeamOverride = LeadingTeam.Anomalies;

			if (sender is PlayerCommandSender playerSender)
			{
				Player player = Player.Get(playerSender);
				if (player != null)
				{
					if (IsMtfTeam(player) || IsChaosTeam(player))
					{
						if (!hasExtracted)
						{
							if (HasItem(player, ItemType.KeycardChaosInsurgency))
							{
								if (recoveryPlayer == null)
								{
									if (!completedRecoveries.Contains(player))
									{
										if (player.Room != null && Plugin.Singleton.Config.RecoveryRooms.ContainsKey(player.Room.Name))
										{
											recoveryPlayer = player;
											recoveryCoroutine = Timing.RunCoroutine(RecoveryCoroutine(player));
											if (canDisplayBroadcast)
											{
												canDisplayBroadcast = false;
												Timing.CallDelayed(Plugin.Singleton.Config.RecoveryBroadcastTime, () => canDisplayBroadcast = true);
												Map.Broadcast((ushort)Plugin.Singleton.Config.RecoveryBroadcastTime, Plugin.Singleton.Config.RecoveryBroadcast.Replace("{player}", player.Nickname).Replace("{room}", Plugin.Singleton.Config.RecoveryRooms[player.Room.Name]).Replace("{teamcolor}", GetTeamColor(player)));
											}
											response = "Initiating Data Recovery...";
											return true;
										}
										else
										{
											player.ReceiveHint($"\n\n\n\n\n\n\n<b><color={GetTeamColor(player)}>Data Recovery Failed!</color></b>\nYou cannot recover data from this room", 3f);
											response = "You cannot recover data from this room.";
											return false;
										}
									}
									else
									{
										response = "You have already completed data recovery! Escape to surface!";
										return false;
									}
								}
								else if (recoveryPlayer == player)
								{
									response = "You are already recovering data!";
									return false;
								}
								else
								{
									KillDataRecovery();
									player.ReceiveHint($"\n\n\n\n\n\n\n<b><color={GetTeamColor(player)}>Data Recovery Failed!</color></b>\nAnother player was recovering data\nYou have <color=red>cancelled</color> their data recovery", 5f);
									response = "Another player was recovering data. You have cancelled their recovery!";
									return false;
								}
							}
							else
							{
								player.ReceiveHint($"\n\n\n\n\n\n\n<b><color={GetTeamColor(player)}>Data Recovery Failed!</color></b>\nYou must have a Chaos Insurgency Device", 3f);
								response = "You must have a Chaos Insurgency Device in order to recover data.";
								return false;
							}
						}
						else
						{
							player.ReceiveHint($"\n\n\n\n\n\n\n<b><color={GetTeamColor(player)}>No Data Present - Drives Wiped</color></b>", 3f);
							response = "No Data Present - Drives Wiped";
							return false;
						}
					}
					else
					{
						response = "Only MTF and Chaos may recover data.";
						return false;
					}
				}
				else
				{
					response = "Internal command error (CODE 2)";
					return false;
				}
			}
			else
			{
				response = "Only players may use this command!";
				return false;
			}
		}

		private IEnumerator<float> RecoveryCoroutine(Player player)
		{
			RoomIdentifier room = player.Room;
			for (float i = Plugin.Singleton.Config.RecoveryTime; i >= 0f; i -= 0.5f)
			{
				player.ReceiveHint($"\n\n\n\n\n\n\n<b><color={GetTeamColor(player)}>Recovering Data...</color></b>\n<color=white>Time Left: </color><color=red>{(int)i} seconds</color>", 1f);
				yield return Timing.WaitForSeconds(0.5f);
				if (player.Room != room)
				{
					player.ReceiveHint($"\n\n\n\n\n\n\n<b><color={GetTeamColor(player)}>Data Recovery Failed!</color></b>\nYou have left the room", 3f);
					recoveryPlayer = null;
					yield break;
				}
				if (!HasItem(player, ItemType.KeycardChaosInsurgency))
				{
					player.ReceiveHint($"\n\n\n\n\n\n\n<b><color={GetTeamColor(player)}>Data Recovery Failed!</color></b>\nYou no longer have a Chaos Insurgency Device", 3f);
					recoveryPlayer = null;
					yield break;
				}
				if (!player.IsAlive) yield break;
			}

			player.ReceiveHint($"\n\n\n\n\n\n\n<b><color={GetTeamColor(player)}>Data Recovery Complete!</color></b>\n<color=white>Escape to surface to finish extraction!</color>", 5f);
			if (completedRecoveries.Count == 0) completedCoroutine = Timing.RunCoroutine(CompletedCoroutine());
			completedRecoveries.Add(player);
			KillDataRecovery();
		}

		private void KillDataRecovery()
		{
			if (recoveryPlayer != null)
			{
				recoveryPlayer = null;
				if (recoveryCoroutine.IsRunning) Timing.KillCoroutines(recoveryCoroutine);
			}
		}

		private IEnumerator<float> CompletedCoroutine()
		{
			while (Round.IsRoundStarted)
			{
				yield return Timing.WaitForSeconds(5f);
				foreach (Player player in completedRecoveries)
				{
					if (player.Room.Zone == FacilityZone.Surface)
					{
						completedRecoveries.Remove(player);

						List<Player> scpList = Player.GetPlayers().Where(x => x.Team == Team.SCPs).ToList();
						Player scp079 = scpList.FirstOrDefault(x => x.Role == RoleTypeId.Scp079);

						if (scpList.Count == 0) // No SCPs alive
						{
							if (IsMtfTeam(player))
							{
								RespawnTokensManager.ForceTeamDominance(SpawnableTeamType.NineTailedFox,
									RespawnTokensManager.GetTeamDominance(SpawnableTeamType.NineTailedFox) + Plugin.Singleton.Config.GenericRecoveryMtfPercent / 100f);
							}
							else if (IsChaosTeam(player))
							{
								RespawnTokensManager.ForceTeamDominance(SpawnableTeamType.ChaosInsurgency,
									RespawnTokensManager.GetTeamDominance(SpawnableTeamType.ChaosInsurgency) + Plugin.Singleton.Config.GenericRecoveryChaosPercent / 100f);
							}
						}
						else if (scp079 != null)
						{
							if (scpList.Count == 1) // Only SCP-079 alive
							{
								if (IsChaosTeam(player))
								{
									RoundEndPatch.LeadingTeamOverride = LeadingTeam.Anomalies;
								}
								else if (IsMtfTeam(player))
								{
									RoundEndPatch.LeadingTeamOverride = LeadingTeam.FacilityForces;
								}
								Round.End();
							}
							else // More than SCP-079 alive
							{
								if (IsChaosTeam(player))
								{
									scp079.Kill("Data extracted.", Plugin.Singleton.Config.MultipleScpsExtractionChaosCassieMessage);
									scp079.SetRole(RoleTypeId.ChaosRepressor, RoleChangeReason.RemoteAdmin);
									RespawnTokensManager.ForceTeamDominance(SpawnableTeamType.ChaosInsurgency,
										RespawnTokensManager.GetTeamDominance(SpawnableTeamType.ChaosInsurgency) + Plugin.Singleton.Config.MultipleScpsRecoveryChaosPercent / 100f);
								}
								else if (IsMtfTeam(player))
								{
									scp079.Kill("Data extracted.", Plugin.Singleton.Config.MultipleScpsExtractionMtfCassieMessage);
									scp079.SetRole(RoleTypeId.NtfCaptain, RoleChangeReason.RemoteAdmin);
									RespawnTokensManager.ForceTeamDominance(SpawnableTeamType.NineTailedFox,
										RespawnTokensManager.GetTeamDominance(SpawnableTeamType.NineTailedFox) + Plugin.Singleton.Config.MultipleScpsRecoveryMtfPercent / 100f);
								}
							}
						}

						player.ReceiveHint($"\n\n\n\n\n\n\n<b><color={GetTeamColor(player)}>Data Extracted!</color></b>\n<color=white>You have gained a {(IsMtfTeam(player) ? Plugin.Singleton.Config.GenericRecoveryMtfPercent : Plugin.Singleton.Config.GenericRecoveryChaosPercent)}% spawn boost for your team!</color>", 5f);
						hasExtracted = true;
						Map.Broadcast((ushort)Plugin.Singleton.Config.ExtractionBroadcastTime, Plugin.Singleton.Config.ExtractionBroadcast.Replace("{player}", player.Nickname).Replace("{teamcolor}", GetTeamColor(player)));
						yield break;
					}
				}
			}
		}

		private static bool HasItem(Player player, ItemType itemType)
		{
			foreach (var item in player.ReferenceHub.inventory.UserInventory.Items)
			{
				if (item.Value.ItemTypeId == itemType) return true;
			}
			return false;
		}

		internal static void ResetData()
		{
			Timing.KillCoroutines(completedCoroutine);
			Timing.KillCoroutines(recoveryCoroutine);
			recoveryPlayer = null;
			completedRecoveries.Clear();
			hasExtracted = false;
		}

		internal static bool HasCompletedRecovery(Player player) => completedRecoveries.Contains(player);
		internal static void RemoveCompletedPlayer(Player player)
		{
			if (completedRecoveries.Contains(player)) completedRecoveries.Remove(player);
		}

		private static bool IsMtfTeam(Player player) => player.Team == Team.FoundationForces || player.Team == Team.Scientists;
		private static bool IsChaosTeam(Player player) => player.Team == Team.ChaosInsurgency || player.Team == Team.ClassD;

		private static string GetTeamColor(Player player)
		{
			switch (player.Role)
			{
				case RoleTypeId.ChaosConscript:
					return "#03811a";
				case RoleTypeId.ChaosMarauder:
					return "#045d22";
				case RoleTypeId.ChaosRepressor:
					return "#0c7732";
				case RoleTypeId.ChaosRifleman:
					return "#07771a";
				case RoleTypeId.ClassD:
					return "#ffae00";
				case RoleTypeId.FacilityGuard:
					return "#bfbfbf";
				case RoleTypeId.NtfPrivate:
					return "#6ab9f1";
				case RoleTypeId.NtfCaptain:
					return "#003dcb";
				case RoleTypeId.NtfSergeant:
					return "#058df1";
				case RoleTypeId.NtfSpecialist:
					return "#0390f5";
				case RoleTypeId.Scientist:
					return "#ffff7c";
				default:
					return "#ffffff";
			}
		}
	}
}
