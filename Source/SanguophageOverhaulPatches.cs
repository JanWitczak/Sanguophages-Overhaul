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
	[HarmonyPatch(typeof(CompAbilityEffect_ReimplantXenogerm), "Valid")]
	static class ReimplantXenogermPatch
	{
		static void Postfix(ref bool __result, LocalTargetInfo target, bool throwMessages)
		{
			if (target.Pawn != null)
			{
				if(__result && Sanguophage.Settings.NoCure && Sanguophage.XenotypeIsVampire((target.Pawn).genes))
				{
					__result = false;
					if (throwMessages) Messages.Message("MessageCannotOverrideVampireXenotype".Translate(), target.Pawn, MessageTypeDefOf.RejectInput, historical: false);
				}
			}
		}
	}
	[HarmonyPatch(typeof(Recipe_ImplantXenogerm), "AvailableOnNow")]
	static class ImplantXenogermPatch
	{
		static void Postfix(ref bool __result, Thing thing)
		{
			if(__result && Sanguophage.Settings.NoCure && Sanguophage.XenotypeIsVampire(((Pawn)thing).genes))
			{
				List<Thing> xenogerms = ((Pawn)thing).Map.listerThings.ThingsOfDef(ThingDefOf.Xenogerm);
				if(!xenogerms.Exists(x => Sanguophage.XenogermIsVampire(((Xenogerm)x).GeneSet)))
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
			if (Sanguophage.Settings.NoCure && Sanguophage.XenotypeIsVampire(pawn.genes))
			{
				___xenogerms.RemoveAll(x => !Sanguophage.XenogermIsVampire(x.GeneSet));
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
			if(Sanguophage.Settings.Cannibalism && Sanguophage.XenotypeIsVampire(___pawn.genes) && !___pawn.health.hediffSet.HasHediff(HediffDefOf.XenogerminationComa) && (___pawn.IsPrisonerOfColony || (___pawn.Downed && !___pawn.HomeFaction.IsPlayer)))
			{
				yield return new CannibalizeCommand(___pawn);
			}
		}
	}
}