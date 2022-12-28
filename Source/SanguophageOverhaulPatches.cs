using System;
using System.Reflection;
using System.Collections.Generic;
using Verse;
using RimWorld;
using HarmonyLib;

namespace SanguophageOverhaul
{
	[StaticConstructorOnStartup]
	static class SanguophagePatches
	{
		static SanguophagePatches()
		{
			Harmony harmony = new Harmony("Azuraal.SanguophageOverhaul");
			Assembly assembly = Assembly.GetExecutingAssembly();
			harmony.PatchAll(assembly);
		}
	}
	[HarmonyPatch(typeof(Recipe_ImplantXenogerm), "AvailableOnNow")]
	static class ImplantXenogermPatch
	{
		static void Postfix(ref bool __result, Thing thing)
		{
			if(__result && SanguophageOverhaul.Settings.NoCure && SanguophageOverhaul.XenotypeIsVampire(((Pawn)thing).genes))
			{
				List<Thing> xenogerms = ((Pawn)thing).Map.listerThings.ThingsOfDef(ThingDefOf.Xenogerm);
				if(!xenogerms.Exists(x => SanguophageOverhaul.XenogermIsVampire(((Xenogerm)x).GeneSet)))
				{
					__result = false;
				}
			}
		}
	}
	[HarmonyPatch(typeof(Dialog_SelectXenogerm),  MethodType.Constructor, new Type[] {typeof(Pawn), typeof(Map), typeof(Xenogerm), typeof(Action<Xenogerm>)})]
	static class SelectXenogermPatch
	{
		static void Postfix(Pawn pawn, ref List<Xenogerm> ___xenogerms)
		{
			if (SanguophageOverhaul.Settings.NoCure && SanguophageOverhaul.XenotypeIsVampire(pawn.genes))
			{
				___xenogerms.RemoveAll(x => !SanguophageOverhaul.XenogermIsVampire(x.GeneSet));
			}
		}
	}
	[HarmonyPatch(typeof(Gene_Deathrest), "GetGizmos")]
	static class DeathrestGizmosPatch
	{
		static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> gizmos, Pawn ___pawn)
		{
			foreach (Gizmo gizmo in gizmos)
			{
				yield return gizmo;
			}
			if(SanguophageOverhaul.XenotypeIsVampire(___pawn.genes) && (___pawn.IsPrisonerOfColony || (___pawn.Downed && !___pawn.HomeFaction.IsPlayer)))
			{
				yield return new CannibalizeCommand(___pawn);
			}
		}
	}
}