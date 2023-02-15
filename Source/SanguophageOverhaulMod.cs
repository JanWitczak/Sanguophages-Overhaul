using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace SanguophageOverhaul
{
	public class SanguophageOverhaul : Mod
	{
		public static SanguophageOverhaulSettings Settings;
		public SanguophageOverhaul(ModContentPack content) : base(content)
		{
			Settings = GetSettings<SanguophageOverhaulSettings>();
		}
		public override void DoSettingsWindowContents(Rect inRect)
		{
			Listing_Standard settingsMenu = new Listing_Standard();
			settingsMenu.Begin(inRect);
			settingsMenu.CheckboxLabeled("NoCure".Translate(), ref Settings.NoCure);
			settingsMenu.GapLine();
			settingsMenu.CheckboxLabeled("DynamicUndeath".Translate(), ref Settings.DynamicUndeath);
			if(Settings.DynamicUndeath)
			{
				settingsMenu.CheckboxLabeled("OnlyBloodfeedersCanCannibalize".Translate(), ref Settings.OnlyBloodfeedersCanCannibalize);
			}
			settingsMenu.End();
		}
		public override string SettingsCategory()
		{
			return "Sanguophage: The Overhaul";
		}

		public static bool XenogermIsVampire(GeneSet genes)
		{
			if (Settings.DynamicUndeath)
			{
				if(genes.GenesListForReading.Contains(SanguophageOverhaulDefsOf.Deathless) && genes.GenesListForReading.Contains(SanguophageOverhaulDefsOf.Deathrest)) return true;
				else return false;
			}
			else
			{
				return false;
			}
		}
		public static bool XenotypeIsVampire(Pawn_GeneTracker genes)
		{
			if(Settings.DynamicUndeath)
			{
				if(genes.HasXenogene(SanguophageOverhaulDefsOf.Deathless) && genes.HasXenogene(SanguophageOverhaulDefsOf.Deathrest)) return true;
				else return false;
			}
			else
			{
				if(genes.Xenotype == XenotypeDefOf.Sanguophage) return true;
				else return false;
			}
		}
		public static bool XenotypeCanCannibalize(Pawn_GeneTracker genes)
		{
			if (Settings.OnlyBloodfeedersCanCannibalize)
			{
				if(XenotypeIsVampire(genes) && genes.HasXenogene(SanguophageOverhaulDefsOf.Bloodfeeder)) return true;
				else return false;
			}
			else return XenotypeIsVampire(genes);
		}
		public static void GiveCannibalizeJob(Pawn cannibal, Pawn target)
		{
			cannibal.jobs.TryTakeOrderedJob(JobMaker.MakeJob(SanguophageOverhaulDefsOf.Cannibalize, target), JobTag.Misc);
		}
		public static void Cannibalize(Pawn cannibal, Pawn target)
		{
			int offset = ((Gene_Deathrest)target.genes.GetGene(SanguophageOverhaulDefsOf.Deathrest)).DeathrestCapacity;
			((Gene_Deathrest)cannibal.genes.GetGene(SanguophageOverhaulDefsOf.Deathrest)).OffsetCapacity(offset, sendNotification:true);
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

	public class SanguophageOverhaulSettings : ModSettings
	{
		public bool NoCure = true;
		public bool DynamicUndeath = false;
		public bool OnlyBloodfeedersCanCannibalize = true;
		public override void ExposeData()
		{
			Scribe_Values.Look(ref NoCure, "NoCure", defaultValue:true);
			Scribe_Values.Look(ref DynamicUndeath, "DynamicUndeath", defaultValue:false);
			Scribe_Values.Look(ref OnlyBloodfeedersCanCannibalize, "OnlyBloodfeedersCanCannibalize", defaultValue:true);
		}
	}

	[DefOf]
	public static class SanguophageOverhaulDefsOf
	{
		public static GeneDef Deathless;
		public static GeneDef Deathrest;
		public static GeneDef Bloodfeeder;
		public static JobDef Cannibalize;
		static SanguophageOverhaulDefsOf()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(SanguophageOverhaulDefsOf));
		}
	}
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
					if(SanguophageOverhaul.XenotypeCanCannibalize(pawn.genes))
					{			
						options.Add(new FloatMenuOption(pawn.LabelShort, delegate
						{
							SanguophageOverhaul.GiveCannibalizeJob(pawn , victim);
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
				SanguophageOverhaul.Cannibalize(pawn, Target);
			});
		}
	}
}