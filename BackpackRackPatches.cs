using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using MyBox;

namespace SmartPocketBoxes
{
    class BackpackRackPatches
    {
        [HarmonyPatch(typeof(RackManager), nameof(RackManager.GetRackSlotThatHasSpaceFor))]
        [HarmonyPrefix]
        static void HasSpaceForPrefix(int productID, int boxID, RackManager __instance)
        {
            Rack backpackRack = Plugin.Instance.backpack.rack;
            backpackRack.RemoveFromRackManagerWhileCarrying();
        }

        [HarmonyPatch(typeof(RackManager), nameof(RackManager.GetRackSlotThatHas))]
        [HarmonyPrefix]
        static void HasPrefix(int productID, RackManager __instance)
        {
            Rack backpackRack = Plugin.Instance.backpack.rack;
            backpackRack.RemoveFromRackManagerWhileCarrying();
        }

        [HarmonyPatch(typeof(RackManager), nameof(RackManager.GetRackSlotThatHasSpaceFor))]
        [HarmonyPostfix]
        static void HasSpaceForPostfix(int productID, int boxID, RackManager __instance)
        {
            Rack backpackRack = Plugin.Instance.backpack.rack;
            backpackRack.AddBackToRackManagerAfterPlaced();
        }

        [HarmonyPatch(typeof(RackManager), nameof(RackManager.GetRackSlotThatHas))]
        [HarmonyPostfix]
        static void HasPostfix(int productID, RackManager __instance)
        {
            Rack backpackRack = Plugin.Instance.backpack.rack;
            backpackRack.AddBackToRackManagerAfterPlaced();
        }
    }
}
