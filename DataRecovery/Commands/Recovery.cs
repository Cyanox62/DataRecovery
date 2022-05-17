using System;
using System.Collections.Generic;
using CommandSystem;
using Exiled.API.Features;
using MEC;
using RemoteAdmin;

namespace DataRecovery.Commands
{
	[CommandHandler(typeof(ClientCommandHandler))]
	class Recovery : ICommand
	{
		public string[] Aliases { get; set; } = Array.Empty<string>();

		public string Description { get; set; } = "Initiates a data recovery";

		string ICommand.Command { get; } = "recover";

		private static Player recoveryPlayer = null;
		private static CoroutineHandle recoveryCoroutine;
		private static CoroutineHandle completedCoroutine;
		private static List<Player> completedRecoveries = new List<Player>();
		private static bool hasExtracted = false;

		private bool canDisplayBroadcast = true;

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (sender is PlayerCommandSender playerSender)
			{
				Player player = Player.Get(playerSender);
				if (player != null)
				{
					if (player.Role.Team == Team.MTF || player.Role.Team == Team.CHI)
					{
						if (!hasExtracted)
						{
							if (player.HasItem(ItemType.KeycardChaosInsurgency))
							{
								if (recoveryPlayer == null)
								{
									if (!completedRecoveries.Contains(player))
									{
										if (player.CurrentRoom != null && Plugin.singleton.Config.RecoveryRooms.ContainsKey(player.CurrentRoom.Type))
										{
											recoveryPlayer = player;
											recoveryCoroutine = Timing.RunCoroutine(RecoveryCoroutine(player));
											if (canDisplayBroadcast)
											{
												canDisplayBroadcast = false;
												Timing.CallDelayed(Plugin.singleton.Config.RecoveryBroadcastTime, () => canDisplayBroadcast = true);
												Map.Broadcast((ushort)Plugin.singleton.Config.RecoveryBroadcastTime, Plugin.singleton.Config.RecoveryBroadcast.Replace("{player}", player.Nickname).Replace("{room}", Plugin.singleton.Config.RecoveryRooms[player.CurrentRoom.Type]));
											}
											response = "Initiating Data Recovery...";
											return true;
										}
										else
										{
											player.ShowHint($"\n\n\n\n\n\n\n<b><color=#{(player.Role.Team == Team.MTF ? "058df1" : "03811a")}>Data Recovery Failed!</color></b>\nYou cannot recover data from this room", 3f);
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
									player.ShowHint($"\n\n\n\n\n\n\n<b><color=#{(player.Role.Team == Team.MTF ? "058df1" : "03811a")}>Data Recovery Failed!</color></b>\nAnother player was recovering data\nYou have <color=red>cancelled</color> their data recovery", 5f);
									response = "Another player was recovering data. You have cancelled their recovery!";
									return false;
								}
							}
							else
							{
								player.ShowHint($"\n\n\n\n\n\n\n<b><color=#{(player.Role.Team == Team.MTF ? "058df1" : "03811a")}>Data Recovery Failed!</color></b>\nYou must have a Chaos Insurgency Device", 3f);
								response = "You must have a Chaos Insurgency Device in order to recover data.";
								return false;
							}
						}
						else
						{
							player.ShowHint($"\n\n\n\n\n\n\n<b><color=#{(player.Role.Team == Team.MTF ? "058df1" : "03811a")}>No Data Present - Drives Wiped</color></b>", 3f);
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
			Room room = player.CurrentRoom;
			for (float i = Plugin.singleton.Config.RecoveryTime; i >= 0f; i -= 0.5f)
			{
				player.ShowHint($"\n\n\n\n\n\n\n<b><color=#{(player.Role.Team == Team.MTF ? "058df1" : "03811a")}>Recovering Data...</color></b>\n<color=white>Time Left: </color><color=red>{(int)i} seconds</color>", 1f);
				yield return Timing.WaitForSeconds(0.5f);
				if (player.CurrentRoom != room)
				{
					player.ShowHint($"\n\n\n\n\n\n\n<b><color=#{(player.Role.Team == Team.MTF ? "058df1" : "03811a")}>Data Recovery Failed!</color></b>\nYou have left the room", 3f);
					recoveryPlayer = null;
					yield break;
				}
				if (!player.HasItem(ItemType.KeycardChaosInsurgency))
				{
					player.ShowHint($"\n\n\n\n\n\n\n<b><color=#{(player.Role.Team == Team.MTF ? "058df1" : "03811a")}>Data Recovery Failed!</color></b>\nYou no longer have a Chaos Insurgency Device", 3f);
					recoveryPlayer = null;
					yield break;
				}
				if (!player.IsAlive) yield break;
			}

			player.ShowHint($"\n\n\n\n\n\n\n<b><color=#{(player.Role.Team == Team.MTF ? "058df1" : "03811a")}>Data Recovery Complete!</color></b>\n<color=white>Escape to surface to finish extraction!</color>", 5f);
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
			while (Round.IsStarted)
			{
				yield return Timing.WaitForSeconds(5f);
				foreach (Player player in completedRecoveries)
				{
					if (player.CurrentRoom.Zone == Exiled.API.Enums.ZoneType.Surface)
					{
						completedRecoveries.Remove(player);
						if (player.Role.Team == Team.MTF)
						{
							Respawn.NtfTickets += (uint)Plugin.singleton.Config.RecoveryMtfTickets;
						}
						else if (player.Role.Team == Team.CHI)
						{
							Respawn.ChaosTickets += (uint)Plugin.singleton.Config.RecoveryChaosTickets;
						}
						bool isMtf = player.Role.Team == Team.MTF;
						player.ShowHint($"\n\n\n\n\n\n\n<b><color=#{(isMtf ? "058df1" : "03811a")}>Data Extracted!</color></b>\n<color=white>You have gained {(isMtf ? Plugin.singleton.Config.RecoveryMtfTickets : Plugin.singleton.Config.RecoveryChaosTickets)} tickets for your team!</color>", 5f);
						hasExtracted = true;
						Map.Broadcast((ushort)Plugin.singleton.Config.ExtractionBroadcastTime, Plugin.singleton.Config.ExtractionBroadcast.Replace("{player}", player.Nickname));
						yield break;
					}
				}
			}
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
	}
}
