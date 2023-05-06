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
                    string accepted = tryGetString(sg);
                    theseAcceptedThings.Add(accepted);
                    allAcceptedThings.Add(accepted);
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
                foreach (string s in allAcceptedThings)
                {
                    row = row + (acceptedThings.Contains(s) ? "x" : "_") + ",";
                }
                Log.Message(row);
            }
        }

        private static string tryGetString(StockGenerator sg)
        {
            Log.Message(sg.ToString());
            if      (sg is StockGenerator_BuySingleDef) { return (sg as StockGenerator_BuySingleDef).thingDef.defName; }
            else if (sg is StockGenerator_BuyTradeTag)  { return (sg as StockGenerator_BuyTradeTag).tag; }
            else if (sg is StockGenerator_Category)     { return (sg.GetType().GetField("categoryDef", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(sg) as ThingCategoryDef).defName; }
            else if (sg is StockGenerator_MultiDef)     { return String.Join(",", (sg.GetType().GetField("thingDefs", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(sg) as List<ThingDef>)); }
            else if (sg is StockGenerator_SingleDef)    { return (sg.GetType().GetField("thingDef", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(sg) as ThingDef).defName; }
            else if (sg is StockGenerator_Tag)          { return (sg as StockGenerator_Tag).tradeTag; }
            else                                        { return sg.GetType().Name;  }
        }
    }
}
