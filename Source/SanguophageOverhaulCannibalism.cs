using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace SanguophageOverhaul
{
	public class CannibalizeCommand : Command_Action
	{
		static CachedTexture BloodFeederTexture = new CachedTexture("UI/Icons/Genes/Gene_Bloodfeeder");
		Pawn victim;
		public CannibalizeCommand(Pawn pawn)
		{
			defaultLabel = "Cannibalize".Translate();
			defaultDesc = "CannibalizeDesc".Translate();
			icon = BloodFeederTexture.Texture;
			victim = pawn;
			action = Action;
		}
		void Action()
		{
			List<FloatMenuOption> options = new List<FloatMenuOption>();
			List<Pawn> PlayerPawns = victim.MapHeld.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
			foreach(Pawn pawn in PlayerPawns)
			{
				if(pawn.genes != null && pawn != victim)
				{
					if(Sanguophage.XenotypeCanCannibalize(pawn.genes))
					{			
						options.Add(new FloatMenuOption(pawn.LabelShort, delegate
						{
							JobDriver_Cannibalize.GiveCannibalizeJob(pawn , victim);
						}, pawn, Color.white));
					}
				}
			}
			if(options.Any())
			{
				Find.WindowStack.Add(new FloatMenu(options));
			}
		}
	}
	public class JobDriver_Cannibalize : JobDriver
	{
		static readonly int CannibalizeDuration = 200;
		public Pawn Target => job.targetA.Thing as Pawn;
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(Target, job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
			yield return Toils_General.WaitWith(TargetIndex.A, CannibalizeDuration, useProgressBar:true);
			yield return Toils_General.Do(delegate
			{
				if(Target.HomeFaction != null && pawn.HomeFaction == Faction.OfPlayer)
				{
					Faction.OfPlayer.TryAffectGoodwillWith(Target.Faction, -100, canSendMessage: true, !Target.HomeFaction.temporary, HistoryEventDefOf.MemberKilled);
				}
				QuestUtility.SendQuestTargetSignals(Target.questTags, "Killed", Target.Named("SUBJECT"));
				Cannibalize(pawn, Target);
			});
		}
		public static void GiveCannibalizeJob(Pawn cannibal, Pawn target)
		{
			cannibal.jobs.TryTakeOrderedJob(JobMaker.MakeJob(SanguophageDefsOf.Cannibalize, target), JobTag.Misc);
		}
		public static void Cannibalize(Pawn cannibal, Pawn target)
		{
			int offset = ((Gene_Deathrest)target.genes.GetGene(SanguophageDefsOf.Deathrest)).DeathrestCapacity;
			((Gene_Deathrest)cannibal.genes.GetGene(SanguophageDefsOf.Deathrest)).OffsetCapacity(offset, sendNotification:true);
			target.genes.SetXenotype(XenotypeDefOf.Baseliner);
			DamageInfo damageInfo = new DamageInfo(DamageDefOf.ExecutionCut, 999f, 999f, -1f, null, target.health.hediffSet.GetBrain());
			damageInfo.SetIgnoreInstantKillProtection(ignore: true);
			damageInfo.SetAllowDamagePropagation(val: false);
			target.forceNoDeathNotification = true;
			target.TakeDamage(damageInfo);
			target.forceNoDeathNotification = false;
			ThoughtUtility.GiveThoughtsForPawnExecuted(target, cannibal, PawnExecutionKind.GenericBrutal);
		}
	}
}