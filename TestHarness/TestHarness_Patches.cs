using HarmonyLib;
using ModButtons;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TestHarness
{
    [HarmonyPatch(typeof(Faction))]
    [HarmonyPatch("GetGoodwillGainForPrisonerRelease")]
    class Patch_Faction_GetGoodwillGainForPrisonerRelease
    {
        static void Postfix(Faction __instance, Pawn member, bool isHealthy, bool isInMentalState, int __result)
        {
            string noAidRelationsGainUntilTime = "";
            int noAidRelationsGainUntilTick = member.mindState.noAidRelationsGainUntilTick;
            if (noAidRelationsGainUntilTick > Find.TickManager.TicksGame)
            {
                int tickDiff = noAidRelationsGainUntilTick - Find.TickManager.TicksGame;
                noAidRelationsGainUntilTime = " (" + tickDiff + " [" + tickDiff.ToStringTicksToPeriod(shortForm: true) + "])";
            }
            Log.Message("GGwGFPR: " + __instance.Name + ", " + member.LabelShort + ", " + isHealthy + ", " + isInMentalState + ", " + noAidRelationsGainUntilTick + noAidRelationsGainUntilTime + ", " + __result);
        }
    }
}
