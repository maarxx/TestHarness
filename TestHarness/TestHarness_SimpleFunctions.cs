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

    }
}
