using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace SanguophageOverhaul
{
	public class Sanguophage : Mod
	{
		public static SanguophageSettings Settings;
		private bool toggleCheck;
		public Sanguophage(ModContentPack content) : base(content)
		{
			Settings = GetSettings<SanguophageSettings>();
			toggleCheck = Settings.FertileSanguophages;
		}
		public override void DoSettingsWindowContents(Rect inRect)
		{
			Listing_Standard settingsMenu = new Listing_Standard();
			settingsMenu.Begin(inRect);
			settingsMenu.CheckboxLabeled("NoCure".Translate(), ref Settings.NoCure);
			if(Current.Game == null)
			{
				settingsMenu.CheckboxLabeled("FertileSanguophages".Translate(), ref Settings.FertileSanguophages);			
			}
			else
			{
				settingsMenu.Label("FertileSanguophagesNotInGame".Translate());
			}
			settingsMenu.GapLine();
			settingsMenu.CheckboxLabeled("DynamicUndeath".Translate(), ref Settings.DynamicUndeath);
			if(Settings.DynamicUndeath)
			{
				settingsMenu.CheckboxLabeled("OnlyBloodfeedersCanCannibalize".Translate(), ref Settings.OnlyBloodfeedersCanCannibalize);
			}
			settingsMenu.End();
			if(toggleCheck != Settings.FertileSanguophages)
			{
				PatchSterility();
				toggleCheck = Settings.FertileSanguophages;
			}
		}
		public override string SettingsCategory()
		{
			return "Sanguophage: The Overhaul";
		}
		public static void PatchSterility()
		{
			if(Sanguophage.Settings.FertileSanguophages && SanguophageDefsOf.Sanguophage.AllGenes.Contains(SanguophageDefsOf.Sterile))
			{
				SanguophageDefsOf.Sanguophage.AllGenes.Remove(SanguophageDefsOf.Sterile);
			}
			else if(!Sanguophage.Settings.FertileSanguophages && !SanguophageDefsOf.Sanguophage.AllGenes.Contains(SanguophageDefsOf.Sterile))
			{
				SanguophageDefsOf.Sanguophage.AllGenes.Add(SanguophageDefsOf.Sterile);
			}
			GeneUtility.SortGeneDefs(SanguophageDefsOf.Sanguophage.AllGenes);
		}
		public static bool XenogermIsVampire(GeneSet genes)
		{
			if (Settings.DynamicUndeath)
			{
				if(genes.GenesListForReading.Contains(SanguophageDefsOf.Deathless) && genes.GenesListForReading.Contains(SanguophageDefsOf.Deathrest)) return true;
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
				if(genes.HasXenogene(SanguophageDefsOf.Deathless) && genes.HasXenogene(SanguophageDefsOf.Deathrest)) return true;
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
				if(XenotypeIsVampire(genes) && genes.HasXenogene(SanguophageDefsOf.Bloodfeeder)) return true;
				else return false;
			}
			else return XenotypeIsVampire(genes);
		}
	}

	public class SanguophageSettings : ModSettings
	{
		public bool NoCure = true;
		public bool FertileSanguophages = false;
		public bool DynamicUndeath = false;
		public bool OnlyBloodfeedersCanCannibalize = true;
		public override void ExposeData()
		{
			Scribe_Values.Look(ref NoCure, "NoCure", defaultValue:true);
			Scribe_Values.Look(ref FertileSanguophages, "FertileSanguophages", defaultValue:false);
			Scribe_Values.Look(ref DynamicUndeath, "DynamicUndeath", defaultValue:false);
			Scribe_Values.Look(ref OnlyBloodfeedersCanCannibalize, "OnlyBloodfeedersCanCannibalize", defaultValue:true);
		}
	}

	[DefOf]
	public static class SanguophageDefsOf
	{
		public static XenotypeDef Sanguophage;
		public static GeneDef Deathless;
		public static GeneDef Deathrest;
		public static GeneDef Bloodfeeder;
		public static GeneDef Sterile;
		public static JobDef Cannibalize;
		static SanguophageDefsOf()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(SanguophageDefsOf));
		}
	}

	[StaticConstructorOnStartup]
	public static class SanguophageFertilityPatcher
	{
		static SanguophageFertilityPatcher()
		{
			Sanguophage.PatchSterility();
		}
	}
}