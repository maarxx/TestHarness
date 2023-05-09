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
                    foreach (KeyValuePair<string, string> kvp in tryGetStringsV2(sg))
                    {
                        setAllAcceptedThings.Add(kvp.Key);
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

            foreach (var row in table)
            {
                TraderKindDef tkd = row.Key;
                Dictionary<string, string> columnValues = row.Value;
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

            HashSet<KeyValuePair<string, string>> parents = new HashSet<KeyValuePair<string, string>>();
            foreach (string s in allAcceptedThings)
            {
                ThingDef td = DefDatabase<ThingDef>.GetNamedSilentFail(s);
                if (td != null)
                {
                    string[] tradeTags = td.tradeTags?.ToArray() ?? new string[0];
                    foreach (string tradeTag in tradeTags)
                    {
                        foreach (string other in allAcceptedThings)
                        {
                            if (s == other) { continue; }
                            if (tradeTag == other)
                            {
                                Log.Message(other + "->" + s);
                                parents.Add(pair(other, s));
                            }
                        }
                    }

                    foreach (ThingCategoryDef tcd in td.thingCategories)
                    {
                        string category = tcd.defName;
                        foreach (string other in allAcceptedThings)
                        {
                            if (s == other) { continue; }
                            if (category == other)
                            {
                                Log.Message(other + "->" + s);
                                parents.Add(pair(other, s));
                            }
                        }
                    }
                }
            }

            foreach (var row in table)
            {
                TraderKindDef tkd = row.Key;
                Dictionary<string, string> columnValues = row.Value;
                foreach (var link in parents)
                {
                    string parentKey = link.Key;
                    string parentValue = columnValues.TryGetValue(parentKey);
                    string childKey = link.Value;
                    string childValue = columnValues.TryGetValue(childKey);
                    columnValues.SetOrAdd(childKey, smushValues(childValue, parentValue, parentKey));
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
                    typed.countRange.ToString().Replace("~", "-")
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
                string value = typed.countRange.ToString().Replace("~", "-");
                if (value == "0-0") { value = "?"; }
                yield return pair(
                    ((ThingDef)typed.GetType().GetField("thingDef", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(typed)).defName,
                    value
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
            else if (sgType == typeof(StockGenerator_BuyExpensiveSimple))
            {
                StockGenerator_BuyExpensiveSimple typed = (StockGenerator_BuyExpensiveSimple)sg;
                yield return pair(
                    sg.GetType().Name,
                    "BUY"
                );
            }
            else
            {
                yield return pair(
                    sg.GetType().Name,
                    "?"
                );
            }
        }

        private static KeyValuePair<string,string> pair(string key, string value)
        {
            return new KeyValuePair<string, string>(key, value);
        }

        private static string smushValues(string oldVal, string newVal, string keyVal = null)
        {
            if (newVal == "")
            {
                return oldVal;
            }
            else if (oldVal.Contains("-") && !oldVal.Contains("~"))
            {
                if (newVal.Contains("-") || newVal.Contains("~"))
                {
                    return oldVal + " +*";
                }
                return oldVal;
            }
            else if (oldVal.Contains("~"))
            {
                if (newVal.Contains("-") && !newVal.Contains("~"))
                {
                    return newVal + " +*";
                }
                else if (newVal.Contains("~"))
                {
                    return oldVal + " +*";
                }
                return oldVal;
            }
            else if ((oldVal == "" || oldVal == "BUY") && newVal.Contains("~") && keyVal != null)
            {
                return keyVal;
            }
            else if (oldVal == "BUY")
            {
                if (newVal.Contains("~") || newVal.Contains("-"))
                {
                    return newVal;
                }
            }
            else if (oldVal == "?")
            {
                if (newVal.Contains("-") || newVal.Contains("~"))
                {
                    return oldVal + " +*";
                }
                return oldVal;
            }
            else if (oldVal == "")
            {
                return newVal;
            }
            Log.Message("POSSIBLE BUG SMUSHING VALUES: " + oldVal + " // " + newVal + " // " + keyVal);
            return "ERROR";
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
            "Neutroamine",
            "ResourcesRaw",
            "Gold",
            "Steel",
            "Plasteel",
            "Uranium",
            "WoodLog",
            "Chemfuel",
            "Textiles",
            "Cloth",
            "Dye",
            "Pemmican",
            "Chocolate",
            "FoodRaw",
            "Kibble",
            "Beer",
            "FoodMeals",
            "MealSurvivalPack",
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
            "StockGenerator_Slaves"
        };
    }
}
