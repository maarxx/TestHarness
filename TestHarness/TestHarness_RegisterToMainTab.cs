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
using Verse.AI;

namespace TestHarness
{
    [StaticConstructorOnStartup]
    class Main
    {
        static Main()
        {
            var harmony = new Harmony("com.github.harmony.rimworld.maarx.testharness");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(MainTabWindow_ModButtons))]
    [HarmonyPatch("DoWindowContents")]
    class Patch_MainTabWindow_ModButtons_DoWindowContents
    {
        static void Prefix(MainTabWindow_ModButtons __instance, ref Rect canvas)
        {
            TestHarness_RegisterToMainTab.ensureMainTabRegistered();
        }
    }
    // RimWorld.WorkGiver_DoBill
    //public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
    [HarmonyPatch(typeof(WorkGiver_DoBill))]
    [HarmonyPatch("JobOnThing")]
    class Patch_WorkGiver_DoBill_JobOnThing
    {
        static bool Prefix(WorkGiver_DoBill __instance, Pawn pawn, Thing thing, bool forced, Job __result)
        {
            CompAssignableToPawn catp = thing.TryGetComp<CompAssignableToPawn>();
            if (catp != null)
            {
                List<Pawn> assignedPawns = catp.AssignedPawnsForReading;
                if (assignedPawns.Count == 0)
                {
                    return true;
                }
                else if (assignedPawns.Contains(pawn))
                {
                    return true;
                }
                __result = null;
                return false;
            }
            return true;
        }
    }

    class TestHarness_RegisterToMainTab
    {

        public static bool wasRegistered = false;

        public static void ensureMainTabRegistered()
        {
            if (wasRegistered) { return; }

            Log.Message("Hello from TestHarness_RegisterToMainTab ensureMainTabRegistered");

            List<List<ModButton_Text>> columns = MainTabWindow_ModButtons.columns;

            List<ModButton_Text> buttons = new List<ModButton_Text>();

            buttons.Add(new ModButton_Text(
                delegate
                {
                    return "Debug Print:" + Environment.NewLine + "WorkTypes and WorkGivers";
                },
                delegate {
                    TestHarness_SimpleFunctions.printWorkTypesAndGivers();
                }
            ));
            buttons.Add(new ModButton_Text(
                delegate
                {
                    return "Debug Print:" + Environment.NewLine + "Current Colony Wealth";
                },
                delegate {
                    new TestHarness_WealthPrinter().printWealth();
                }
            ));
            buttons.Add(new ModButton_Text(
                delegate
                {
                    return "Debug Print:" + Environment.NewLine + "Social Fight Chances";
                },
                delegate {
                    TestHarness_SimpleFunctions.printSortedSocialFightChances();
                }
            ));
            buttons.Add(new ModButton_Text(
                delegate
                {
                    return "Debug Print:" + Environment.NewLine + "Trader And Item Categories";
                },
                delegate {
                    TestHarness_TraderKindDefExtractor.printTradersAndItemCategoriesV2();
                }
            ));

            columns.Add(buttons);

            wasRegistered = true;
        }
    }
}
