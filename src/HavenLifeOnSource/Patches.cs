using System.Collections.Generic;
using HarmonyLib;

namespace HavenLifeOnSource
{
    [HarmonyPatch(typeof(LoadMainMenuFsmAction), "Execute")]
    internal static class LoadMainMenuPatch
    {
        private static bool Prefix(LoadMainMenuFsmAction __instance, ref bool __result)
        {
            if (!Plugin.Enabled.Value || __instance.apiaryEnding.Value || !FunctionCondition.IsInEpilogue())
                return true;

            var igs = UnitySingleton<IGSManager>.Instance.CurrentPlayingIGS?.igs;
            Plugin.Log.LogInfo($"Skipping main menu after epilogue ending (IGS: {igs?.Name}, FSM: {__instance.Fsm?.Name})");

            // Returning true sends "Done": the FSM moves to its Trigger state, whose
            // EndEventHandler calls IGSManager.OnFsmEnd and ends the scene normally.
            Plugin.PendingContinue = true;
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(IGSManager), "OnIGSEnd")]
    internal static class IGSEndPatch
    {
        private static void Postfix(IGSManager __instance)
        {
            if (!Plugin.PendingContinue || __instance.IsPlayingIGS)
                return;
            Plugin.PendingContinue = false;
            Plugin.Instance.ContinueAfterEnding();
        }
    }

    // Every nest activity has an "Epilogue [BackupChat]/[Nothing]" content gated by IsInEpilogue that
    // replaces the real activity, and the home island's bridges have an "Epilogue-CantLeaveWhileFlowerTrailing"
    // lock. Once the ending has played, report "not in epilogue" to those only, so the normal contents are
    // picked while other epilogue effects (unlocks, plants) stay.
    [HarmonyPatch(typeof(FunctionCondition), "IsValidInternal")]
    internal static class EpilogueLockPatch
    {
        private static bool Prefix(FunctionCondition __instance, UnityEngine.Object ___context, ref bool __result)
        {
            if (__instance.Type != FunctionCondition.FunctionType.IsInEpilogue || !Plugin.Enabled.Value)
                return true;

            bool lift;
            if (___context is ActivityParams)
                lift = Plugin.UnlockNestActivities.Value && Plugin.EndingSeen();
            else if (___context is ActionParams action && action.name.Contains("BridgeLock"))
                lift = Plugin.EndingSeen() && Plugin.BridgesOpen();
            else
                return true;

            if (!lift)
                return true;
            __result = false;
            return false;
        }
    }

    // Yu hesitance checkpoint: runs before any post-ending bridge crossing when enabled.
    [HarmonyPatch(typeof(BridgeGPE), "OnBridgeTrigger")]
    internal static class BridgeCrossPatch
    {
        private static bool Prefix(BridgeGPE __instance)
        {
            if (!Plugin.YuBridgeHesitance.Value || !Plugin.IsPostEnding())
                return true;
            if (Plugin.BridgeHesitanceCheckpoint(__instance))
                return true;
            Traverse.Create(__instance).Method("PreventBridgeUse").GetValue();
            return false;
        }
    }

    // Adds the Flow Bridge Stabilizer to the Enhance workshop.
    [HarmonyPatch(typeof(WorkshopManager), nameof(WorkshopManager.Instance), MethodType.Getter)]
    internal static class WorkshopInjectPatch
    {
        private static void Postfix(WorkshopManager __result)
        {
            var enhance = __result?.GetWorkshop(WorkshopType.Enhance);
            if (enhance == null || enhance.GetPart(Plugin.StabilizerPartId) != null)
                return;

            var cost = new ItemCountArray();
            cost.array = new[] { new ItemCount { itemId = Plugin.HyperrustItemId, itemCount = 1 } };
            var part = new WorkshopPartInfo
            {
                id = Plugin.StabilizerPartId,
                buildConditions = new ConditionHandler(),
                displayConditions = new ConditionHandler(),
                cost = cost,
                workshopType = WorkshopType.Enhance
            };
            Traverse.Create(part).Field("locKey").SetValue(Plugin.StabilizerLocKey);

            var parts = new List<WorkshopPartInfo>(enhance.parts.array) { part };
            enhance.parts.array = parts.ToArray();
            Plugin.Log.LogInfo("Added Flow Bridge Stabilizer to the Enhance workshop; parts: "
                + string.Join(", ", parts.ConvertAll(p => p.id).ToArray()));
        }
    }

    // The stabilizer can only be built/shown after the ending, when bridge access requires it.
    [HarmonyPatch(typeof(WorkshopPartInfo), nameof(WorkshopPartInfo.IsAllowedToBeBuilt), MethodType.Getter)]
    internal static class StabilizerAllowedPatch
    {
        private static void Postfix(WorkshopPartInfo __instance, ref bool __result)
        {
            if (__instance.id == Plugin.StabilizerPartId)
                __result = Plugin.IsPostEnding() && Plugin.BridgeAccess.Value == BridgeAccessMode.RequireStabilizer;
        }
    }

    [HarmonyPatch(typeof(WorkshopPartInfo), nameof(WorkshopPartInfo.DisplayInMenu), MethodType.Getter)]
    internal static class StabilizerDisplayPatch
    {
        private static void Postfix(WorkshopPartInfo __instance, ref bool __result)
        {
            if (__instance.id == Plugin.StabilizerPartId)
                __result = !__instance.IsBuilt && __instance.IsAllowedToBeBuilt;
        }
    }

    [HarmonyPatch(typeof(WorkshopPartInfo), nameof(WorkshopPartInfo.Build))]
    internal static class StabilizerBuiltPatch
    {
        private static void Postfix(WorkshopPartInfo __instance)
        {
            if (__instance.id == Plugin.StabilizerPartId)
                Plugin.Log.LogInfo("Flow Bridge Stabilizer built; flow bridges can be crossed safely");
        }
    }

    // Skips the Ozias arrival variants of the zone 6 chatter once the bridge is severed. The knots cycle
    // through their stitches on each visit, so we dry-run the knot: if the variant about to play is an
    // Ozias one, keep the advanced visit count (the real start then picks the next variant); otherwise
    // restore the story state untouched.
    [HarmonyPatch(typeof(DialogManager), "InternalStartStory")]
    internal static class OziasChatterPatch
    {
        private static void Prefix(string dialog)
        {
            if (dialog == null || !Plugin.OziasChatterKnots.Contains(dialog)
                || !Plugin.Enabled.Value || !Plugin.DisableOziasArrivalLines.Value || !FunctionCondition.IsInEpilogue())
                return;

            var story = Traverse.Create(typeof(DialogManager)).Field("story").GetValue<Ink.Runtime.Story>();
            if (story == null)
                return;

            string snapshot = story.state.ToJson();
            for (int attempt = 0; attempt < 8; attempt++)
            {
                string firstKey = PeekFirstLineKey(story, dialog);
                if (firstKey == null || !Plugin.OziasArrivalFirstLines.Contains(firstKey))
                {
                    story.state.LoadJson(snapshot);
                    return;
                }
                Plugin.Log.LogInfo($"Skipped Ozias arrival chatter ({firstKey})");
                snapshot = story.state.ToJson();
            }
            story.state.LoadJson(snapshot);
        }

        private static string PeekFirstLineKey(Ink.Runtime.Story story, string knot)
        {
            story.ChoosePathString(knot, true);
            while (story.canContinue)
            {
                string line = story.Continue().Trim();
                if (line.Length == 0)
                    continue;
                int colon = line.IndexOf(':');
                return colon < 0 ? line : line.Substring(colon + 1).Trim();
            }
            return null;
        }
    }

    // Text for the mod's own keys (English; other languages fall back to English).
    [HarmonyPatch(typeof(LocalizationManager), nameof(LocalizationManager.UpdateLocalizationDictionary))]
    internal static class LocalizationPatch
    {
        private static void Postfix(LocalizationManager __instance)
        {
            var stabilizer = new LocalizationData { key = Plugin.StabilizerLocKey, platform = "default" };
            stabilizer.SetText("EN", "Flow Bridge Stabilizer");
            Traverse.Create(__instance).Method("LoadLocIntoDictionary", new[] { typeof(List<LocalizationData>) })
                .GetValue(new List<LocalizationData> { stabilizer });
        }
    }
}
