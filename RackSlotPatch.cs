using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using UnityEngine;
using BepInEx;
using System.Collections;

namespace SmartPocketBoxes
{
    class RackSlotPatch
    {
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

        [HarmonyPatch(typeof(BoxInteraction), "PlaceBoxToRack")]
        [HarmonyPrefix]
        static bool PlayerPlaceToRackPrefix(BoxInteraction __instance)
        {
            if (!__instance.m_Box.HasProducts) return false;
            if(__instance.m_Box != null && __instance.m_Box.Data.ProductCount > 0 && __instance.m_CurrentRackSlot.Full)
            {
                return !(Plugin.Instance.ConsolidateBoxToFullRack(__instance));
            }
            return true;
        }

        [HarmonyPatch(typeof(RackSlot), "AddBox")]
        [HarmonyPrefix]
        static void Prefix(RackSlot __instance, out RackSlot __state)
        {
            __state = __instance;
        }

        [HarmonyPatch(typeof(RackSlot), "AddBox")]
        [HarmonyPostfix]
        static void Postfix(RackSlot __instance, ref RackSlot __state)
        {
            Plugin.Instance.ConsolidateRack(__state);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Restocker), "PickUpBox")]
        static bool StockerConsolidateBeforePickup(Restocker __instance)
        {
            Plugin.Instance.ConsolidateRack(__instance.m_TargetRackSlot);
            return true;
        }
    }
}
