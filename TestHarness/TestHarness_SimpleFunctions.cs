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
    public class TestHarness_SimpleFunctions
    {
        public static void printWorkTypesAndGivers()
        {
            foreach (WorkTypeDef workType in WorkTypeDefsUtility.WorkTypeDefsInPriorityOrder)
            {
                Log.Message(workType.defName + "(" + workType.naturalPriority + "), " + workType.workTags);
                foreach (WorkGiverDef workGiver in workType.workGiversByPriority)
                {
                    Log.Message("  " + workGiver.defName + "(" + workGiver.priorityInType + "), " + workGiver.workTags + ", " + workGiver.workType);
                }
            }
        }

        public static void printSortedSocialFightChances()
        {
            List<KeyValuePair<float, string>> socialFightChances = new List<KeyValuePair<float, string>>();
            List<Pawn> colonists = Find.CurrentMap.mapPawns.FreeColonists;
            foreach (Pawn p1 in colonists)
            {
                foreach (Pawn p2 in colonists)
                {
                    if (p1 != p2)
                    {
                        socialFightChances.Add(new KeyValuePair<float, string>(p1.interactions.SocialFightChance(InteractionDefOf.Insult, p2), p1.Name.ToStringShort + " -> " + p2.Name.ToStringShort));
                    }
                }
            }
            socialFightChances.Sort((x, y) => (x.Key.CompareTo(y.Key)));
            foreach (KeyValuePair<float, string> chance in socialFightChances)
            {
                Log.Message(chance.Key + " for " + chance.Value);
            }
        }

        public static void printTradersAndItemCategories()
        {
            List<TraderKindDef> traderKindDefs = DefDatabase<TraderKindDef>.AllDefsListForReading;
            HashSet<string> allAcceptedThings = new HashSet<string>();
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
                        allAcceptedThings.Add(acceptedThing);
                    }
                }
                maps.Add(traderKindDefName, theseAcceptedThings);
            }
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
            if      (sg is StockGenerator_BuySingleDef) { yield return (sg as StockGenerator_BuySingleDef).thingDef.defName; }
            else if (sg is StockGenerator_BuyTradeTag)  { yield return (sg as StockGenerator_BuyTradeTag).tag; }
            else if (sg is StockGenerator_Tag)          { yield return (sg as StockGenerator_Tag).tradeTag; }
            else if (sg is StockGenerator_MarketValue)  { yield return (sg as StockGenerator_MarketValue).tradeTag; }
            else if (sg is StockGenerator_Category)     { yield return (sg.GetType().GetField("categoryDef", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(sg) as ThingCategoryDef).defName; }
            else if (sg is StockGenerator_MultiDef)     { foreach (ThingDef td in sg.GetType().GetField("thingDefs", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(sg) as List<ThingDef>) { yield return td.defName; } }
            else if (sg is StockGenerator_SingleDef)    { yield return (sg.GetType().GetField("thingDef", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(sg) as ThingDef).defName; }
            else                                        { yield return sg.GetType().Name;  }
        }
    }
}
