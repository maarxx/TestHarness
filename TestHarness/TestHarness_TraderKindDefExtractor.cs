using RimWorld;
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
        public static void printTradersAndItemCategoriesV2()
        {
            // get all of the TraderKindDefs from the database for the rows
            List<TraderKindDef> traderKindDefs = DefDatabase<TraderKindDef>.AllDefsListForReading;
            // order them based upon pre-seeding
            traderKindDefs = traderKindDefs.OrderBy(
                tdk => Array.IndexOf(seededRowOrder, tdk.defName) >= 0 ? Array.IndexOf(seededRowOrder, tdk.defName) : int.MaxValue
            ).ToList();

            // iterate all of the TraderKindDefs and all of the StockGenerators for all the columns
            HashSet<string> setAllAcceptedThings = new HashSet<string>();
            foreach (TraderKindDef tkd in traderKindDefs)
            {
                foreach (StockGenerator sg in tkd.stockGenerators)
                {
                    List<string> strings = tryGetStrings(sg).ToList();
                    foreach (string acceptedThing in tryGetStrings(sg))
                    {
                        setAllAcceptedThings.Add(acceptedThing);
                    }
                }
            }
            // order them based upon pre-seeding
            List<string> allAcceptedThings = setAllAcceptedThings.OrderBy(
                str => Array.IndexOf(seededColumnOrder, str) >= 0 ? Array.IndexOf(seededColumnOrder, str) : int.MaxValue
            ).ToList();

            // seed the whole grid with null strings
            Dictionary<TraderKindDef, Dictionary<string, string>> table = new Dictionary<TraderKindDef, Dictionary<string, string>>();
            foreach (TraderKindDef row in traderKindDefs)
            {
                Dictionary<string, string> columnsAndValues = new Dictionary<string, string>();
                foreach (string column in allAcceptedThings)
                {
                    columnsAndValues.Add(column, "");
                }
                table.Add(row, columnsAndValues);
            }

            // iterate all of the TraderKindDefs again
            foreach (var row in table)
            {
                TraderKindDef tkd = row.Key;
                Dictionary<string, string> columnValues = row.Value;
                // iterate each of their StockGenerators again
                foreach (StockGenerator sg in tkd.stockGenerators)
                {
                    foreach (KeyValuePair<string,string> kvp in tryGetStringsV2(sg))
                    {
                        string oldVal = columnValues.TryGetValue(kvp.Key);
                        string newVal = kvp.Value;
                        columnValues.SetOrAdd(kvp.Key, smushValues(oldVal, newVal));
                    }
                }
            }

            string header = "TraderKindDef,";
            foreach (string s in allAcceptedThings)
            {
                header = header + s + ",";
            }
            Log.Message(header);

            foreach (var entry in table)
            {
                string row = entry.Key + ",";
                foreach (string s in allAcceptedThings)
                {
                    row += entry.Value.TryGetValue(s) + ",";
                }
                Log.Message(row);
            }
        }

        private static IEnumerable<KeyValuePair<string, string>> tryGetStringsV2(StockGenerator sg)
        {
            Type sgType = sg.GetType();
            //Log.Message(sgType.ToString());
            if (sgType == typeof(StockGenerator_BuySingleDef))
            {
                StockGenerator_BuySingleDef typed = (StockGenerator_BuySingleDef)sg;
                yield return pair(
                    typed.thingDef.defName,
                    "BUY"
                );
            }
            else if (sgType == typeof(StockGenerator_BuyTradeTag))
            {
                StockGenerator_BuyTradeTag typed = (StockGenerator_BuyTradeTag)sg;
                yield return pair(
                    typed.tag,
                    "BUY"
                );
            }
            else if (sgType == typeof(StockGenerator_Tag))
            {
                StockGenerator_Tag typed = (StockGenerator_Tag)sg;
                yield return pair(
                    typed.tradeTag,
                    ((IntRange)typed.GetType().GetField("thingDefCountRange", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(typed)).ToString()
                );
            }
            else if (sgType == typeof(StockGenerator_MarketValue))
            {
                StockGenerator_MarketValue typed = (StockGenerator_MarketValue)sg;
                yield return pair(
                    typed.tradeTag,
                    typed.countRange.ToString()
                );
            }
            else if (sgType == typeof(StockGenerator_Category))
            {
                StockGenerator_Category typed = (StockGenerator_Category)sg;
                yield return pair(
                    ((ThingCategoryDef)typed.GetType().GetField("categoryDef", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(typed)).defName,
                    ((IntRange)typed.GetType().GetField("thingDefCountRange", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(typed)).ToString()
                );
            }
            else if (sgType == typeof(StockGenerator_SingleDef))
            {
                StockGenerator_SingleDef typed = (StockGenerator_SingleDef)sg;
                yield return pair(
                    ((ThingDef)typed.GetType().GetField("thingDef", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(typed)).defName,
                    typed.countRange.ToString()
                );
            }
            else if (sgType == typeof(StockGenerator_Animals))
            {
                StockGenerator_Animals typed = (StockGenerator_Animals)sg;
                foreach (string s in typed.GetType().GetField("tradeTagsBuy", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(typed) as List<string>)
                {
                    yield return pair(
                        s,
                        "BUY"
                    );
                }
                foreach (string s in typed.GetType().GetField("tradeTagsSell", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(typed) as List<string>)
                {
                    yield return pair(
                        s,
                        ((IntRange)typed.GetType().GetField("kindCountRange", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(typed)).ToString()
                    );
                }
            }
            else if (sgType == typeof(StockGenerator_MultiDef))
            {
                StockGenerator_MultiDef typed = (StockGenerator_MultiDef)sg;
                foreach (ThingDef td in typed.GetType().GetField("thingDefs", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(typed) as List<ThingDef>)
                {
                    yield return pair(
                        td.defName,
                        "?"
                    );
                }
            }
            else
            {
                yield return pair(
                    sg.GetType().Name,
                    "*"
                );
            }
        }

        private static KeyValuePair<string,string> pair(string key, string value)
        {
            return new KeyValuePair<string, string>(key, value);
        }

        private static string smushValues(string oldVal, string newVal)
        {
            if (oldVal == "" || oldVal == "BUY")
            {
                return newVal;
            }
            else if (newVal == "BUY" && oldVal != "" && oldVal != "BUY")
            {
                return oldVal;
            }
            return oldVal + "+" + newVal;
        }






















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
                TraderKindDef again = DefDatabase<TraderKindDef>.GetNamed(traderKindDef);
                string newString = "_";
                foreach (string acceptedThing in allAcceptedThings)
                {
                    newString = "_";
                    if (acceptedThings.Contains(acceptedThing))
                    {
                        newString = acceptedThing;
                    }
                    else
                    {
                        ThingDef thingOrNull = DefDatabase<ThingDef>.GetNamedSilentFail(acceptedThing);
                        if (thingOrNull != null)
                        {
                            foreach (StockGenerator sg in again.stockGenerators)
                            {
                                if (sg.HandlesThingDef(thingOrNull))
                                {
                                    newString = "BUY";
                                    //break;
                                }
                            }
                        }
                    }
                    row = row + newString + ",";
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
            "AnimalFighter",
            "AnimalExotic",
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
