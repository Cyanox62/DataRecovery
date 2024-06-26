using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using static RoundSummary;
using static HarmonyLib.AccessTools;

namespace DataRecovery.Patches
{
	[HarmonyPatch(typeof(RoundSummary), nameof(RoundSummary._ProcessServerSideCode), MethodType.Enumerator)]
	public class RoundEndPatch
	{
		internal static LeadingTeam LeadingTeamOverride { get; set; } = (LeadingTeam)CustomLeadingTeam.None;

		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			List<CodeInstruction> newInstructions = NorthwoodLib.Pools.ListPool<CodeInstruction>.Shared.Rent(instructions);

			int index = newInstructions.FindLastIndex(i => i.opcode == OpCodes.Ldloc_S && i.operand is LocalBuilder local && local.LocalType == typeof(bool) && local.LocalIndex == 5) + 10;

			Label skipLabel = generator.DefineLabel();
			newInstructions[index].WithLabels(skipLabel);

			newInstructions.InsertRange(index, new[]
			{
				new CodeInstruction(OpCodes.Call, PropertyGetter(typeof(RoundEndPatch), nameof(LeadingTeamOverride))),
				new CodeInstruction(OpCodes.Ldc_I4, (int)CustomLeadingTeam.None),
				new CodeInstruction(OpCodes.Beq, skipLabel),

				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Call, PropertyGetter(typeof(RoundEndPatch), nameof(LeadingTeamOverride))),
				new CodeInstruction(newInstructions[index - 1]),
			});

			for (int i = 0; i < newInstructions.Count; i++)
				yield return newInstructions[i];

			NorthwoodLib.Pools.ListPool<CodeInstruction>.Shared.Return(newInstructions);
		}
	}
}
