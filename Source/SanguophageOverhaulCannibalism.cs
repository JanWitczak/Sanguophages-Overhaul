using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace SanguophageOverhaul
{
	public class FloatMenuOptionProvider_Cannibalize : FloatMenuOptionProvider
	{
		protected override bool Drafted => true;
		protected override bool Undrafted => true;
		protected override bool Multiselect => false;
		protected override bool RequiresManipulation => true;
		protected override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
		{
			if (context.FirstSelectedPawn.genes == null || !Sanguophage.XenotypeIsVampire(context.FirstSelectedPawn.genes))
			{
				return null;
			}
			if (clickedPawn.genes == null || !Sanguophage.XenotypeIsVampire(clickedPawn.genes))
			{
				return null;
			}
			if (clickedPawn.Dead || !(clickedPawn.IsPrisonerInPrisonCell() || clickedPawn.health.Downed))
			{
				return null;
			}
			if (!context.FirstSelectedPawn.CanReach(clickedPawn, PathEndMode.ClosestTouch, Danger.Deadly))
			{
				return null; //TODO : Add a message about the pawn being unreachable.
			}
			return FloatMenuUtility.DecoratePrioritizedTask(
				new FloatMenuOption(
					"Cannibalize".Translate(),
					delegate
					{
						JobDriver_Cannibalize.GiveCannibalizeJob(context.FirstSelectedPawn, clickedPawn);
					}),
					context.FirstSelectedPawn, clickedPawn);
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
			this.FailOn(() => !Target.health.Downed || !Target.IsPrisonerInPrisonCell());
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
			yield return Toils_General.WaitWith(TargetIndex.A, CannibalizeDuration, useProgressBar: true);
			yield return Toils_General.Do(delegate
			{
				if (Target.HomeFaction != null && pawn.HomeFaction == Faction.OfPlayer)
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
			((Gene_Deathrest)cannibal.genes.GetGene(SanguophageDefsOf.Deathrest)).OffsetCapacity(offset, sendNotification: true);
			target.genes.SetXenotype(XenotypeDefOf.Baseliner);
			DamageInfo damageInfo = new DamageInfo(DamageDefOf.Bite, 999f, 999f, -1f, null, target.health.hediffSet.GetBrain());
			damageInfo.SetIgnoreInstantKillProtection(ignore: true);
			damageInfo.SetAllowDamagePropagation(val: false);
			target.TakeDamage(damageInfo);
			ThoughtUtility.GiveThoughtsForPawnExecuted(target, cannibal, PawnExecutionKind.GenericBrutal);
		}
	}
}