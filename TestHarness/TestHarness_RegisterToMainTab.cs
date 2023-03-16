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

    [HarmonyPatch(typeof(RelationsUtility))]
    [HarmonyPatch("RomanceOption")]
    class Patch_RelationsUtility_RomanceOption
    {

        static bool Prefix(Pawn initiator, Pawn romanceTarget, out FloatMenuOption option, out float chance, ref bool __result)
        {
            if (!AttractedToGender(initiator, romanceTarget.gender))
            {
                option = null;
                chance = 0f;
                __result = false;
                return false;
            }
            AcceptanceReport acceptanceReport = RomanceEligiblePair(initiator, romanceTarget, forOpinionExplanation: false);
            if (!acceptanceReport.Accepted && acceptanceReport.Reason.NullOrEmpty())
            {
                option = null;
                chance = 0f;
                __result = false;
                return false;
            }
            if (acceptanceReport.Accepted)
            {
                chance = InteractionWorker_RomanceAttempt.SuccessChance(initiator, romanceTarget, 1f);
                string label = string.Format("{0} ({1} {2})", romanceTarget.LabelShort, chance.ToStringPercent(), "chance".Translate());
                option = new FloatMenuOption(label, delegate
                {
                    GiveRomanceJobWithWarning(initiator, romanceTarget);
                }, MenuOptionPriority.Low);
                __result = true;
                return false;
            }
            chance = 0f;
            option = new FloatMenuOption(romanceTarget.LabelShort + " (" + acceptanceReport.Reason + ")", null);
            __result = false;
            return false;
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

            columns.Add(buttons);

            wasRegistered = true;
        }
    }
}
