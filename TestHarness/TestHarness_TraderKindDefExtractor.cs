﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TestHarness
{
    class TestHarness_TraderKindDefExtractor
    {
        public static void printTradersAndItemCategories()
        {
            List<TraderKindDef> traderKindDefs = DefDatabase<TraderKindDef>.AllDefsListForReading;
            traderKindDefs = traderKindDefs.OrderBy(tdk => Array.IndexOf(seededRowOrder, tdk.defName)).ToList();
            HashSet<string> setAllAcceptedThings = new HashSet<string>();
            Dictionary<string, HashSet<string>> maps = new Dictionary<string, HashSet<string>>();

            foreach (TraderKindDef tkd in traderKindDefs)
            {
                string traderKindDefName = tkd.defName;
                HashSet<string> theseAcceptedThings = new HashSet<string>();
                foreach (StockGenerator sg in tkd.stockGenerators)
                {
                    List<string> strings = tryGetStrings(sg).ToList();
                    foreach (string acceptedThing in tryGetStrings(sg))
                    {
                        theseAcceptedThings.Add(acceptedThing);
                        setAllAcceptedThings.Add(acceptedThing);
                    }
                }
                maps.Add(traderKindDefName, theseAcceptedThings);
            }

            List<string> allAcceptedThings = setAllAcceptedThings.OrderBy(str => Array.IndexOf(seededColumnOrder, str) >= 0 ? Array.IndexOf(seededColumnOrder, str) : int.MaxValue).ToList();

            string header = "_,";
            foreach (string s in allAcceptedThings)
            {
                header = header + s + ",";
            }
            Log.Message(header);

            foreach (KeyValuePair<string, HashSet<string>> kvp in maps)
            {
                string traderKindDef = kvp.Key;
                HashSet<string> acceptedThings = kvp.Value;
                string row = traderKindDef + ",";
                foreach (string acceptedThing in allAcceptedThings)
                {
                    row = row + (acceptedThings.Contains(acceptedThing) ? acceptedThing : "_") + ",";
                }
                Log.Message(row);
            }
        }

        private static IEnumerable<string> tryGetStrings(StockGenerator sg)
        {
            if (sg is StockGenerator_BuySingleDef) { yield return (sg as StockGenerator_BuySingleDef).thingDef.defName; }
            else if (sg is StockGenerator_BuyTradeTag) { yield return (sg as StockGenerator_BuyTradeTag).tag; }
            else if (sg is StockGenerator_Tag) { yield return (sg as StockGenerator_Tag).tradeTag; }
            else if (sg is StockGenerator_MarketValue) { yield return (sg as StockGenerator_MarketValue).tradeTag; }
            else if (sg is StockGenerator_Category) { yield return (sg.GetType().GetField("categoryDef", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(sg) as ThingCategoryDef).defName; }
            else if (sg is StockGenerator_SingleDef) { yield return (sg.GetType().GetField("thingDef", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(sg) as ThingDef).defName; }
            else if (sg is StockGenerator_Animals)
            {
                foreach (string s in sg.GetType().GetField("tradeTagsSell", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(sg) as List<string>)
                {
                    yield return s;
                }
                foreach (string s in sg.GetType().GetField("tradeTagsBuy", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(sg) as List<string>)
                {
                    yield return "Buy_" + s;
                }
            }
            else if (sg is StockGenerator_MultiDef)
            {
                foreach (ThingDef td in sg.GetType().GetField("thingDefs", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(sg) as List<ThingDef>)
                {
                    yield return td.defName;
                }
            }
            else
            {
                yield return sg.GetType().Name;
            }
        }

        private static string[] seededRowOrder = new string[] {
            "Orbital_Exotic",
            "Caravan_Outlander_Exotic",
            "Caravan_Neolithic_ShamanMerchant",
            "Base_Empire_Standard",
            "Base_Outlander_Standard",
            "Base_Neolithic_Standard",
            "Orbital_Empire",
            "Empire_Caravan_TraderGeneral",
            "Orbital_PirateMerchant",
            "Caravan_Outlander_PirateMerchant",
            "Caravan_Neolithic_Slaver",
            "Orbital_BulkGoods",
            "Caravan_Outlander_BulkGoods",
            "Caravan_Neolithic_BulkGoods",
            "Orbital_CombatSupplier",
            "Caravan_Outlander_CombatSupplier",
            "Caravan_Neolithic_WarMerchant",
            "Visitor_Outlander_Standard",
            "Visitor_Neolithic_Standard",
            "Empire_Caravan_TributeCollector"
        };

        private static string[] seededColumnOrder = new string[] {
            "StockGenerator_BuySlaves",
            "Silver",
            "StockGenerator_BuyExpensiveSimple",
            "Art",
            "MusicalInstrument",
            "Drugs",
            "Clothing",
            "BasicClothing",
            "BuildingsFurniture",
            "StockGenerator_Techprints",
            "AnimusStone",
            "Artifact",
            "Genepack",
            "StockGenerator_ReinforcedBarrels",
            "ExoticMisc",
            "ArchiteCapsule",
            "DeathrestCapacitySerum",
            "Telescope",
            "Television",
            "ExoticBuilding",
            "PoluxSeed",
            "ImplantEmpireCommon",
            "ImplantEmpireRoyal",
            "TechHediff",
            "PsylinkNeuroformer",
            "PsychicWeapon",
            "PsychicApparel",
            "NeurotrainersPsycast",
            "AnimalFarm",
            "AnimalPet",
            "AnimalUncommon",
            "Buy_AnimalUncommon",
            "AnimalFighter",
            "AnimalExotic",
            "Buy_AnimalExotic",
            "ComponentIndustrial",
            "ComponentSpacer",
            "Gold",
            "Steel",
            "Plasteel",
            "Neutroamine",
            "Uranium",
            "WoodLog",
            "Chemfuel",
            "Cloth",
            "Textiles",
            "ResourcesRaw",
            "Pemmican",
            "Chocolate",
            "FoodRaw",
            "Kibble",
            "Beer",
            "FoodMeals",
            "Medicine",
            "MedicineHerbal",
            "MedicineIndustrial",
            "MedicineUltratech",
            "WeaponRanged",
            "WeaponsMelee",
            "WeaponMelee",
            "Armor",
            "HiTechArmor",
            "MortarShell",
            "StockGenerator_Slaves",
            "Dye",
            "MealSurvivalPack"
        };
    }
}