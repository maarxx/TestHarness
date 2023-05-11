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

    // RimWorld.GenConstruct
    // public static bool CanPlaceBlueprintOver(BuildableDef newDef, ThingDef oldDef)
    [HarmonyPatch(typeof(GenConstruct))]
    [HarmonyPatch("CanPlaceBlueprintOver")]
    class Patch_GenConstruct_CanPlaceBlueprintOver
    {
        static bool Prefix(BuildableDef newDef, ThingDef oldDef, ref bool __result)
        {
            Log.Message("HELLO FROM Patch_GenConstruct_CanPlaceBlueprintOver");
            if (oldDef.Minifiable)
            {
                __result = true;
                return false;
            }
            return true;
        }
    }

    // RimWorld.GenConstruct
    // public static Job HandleBlockingThingJob(Thing constructible, Pawn worker, bool forced = false)
    [HarmonyPatch(typeof(GenConstruct))]
    [HarmonyPatch("HandleBlockingThingJob")]
    class Patch_GenConstruct_HandleBlockingThingJob
    {
        static bool Prefix(Thing constructible, Pawn worker, bool forced, ref Job __result)
        {
            Log.Message("HELLO FROM Patch_GenConstruct_HandleBlockingThingJob");
            Thing thing = GenConstruct.FirstBlockingThing(constructible, worker);

            // lifting this from original for performance reasons i think
            if (thing == null)
            {
                __result = null;
                return false;
            }

            if (thing.def.Minifiable && thing.def.category == ThingCategory.Building)
            {
                if (((Building)thing).DeconstructibleBy(worker.Faction))
                {
                    __result = JobMaker.MakeJob(JobDefOf.Uninstall, thing);
                    return false;
                }
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
