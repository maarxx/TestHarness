using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
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
            for (int i = 0; i < colonists.Count; i++)
            {
                for (int j = i+1; j < colonists.Count; j++)
                {
                    Pawn p1 = colonists[i];
                    Pawn p2 = colonists[j];
                    socialFightChances.Add(new KeyValuePair<float, string>(p1.interactions.SocialFightChance(InteractionDefOf.Insult, p2), p1.Name.ToStringShort + "<->" + p2.Name.ToStringShort));
                }
            }
            //List<KeyValuePair<float, string>> sorted = socialFightChances.ToList();
            socialFightChances.Sort((x, y) => (y.Key.CompareTo(x.Key)));
            foreach (KeyValuePair<float, string> chance in socialFightChances)
            {
                Log.Message(chance.Key + " for " + chance.Value);
            }
        }
    }
}
