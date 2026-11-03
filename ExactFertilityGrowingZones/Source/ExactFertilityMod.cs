using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ExactFertilityGrowingZones
{
    public sealed class ExactFertilityMod : Mod
    {
        public ExactFertilityMod(ModContentPack content) : base(content)
        {
            new Harmony("kotalow.exactfertilitygrowingzones").PatchAll();
        }
    }

    // UI preference only: deliberately not stored in a map, zone, or save file.
    internal static class FilterState
    {
        internal static bool Enabled;
        internal static float Target = 1f;
    }

    [HarmonyPatch(typeof(Designator_ZoneAdd_Growing), nameof(Designator_ZoneAdd_Growing.CanDesignateCell))]
    internal static class GrowingCellPatch
    {
        private static readonly FertilityMessageCache RejectionMessage = new FertilityMessageCache(
            target => "EFGZ_WrongFertility".Translate(FertilityValues.Label(target)).ToString());

        private static void Postfix(Designator_ZoneAdd_Growing __instance, IntVec3 c,
            ref AcceptanceReport __result)
        {
            // Never turn a vanilla/other-mod rejection into an acceptance.
            if (!__result.Accepted || !FilterState.Enabled)
                return;

            Map map = __instance.Map;
            if (map != null && !FertilityValues.Matches(map.fertilityGrid.FertilityAt(c), FilterState.Target))
                __result = new AcceptanceReport(RejectionMessage.Get(FilterState.Target, LanguageDatabase.activeLanguage));
        }
    }

    [HarmonyPatch(typeof(Designator), nameof(Designator.DoExtraGuiControls))]
    internal static class GrowingControlsPatch
    {
        // Positional arguments avoid relying on parameter names in different game builds.
        private static void Postfix(Designator __instance, float __0, float __1)
        {
            if (!(__instance is Designator_ZoneAdd_Growing) || __instance.Map == null)
                return;

            // Vanilla uses the bottom 90px for its draw-style controls. Stay above them.
            var rect = new Rect(__0, Math.Max(0f, __1 - 184f), 260f, 90f);
            Map map = __instance.Map;
            Find.WindowStack.ImmediateWindow(1894712301, rect, WindowLayer.GameUI,
                () => DrawContents(map));
        }

        private static void DrawContents(Map map)
        {
            GameFont previousFont = Text.Font;
            TextAnchor previousAnchor = Text.Anchor;
            try
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                var checkboxRect = new Rect(10f, 7f, 240f, 28f);
                Widgets.CheckboxLabeled(checkboxRect, "EFGZ_FilterEnabled".Translate(), ref FilterState.Enabled);
                TooltipHandler.TipRegion(checkboxRect, "EFGZ_FilterTooltip".Translate());

                var buttonRect = new Rect(10f, 43f, 240f, 32f);
                if (Widgets.ButtonText(buttonRect,
                    "EFGZ_ChooseFertility".Translate(FertilityValues.Label(FilterState.Target))))
                    OpenChoices(map);
            }
            finally
            {
                Text.Font = previousFont;
                Text.Anchor = previousAnchor;
            }
        }

        private static void OpenChoices(Map map)
        {
            List<float> values = FertilityValues.SortedDistinctPositive(RevealedFertilities(map));
            var options = new List<FloatMenuOption>();
            foreach (float value in values)
            {
                float selectedValue = value;
                options.Add(new FloatMenuOption(FertilityValues.Label(value), () =>
                {
                    FilterState.Target = selectedValue;
                    FilterState.Enabled = true;
                }));
            }

            if (options.Count == 0)
                options.Add(new FloatMenuOption("EFGZ_NoFertility".Translate(), null));
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static IEnumerable<float> RevealedFertilities(Map map)
        {
            // Scan on menu-open only, so exploration and terrain changes are reflected immediately.
            int cellCount = map.Size.x * map.Size.z;
            for (int index = 0; index < cellCount; index++)
            {
                if (map.fogGrid.IsFogged(index))
                    continue;
                yield return map.fertilityGrid.FertilityAt(map.cellIndices.IndexToCell(index));
            }
        }
    }
}
