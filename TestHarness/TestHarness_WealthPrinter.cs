using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TestHarness
{
    class TestHarness_WealthPrinter
    {
        private float wealthItems;

        private float wealthBuildings;

        private float wealthPawns;

        private float wealthFloorsOnly;

        private int totalHealth;

        private float lastCountTick = -99999f;

        private static float[] cachedTerrainMarketValue;

        private const int MinCountInterval = 5000;

        private List<Thing> tmpThings = new List<Thing>();

        private Dictionary<String, float> wealthTally = new Dictionary<string, float>();

        public void printWealth()
        {
            ResetStaticData();
            ForceRecount();
            var myList = wealthTally.ToList();

            myList.Sort((pair1, pair2) => -1 * pair1.Value.CompareTo(pair2.Value));
            foreach (KeyValuePair<string, float> e in myList)
            {
                if (e.Value < 300) { continue; }
                Log.Message(e.Key + ": " + e.Value);
            }
        }

        public int HealthTotal
        {
            get
            {
                RecountIfNeeded();
                return totalHealth;
            }
        }

        public float WealthTotal
        {
            get
            {
                RecountIfNeeded();
                return wealthItems + wealthBuildings + wealthPawns;
            }
        }

        public float WealthItems
        {
            get
            {
                RecountIfNeeded();
                return wealthItems;
            }
        }

        public float WealthBuildings
        {
            get
            {
                RecountIfNeeded();
                return wealthBuildings;
            }
        }

        public float WealthFloorsOnly
        {
            get
            {
                RecountIfNeeded();
                return wealthFloorsOnly;
            }
        }

        public float WealthPawns
        {
            get
            {
                RecountIfNeeded();
                return wealthPawns;
            }
        }

        public static void ResetStaticData()
        {
            int num = -1;
            List<TerrainDef> allDefsListForReading = DefDatabase<TerrainDef>.AllDefsListForReading;
            for (int i = 0; i < allDefsListForReading.Count; i++)
            {
                num = Mathf.Max(num, allDefsListForReading[i].index);
            }
            cachedTerrainMarketValue = new float[num + 1];
            for (int j = 0; j < allDefsListForReading.Count; j++)
            {
                cachedTerrainMarketValue[allDefsListForReading[j].index] = allDefsListForReading[j].GetStatValueAbstract(StatDefOf.MarketValue);
            }
        }

        private void RecountIfNeeded()
        {
            if ((float)Find.TickManager.TicksGame - lastCountTick > 5000f)
            {
                ForceRecount();
            }
        }

        public void ForceRecount(bool allowDuringInit = false)
        {
            if (!allowDuringInit && Current.ProgramState != ProgramState.Playing)
            {
                Log.Error("WealthWatcher recount in game mode " + Current.ProgramState);
                return;
            }
            wealthTally = new Dictionary<string, float>();
            wealthItems = CalculateWealthItems();
            wealthBuildings = 0f;
            wealthPawns = 0f;
            wealthFloorsOnly = 0f;
            totalHealth = 0;
            List<Thing> list = Find.CurrentMap.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
            for (int i = 0; i < list.Count; i++)
            {
                Thing thing = list[i];
                if (thing.Faction == Faction.OfPlayer)
                {
                    wealthBuildings += thing.GetStatValue(StatDefOf.MarketValueIgnoreHp);
                    wealthTally[thing.def.defName] = wealthTally.TryGetValue(thing.def.defName) + thing.GetStatValue(StatDefOf.MarketValueIgnoreHp);
                    totalHealth += thing.HitPoints;
                }
            }
            wealthFloorsOnly = CalculateWealthFloors();
            wealthBuildings += wealthFloorsOnly;
            foreach (Pawn item in Find.CurrentMap.mapPawns.PawnsInFaction(Faction.OfPlayer))
            {
                if (!item.IsQuestLodger())
                {
                    wealthPawns += item.MarketValue;
                    wealthTally[item.def.defName] = wealthTally.TryGetValue(item.def.defName) + item.MarketValue;
                    if (item.IsFreeColonist)
                    {
                        totalHealth += Mathf.RoundToInt(item.health.summaryHealth.SummaryHealthPercent * 100f);
                        wealthTally[item.def.defName] = wealthTally.TryGetValue(item.def.defName) + Mathf.RoundToInt(item.health.summaryHealth.SummaryHealthPercent * 100f);
                    }
                }
            }
            lastCountTick = Find.TickManager.TicksGame;
        }

        public static float GetEquipmentApparelAndInventoryWealth(Pawn p)
        {
            float num = 0f;
            if (p.equipment != null)
            {
                List<ThingWithComps> allEquipmentListForReading = p.equipment.AllEquipmentListForReading;
                for (int i = 0; i < allEquipmentListForReading.Count; i++)
                {
                    Thing t = allEquipmentListForReading[i];
                    num += t.MarketValue * (float)t.stackCount;
                    //wealthTally[t.def.defName] += t.MarketValue * (float)t.stackCount;
                }
            }
            if (p.apparel != null)
            {
                List<Apparel> wornApparel = p.apparel.WornApparel;
                for (int j = 0; j < wornApparel.Count; j++)
                {
                    num += wornApparel[j].MarketValue * (float)wornApparel[j].stackCount;
                }
            }
            if (p.inventory != null)
            {
                ThingOwner<Thing> innerContainer = p.inventory.innerContainer;
                for (int k = 0; k < innerContainer.Count; k++)
                {
                    num += innerContainer[k].MarketValue * (float)innerContainer[k].stackCount;
                }
            }
            return num;
        }

        private float CalculateWealthItems()
        {
            tmpThings.Clear();
            ThingOwnerUtility.GetAllThingsRecursively(Find.CurrentMap, ThingRequest.ForGroup(ThingRequestGroup.HaulableEver), tmpThings, allowUnreal: false, delegate (IThingHolder x)
            {
                if (x is PassingShip || x is MapComponent)
                {
                    return false;
                }
                Pawn pawn = x as Pawn;
                if (pawn != null && pawn.Faction != Faction.OfPlayer)
                {
                    return false;
                }
                if (pawn != null && pawn.IsQuestLodger())
                {
                    return false;
                }
                return true;
            });
            float num = 0f;
            for (int i = 0; i < tmpThings.Count; i++)
            {
                if (tmpThings[i].SpawnedOrAnyParentSpawned && !tmpThings[i].PositionHeld.Fogged(Find.CurrentMap))
                {
                    Thing t = tmpThings[i];
                    num += t.MarketValue * (float)t.stackCount;
                    wealthTally[t.def.defName] = wealthTally.TryGetValue(t.def.defName) + (t.MarketValue * (float)t.stackCount);
                }
            }
            tmpThings.Clear();
            return num;
        }

        private float CalculateWealthFloors()
        {
            TerrainDef[] topGrid = Find.CurrentMap.terrainGrid.topGrid;
            bool[] fogGrid = Find.CurrentMap.fogGrid.fogGrid;
            IntVec3 size = Find.CurrentMap.Size;
            float num = 0f;
            int i = 0;
            for (int num2 = size.x * size.z; i < num2; i++)
            {
                if (!fogGrid[i])
                {
                    num += cachedTerrainMarketValue[topGrid[i].index];
                    wealthTally["floors"] = wealthTally.TryGetValue("floors") + cachedTerrainMarketValue[topGrid[i].index];
                }
            }
            return num;
        }

    }
}
