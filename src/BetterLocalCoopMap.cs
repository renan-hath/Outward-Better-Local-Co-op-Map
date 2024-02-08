using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;

namespace OutwardModTemplate
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class MyMod : BaseUnityPlugin
    {
        public const string GUID = "com.lostjak.betterlocalcoopmap";
        public const string NAME = "Better Local Co-op Map";
        public const string VERSION = "1.0.0";

        internal static ManualLogSource Log;
        public static ConfigEntry<bool> ExampleConfig;

        internal void Awake()
        {
            var harmony = new Harmony(GUID);
            harmony.PatchAll();
            Log = this.Logger;
        }

        internal void Update()
        {
        }

        [HarmonyPatch(typeof(CharacterManager), nameof(CharacterManager.UpdateActiveMapCategories))]
        public class CharacterManager_UpdateActiveMapCategories
        {
            [HarmonyPrefix]
            public static bool Prefix()
            {
                Override();
                return false;
            }

            static void Override()
            {
                if (!ControlsInput.IsInputReady)
                {
                    return;
                }

                IList<SplitPlayer> localPlayers = SplitScreenManager.Instance.LocalPlayers;

                for (int i = 0; i < localPlayers.Count; i++)
                {
                    CharacterUI charUI = localPlayers[i].CharUI;
                    Character assignedCharacter = localPlayers[i].AssignedCharacter;
                    bool isPlayerViewingMap = false;

                    if (charUI)
                    {
                        // If map is opened
                        if (MenuManager.Instance.IsMapDisplayed)
                        {
                            // If current player is the one who opened the map
                            if (charUI == MenuManager.Instance.m_mapScreen.CharacterUI)
                                isPlayerViewingMap = true;
                            else
                                isPlayerViewingMap = false;
                        }

                        bool flag = assignedCharacter &&
                                   !charUI.GetIsMenuDisplayed(CharacterUI.MenuScreens.PauseMenu) &&
                                   !NetworkLevelLoader.Instance.IsGameplayPaused &&
                                   // If any player has opened the map
                                   /*!MenuManager.Instance.IsMapDisplayed &&*/
                                   // Instead of checking if the map is opened, it now checks
                                   // if the current player is the one who opened it
                                   !isPlayerViewingMap &&
                                   !MenuManager.Instance.IsConnectionScreenDisplayed &&
                                   !MenuManager.Instance.InFade &&
                                    MenuManager.Instance.IsApplicationFocused;

                        bool flag2 = flag && !charUI.IsMenuFocused;
                        bool flag3 = flag2 && !charUI.IsDialogueInProgress;
                        bool active = flag3 && !assignedCharacter.Deploying;
                        bool active2 = flag3 && assignedCharacter.Deploying;
                        bool active3 = flag && /*(!assignedCharacter.CharacterUI.IsMenuFocused ||
                                                  assignedCharacter.CharacterUI.GetIsMenuDisplayed(CharacterUI.MenuScreens.QuickSlotAssignation)) &&*/
                                                 !assignedCharacter.CharacterUI.IsDialogueInProgress;

                        ControlsInput.SetMovementActive(charUI.RewiredID, flag3);
                        ControlsInput.SetCameraActive(charUI.RewiredID, flag2);
                        ControlsInput.SetActionActive(charUI.RewiredID, active);
                        ControlsInput.SetDeployActive(charUI.RewiredID, active2);
                        ControlsInput.SetQuickSlotActive(charUI.RewiredID, active3);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(CharacterUI), nameof(CharacterUI.ToggleMenu))]
        public class CharacterUI_ToggleMenu
        {
            [HarmonyPrefix]
            public static bool Prefix(CharacterUI __instance, CharacterUI.MenuScreens _menu, bool _failSafe = true)
            {
		// Map will only be closed if the player who's opening a menu is
		// the same who's opened the map
                if (MenuManager.Instance.MapOwnerPlayerID == __instance.RewiredID)
                    MenuManager.Instance.HideMap();

                if (__instance.m_menus[(int)_menu])
                {
                    if (__instance.m_menus[(int)_menu].IsDisplayed)
                        __instance.HideMenu(_menu, true);
                    else
                        __instance.ShowMenu(_menu);
                }
                

                return false;
            }
        }
    }
}