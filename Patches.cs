using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using UnityEngine;
using BepInEx;
using System.Collections;
using MyBox;

namespace SmartPocketBoxes
{
    class Patches
    {
        [HarmonyPatch(typeof(PlayerInteraction), "Start")]
        [HarmonyPostfix]
        static void PlayerInteractionStartPostfix(PlayerInteraction __instance)
        {
            Plugin.Instance._interaction = __instance;
            Plugin.Instance._boxInteraction = __instance.GetComponent<BoxInteraction>();
        }

        [HarmonyPatch(typeof(PlayerObjectHolder), "ThrowObject")]
        [HarmonyPostfix]
        static void PlayerObjectHolderThrowObjectPostfix(bool __result)
        {
            if(__result)
                Plugin.Instance._boxInteraction.m_Box = null;
        }

        [HarmonyPatch(typeof(Box), "SpawnProducts")]
        [HarmonyPrefix]
        static bool SpawnProductsPrefix(Box __instance)
        {
            if (!__instance.HasProducts)
            {
                return false;
            }
            return true;
        }
    }
}
