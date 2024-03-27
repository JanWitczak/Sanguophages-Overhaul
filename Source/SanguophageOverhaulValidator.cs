using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace SanguophageOverhaul
{
	public class GenesValidator : GameComponent
	{
		Game game => Current.Game;
		public GenesValidator(Game game)
		{
		}
		public override void LoadedGame()
		{
			foreach (Map map in game.Maps)
			{
				foreach (Pawn pawn in map.mapPawns.AllPawns)
				{
					ValidatePawnGenes(pawn);
				}
			}
			foreach (Pawn pawn in game.World.worldPawns.AllPawnsAlive)
			{
				ValidatePawnGenes(pawn);
			}
		}
		private void ValidatePawnGenes(Pawn pawn)
		{
			if (pawn.genes != null && pawn.genes.Xenotype == SanguophageDefsOf.Sanguophage)
			{
				if (Sanguophage.Settings.FertileSanguophages && pawn.genes.Xenogenes.Find(x => x.def == SanguophageDefsOf.Sterile) != null)
				{
					pawn.genes.Xenogenes.RemoveAll(x => x.def == SanguophageDefsOf.Sterile);
				}
				else if (!Sanguophage.Settings.FertileSanguophages && pawn.genes.Xenogenes.Find(x => x.def == SanguophageDefsOf.Sterile) == null)
				{
					pawn.genes.AddGene(SanguophageDefsOf.Sterile, true);
				}
				if (Sanguophage.Settings.ValidateGenes)
				{
					if (!(new HashSet<string>(pawn.genes.Xenogenes.ConvertAll(x => x.def.defName)).SetEquals(new HashSet<string>(SanguophageDefsOf.Sanguophage.AllGenes.ConvertAll(x => x.defName)))))
					{
						int deathrestCapacity = 1;
						if (pawn.genes.Xenogenes.Find(x => x.def == SanguophageDefsOf.Deathrest) != null)
						{
							deathrestCapacity = ((Gene_Deathrest)pawn.genes.Xenogenes.Find(x => x.def == SanguophageDefsOf.Deathrest)).DeathrestCapacity;
						}
						Log.Warning("Resetting xenotype of " + pawn.Name.ToString());
						pawn.genes.SetXenotype(SanguophageDefsOf.Sanguophage);
						if (deathrestCapacity > 1) ((Gene_Deathrest)pawn.genes.Xenogenes.Find(x => x.def == SanguophageDefsOf.Deathrest)).OffsetCapacity(deathrestCapacity - 1, false);
					}
				}
			}
		}
	}
}