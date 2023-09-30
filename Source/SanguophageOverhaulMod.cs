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
			settingsMenu.CheckboxLabeled("Cannibalism".Translate(), ref Settings.Cannibalism);
			settingsMenu.CheckboxLabeled("DynamicUndeath".Translate(), ref Settings.DynamicUndeath);
			if(Settings.DynamicUndeath && Settings.Cannibalism)
			{
				settingsMenu.CheckboxLabeled("OnlyBloodfeedersCanCannibalize".Translate(), ref Settings.OnlyBloodfeedersCanCannibalize);
			}
			settingsMenu.GapLine();
			if(Current.Game == null)
			{
				settingsMenu.CheckboxLabeled("FertileSanguophages".Translate(), ref Settings.FertileSanguophages);
				settingsMenu.CheckboxLabeled("ValidateGenes".Translate(), ref Settings.ValidateGenes);		
			}
			else
			{
				settingsMenu.Label("FertileSanguophagesNotInGame".Translate());
				settingsMenu.Label("ValidateGenesNotInGame".Translate());
			}
			settingsMenu.End();
			if(toggleCheck != Settings.FertileSanguophages)
			{
				SanguophageSterilityPatcher.PatchSterility();
				toggleCheck = Settings.FertileSanguophages;
			}
		}
		public override string SettingsCategory()
		{
			return "Sanguophage: The Overhaul";
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
		public bool ValidateGenes = true;
		public bool FertileSanguophages = false;
		public bool Cannibalism = true;

		public bool DynamicUndeath = false;
		public bool OnlyBloodfeedersCanCannibalize = true;
		public override void ExposeData()
		{
			Scribe_Values.Look(ref NoCure, "NoCure", defaultValue:true);
			Scribe_Values.Look(ref ValidateGenes, "ValidateGenes", defaultValue:true);
			Scribe_Values.Look(ref FertileSanguophages, "FertileSanguophages", defaultValue:false);
			Scribe_Values.Look(ref DynamicUndeath, "DynamicUndeath", defaultValue:false);
			Scribe_Values.Look(ref OnlyBloodfeedersCanCannibalize, "OnlyBloodfeedersCanCannibalize", defaultValue:true);
			Scribe_Values.Look(ref Cannibalism, "Cannibalism", defaultValue:true);
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
	public static class SanguophageSterilityPatcher
	{
		static SanguophageSterilityPatcher()
		{
			PatchSterility();
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
		}
	}
}