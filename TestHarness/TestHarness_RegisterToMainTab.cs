using HarmonyLib;
using ModButtons;
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

            columns.Add(buttons);

            wasRegistered = true;
        }
    }
}
