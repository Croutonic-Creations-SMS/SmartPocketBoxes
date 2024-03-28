﻿using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using UnityEngine;
using BepInEx;
using System.Collections;

namespace SmartPocketBoxes
{
    class BoxInteractionPatch
    {
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