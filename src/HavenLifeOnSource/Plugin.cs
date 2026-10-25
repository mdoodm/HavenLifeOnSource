using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace HavenLifeOnSource
{
    public enum BridgeAccessMode
    {
        // Epilogue bridge locks stay (vanilla behaviour).
        Vanilla,
        // Bridges open once the Flow Bridge Stabilizer has been built at the Enhance bench.
        RequireStabilizer,
        // Bridges open as soon as the ending has played.
        AlwaysOpen
    }

    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "lifeonsource.haven";
        public const string Name = "Haven: Life on Source";
        public const string Version = "1.4.1";

        // Enhance workshop part added by this mod. The game saves its built state under this id,
        // so it must not change between versions.
        internal const string StabilizerPartId = "EE_FlowBridgeStabilizer";
        internal const string StabilizerLocKey = "EE_PART_FLOWBRIDGESTABILIZER";
        internal const string HyperrustItemId = "Key_Unlock_Hyperrust";

        // StoryEventParams "Nest_Bedroom_DialogAnimated_EpilogueEnd" (plays Epilogue_Unique_Sleep_TheEnd).
        internal const string EndingStoryEventId = "38a3e70b-ac60-40df-84e9-52171bbdcddb";
        // Global GV "Epilogue_PreventGameSave", set when the Flower Boots are acquired and never cleared.
        internal const string PreventSaveGVId = "49fc8d0c-1863-4b55-9549-0939b5e7bb1a";

        internal static ManualLogSource Log;
        internal static Plugin Instance;
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<bool> UnlockNestActivities;
        internal static ConfigEntry<bool> RestoreNestIdles;
        internal static ConfigEntry<BridgeAccessMode> BridgeAccess;
        internal static ConfigEntry<bool> YuBridgeHesitance;
        internal static ConfigEntry<bool> DisableOziasArrivalLines;

        // Roaming chatter from zone 6 still cycles once the flow bridge is severed. These knots cycle
        // through stitches A..H; the variants about Ozias coming to Source are skipped, identified by
        // the loc key of their first line.
        internal static readonly HashSet<string> OziasChatterKnots = new HashSet<string>
        {
            "ChitChatAfterOzias_KayLead",
            "ChitChatAfterOzias_YuLead"
        };

        internal static readonly HashSet<string> OziasArrivalFirstLines = new HashSet<string>
        {
            "ChitChatAfterOzias_KayLeadA01",
            "ChitChatAfterOzias_KayLeadH01",
            "ChitChatAfterOzias_YuLeadA01"
        };

        /// <summary>
        /// Checkpoint for future content: raised before the MCs cross a flow bridge after the ending.
        /// Return false from a handler to stop the crossing (the MCs turn back, like a vanilla bridge lock).
        /// Only raised when YuBridgeHesitance is enabled.
        /// </summary>
        public static event Func<BridgeGPE, bool> BeforePostEndingBridgeCross;

        // Set when we skipped the epilogue's "return to main menu"; consumed when the IGS ends.
        internal static bool PendingContinue;
        // Session fallback in case the story event state isn't available yet.
        private static bool endingSeenThisSession;
        private static StoryEventParams endingEvent;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            Enabled = Config.Bind("General", "Enabled", true,
                "After the epilogue's final scene and credits, stay in the world instead of returning to the main menu.");
            UnlockNestActivities = Config.Bind("General", "UnlockNestActivities", true,
                "After the ending, lift the epilogue locks on nest activities (leaving, cooking, crafting, curing, synthesizing...).");
            RestoreNestIdles = Config.Bind("General", "RestoreNestIdles", true,
                "After the ending, use the game's normal context-based nest idle animations instead of the epilogue's fixed couch/bed poses.");
            BridgeAccess = Config.Bind("Bridges", "BridgeAccess", BridgeAccessMode.RequireStabilizer,
                "After the ending, how the epilogue's flow bridge locks are handled. RequireStabilizer: build the Flow Bridge Stabilizer (1 Hyperrust) at the Enhance bench to cross safely, with or without the Flower Boots on. AlwaysOpen: no upgrade needed. Vanilla: bridges stay locked.");
            YuBridgeHesitance = Config.Bind("Bridges", "YuBridgeHesitance", false,
                "Checkpoint for future content where Yu hesitates to risk crossing the flow bridges. Currently it only logs each crossing; nothing happens in game yet.");
            DisableOziasArrivalLines = Config.Bind("Dialogue", "DisableOziasArrivalLines", true,
                "Once the flow bridge is severed, skip the roaming chatter about Ozias coming to Source.");
            GameEventManager.OnGameStateDataLoaded += OnGameStateDataLoaded;
            new Harmony(Guid).PatchAll();
            Log.LogInfo($"{Name} {Version} loaded");
        }

        internal static bool EndingSeen()
        {
            if (endingSeenThisSession)
                return true;
            var sep = FindEndingEvent();
            return sep != null && sep.WasPlayed;
        }

        internal static bool IsPostEnding()
        {
            return Enabled.Value && FunctionCondition.IsInEpilogue() && EndingSeen();
        }

        internal static bool BridgesOpen()
        {
            switch (BridgeAccess.Value)
            {
                case BridgeAccessMode.AlwaysOpen:
                    return true;
                case BridgeAccessMode.RequireStabilizer:
                    return UnitySingleton<GameDataManager>.Instance.StoryData.IsHomePanelContructionBuilt(WorkshopType.Enhance, StabilizerPartId);
                default:
                    return false;
            }
        }

        // Returns false when the crossing should be cancelled.
        internal static bool BridgeHesitanceCheckpoint(BridgeGPE bridge)
        {
            Log.LogInfo($"Bridge hesitance checkpoint: {bridge.name} (no content yet, crossing allowed)");
            var handlers = BeforePostEndingBridgeCross;
            if (handlers == null)
                return true;
            foreach (Func<BridgeGPE, bool> handler in handlers.GetInvocationList())
            {
                if (!handler(bridge))
                    return false;
            }
            return true;
        }

        private static StoryEventParams FindEndingEvent()
        {
            if (endingEvent != null)
                return endingEvent;
            if (!UnitySingleton<StoryEventManager>.IsAvailable())
                return null;
            foreach (var sep in StoryEventManager.GetStoryEventParamsList())
            {
                if (sep != null && sep.ID == EndingStoryEventId)
                    return endingEvent = sep;
            }
            return null;
        }

        private void OnGameStateDataLoaded()
        {
            // Each loaded slot decides for itself; don't carry the flag across slots.
            endingSeenThisSession = false;
            if (Enabled.Value && FunctionCondition.IsInEpilogue() && EndingSeen())
            {
                ClearPreventSave();
                StartCoroutine(GrantHyperrustWhenReady());
            }
        }

        private IEnumerator GrantHyperrustWhenReady()
        {
            var ggm = UnitySingleton<GlobalGameManager>.Instance;
            while (ggm.loadingState != GlobalGameManager.LoadingState.GameStarted || UnitySingleton<IGSManager>.Instance.IsPlayingIGS)
                yield return null;
            yield return new WaitForSecondsRealtime(1f);
            if (GrantHyperrustIfNeeded())
                GameEventManager.RequestSave(SavingIndicatorHUD.FeedbackType.Immediately);
        }

        // All Hyperrust is usually spent on the gloves before the ending and can't be found again with
        // the bridges locked, so give one for the stabilizer if the player has none.
        private static bool GrantHyperrustIfNeeded()
        {
            if (BridgeAccess.Value != BridgeAccessMode.RequireStabilizer || BridgesOpen())
                return false;
            var inventory = UnitySingleton<InventoryManager>.Instance;
            if (inventory.GetItemCount(HyperrustItemId) > 0)
                return false;
            inventory.Store(HyperrustItemId, 1);
            Log.LogInfo("Granted 1 Hyperrust for the Flow Bridge Stabilizer");
            return true;
        }

        private static void ClearPreventSave()
        {
            var gv = GVManager.GetGlobalGameVariable(PreventSaveGVId);
            if (gv != null && gv.value.floatValue != 0f)
            {
                gv.SetValue(0f);
                Log.LogInfo("Cleared Epilogue_PreventGameSave");
            }
        }

        internal void ContinueAfterEnding()
        {
            StartCoroutine(ContinueRoutine());
        }

        private IEnumerator ContinueRoutine()
        {
            endingSeenThisSession = true;
            var sep = FindEndingEvent();
            if (sep != null && !sep.WasPlayed)
                sep.StateData.timePlayed = UnitySingleton<GameTimeManager>.Instance.GameTime;

            // Let the IGS camera transition settle before touching fade/save.
            yield return new WaitForSecondsRealtime(1.5f);

            var fade = UnitySingleton<FadeManager>.Instance;
            if (fade != null && fade.CurrentAlpha > 0f)
            {
                var fadeIn = Tweakables.instance.fadeManagerSettings.defaultFadeIn;
                fade.Fade(fadeIn.fadeType, fadeIn.duration);
                Log.LogInfo("Faded screen back in after ending");
            }

            while (UnitySingleton<IGSManager>.Instance.IsPlayingIGS)
                yield return null;
            yield return new WaitForSecondsRealtime(1f);

            // The epilogue blocks game saves; lift that so "Epilogue" on the title screen resumes here.
            ClearPreventSave();
            GrantHyperrustIfNeeded();
            GameEventManager.RequestSave(SavingIndicatorHUD.FeedbackType.Immediately);
            Log.LogInfo("Saved post-ending state; free roam continues");
        }
    }
}
