using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using MyBox;
using UnityEngine;
using BepInEx.Logging;
using Logger = BepInEx.Logging.Logger;
using UnityEngine.UIElements;
using System.Collections;
using HarmonyLib;
using Lean;
using Lean.Pool;

namespace SmartPocketBoxes
{
    public class Backpack : MonoBehaviour
    {
        private ManualLogSource _logger;

        private Plugin _plugin;

        public bool setup = false;

        public Rack rack;

        //this is left here to remove the old backpack rack that will no longer be used.
        //will remove eventually
        public void Setup(Plugin plugin, ManualLogSource logger)
        {
            _plugin = plugin;
            _logger = logger;
            setup = true;

            RackManager rack_manager = FindFirstObjectByType<RackManager>();

            foreach(Rack existing_rack in rack_manager.m_Racks)
            {
                if(existing_rack.transform.position.y == 10) //rack is placed at y 10 andn othing else should be
                {
                    rack = existing_rack;
                }
            }

            //if (plugin.enableBackpack.Value == false)
            //{
                if(rack != null)
                {
                    rack_manager.RemoveRack(rack);
                }
                Destroy(this);
            //}

            return;
        }
    }
}
