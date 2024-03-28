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
        private RackSlot slot;

        private BoxInteraction _boxInteraction;

        public void Setup(Plugin plugin, ManualLogSource logger)
        {
            _plugin = plugin;
            _logger = logger;
            setup = true;

            _logger.LogError("Getting Rack Manager");
            RackManager rack_manager = FindFirstObjectByType<RackManager>();

            foreach(Rack existing_rack in rack_manager.m_Racks)
            {
                if(existing_rack.transform.position.y == 10) //rack is placed at y 10 andn othing else should be
                {
                    rack = existing_rack;
                }
            }

            if (plugin.enableBackpack.Value == false)
            {
                if(rack != null)
                {
                    rack_manager.RemoveRack(rack);
                }
                Destroy(this);
            }

            if(this.rack == null)
            {
                Vector3 player_position = Singleton<PlayerController>.Instance.transform.position;

                rack = Singleton<FurnitureGenerator>.Instance.SpawnFurniture(7, player_position + new Vector3(-10, 10, 0)).GetComponent<Rack>();

                rack_manager.m_Racks.Add(rack);
                rack_manager.m_RackDatas.Add(rack.Data);
            }

            slot = rack.RackSlots[0];

            _boxInteraction = _plugin._boxInteraction;
        }

        public bool cooldown = false;
        public IEnumerator DoCooldown()
        {
            cooldown = true;
            yield return new WaitForSecondsRealtime(.15f);
            cooldown = false;
        }

        private void Update()
        {
            if (!setup || cooldown) return;
            if (Input.GetKeyDown(_plugin.backpackInteract.Value.MainKey))
            {
                BoxInteraction _box_interaction = _plugin._boxInteraction;

                RackSlot aimed_slot = _plugin.GetAimedRackSlot();
                if(aimed_slot != null && _box_interaction.m_Box == null)
                {
                    if(_plugin.backpackInteract.Value.IsDown())
                    {
                        _logger.LogError("Attempt to take place into rack");
                        Box rack_box = slot.TakeBoxFromRack();

                        if (rack_box != null)
                        {
                            if (!PlaceBoxToRack(rack_box, aimed_slot))
                            {
                                slot.AddBox(rack_box.BoxID, rack_box);
                            } else
                            {
                                _plugin.ConsolidateRack(aimed_slot);
                            }
                        }
                    } else
                    {
                        _logger.LogError("Attempt to take from rack");
                        Box rack_box = aimed_slot.TakeBoxFromRack();

                        if (rack_box != null)
                        {
                            if (!PlaceBoxToRack(rack_box, slot))
                            {
                                aimed_slot.AddBox(rack_box.BoxID, rack_box);
                            } else
                            {
                                _plugin.ConsolidateRack(slot);
                            }
                        }
                    }
                    return;
                }

                Box aimed_box = _plugin.GetAimedBox();
                if(aimed_box != null && aimed_box.Racked == false && _box_interaction.m_Box == null)
                {
                    if ((slot.Data.ProductID != -1 && slot.Data.TotalProductCount > 0 && slot.Data.ProductID != aimed_box.Data.ProductID) || (slot.Data.BoxCount > 0 && slot.Data.ProductID == -1 && aimed_box.Data.ProductID != -1))
                    {
                        Singleton<WarningSystem>.Instance.RaiseInteractionWarning(InteractionWarningType.PRODUCTS_MUSTBE_SAME);
                    } else if (slot.CurrentBoxID != -1 && aimed_box.BoxID != slot.CurrentBoxID)
                    {
                        Singleton<WarningSystem>.Instance.RaiseInteractionWarning(InteractionWarningType.BOX_DOESNT_MATCH);
                    } else if (slot.Full)
                    {
                        Singleton<WarningSystem>.Instance.RaiseInteractionWarning(InteractionWarningType.FULL_RACK);
                    } else
                    {
                        PlaceBoxToRack(aimed_box, slot);
                    }

                } else  if (_box_interaction.m_Box != null) {
                    PlaceBoxToRack(_box_interaction.m_Box, slot);
                } else {
                    if (slot.HasBox && _box_interaction.m_Box == null && !_plugin._interaction.InInteraction)
                    {
                        Box box = slot.TakeBoxFromRack();

                        _plugin._interaction.m_CurrentInteractable = box;
                        _plugin._interaction.Interact();
                        StartCoroutine(DoCooldown());
                    }
                }
                
                _box_interaction.m_CurrentRackSlot = null;
            }
        }

        public bool PlaceBoxToRack(Box box, RackSlot slot)
        {
            if (!(slot == null) && !(box == null))
            {
                if ((slot.Data.ProductID != -1 && slot.Data.TotalProductCount > 0 && slot.Data.ProductID != box.Data.ProductID) || (slot.Data.BoxCount > 0 && slot.Data.ProductID == -1 && box.Data.ProductID != -1))
                {
                    Singleton<WarningSystem>.Instance.RaiseInteractionWarning(InteractionWarningType.PRODUCTS_MUSTBE_SAME);
                    return false;
                }

                if (slot.CurrentBoxID != -1 && box.BoxID != slot.CurrentBoxID)
                {
                    Singleton<WarningSystem>.Instance.RaiseInteractionWarning(InteractionWarningType.BOX_DOESNT_MATCH);
                    return false;
                }

                if (slot.Full)
                {
                    Singleton<WarningSystem>.Instance.RaiseInteractionWarning(InteractionWarningType.FULL_RACK);
                    return false;
                }

                box.gameObject.layer = Singleton<PlayerObjectHolder>.Instance.m_CurrentObjectsLayer;
                Collider[] componentsInChildren = box.gameObject.GetComponentsInChildren<Collider>();
                for (int i = 0; i < componentsInChildren.Length; i++)
                {
                    componentsInChildren[i].isTrigger = false;
                }

                box.Racked = true;
                slot.AddBox(box.BoxID, box);

                _boxInteraction.m_Box = null;

                if (Singleton<PlayerInteraction>.Instance.InInteraction)
                {
                    Singleton<PlayerInteraction>.Instance.InteractionEnd(_boxInteraction);
                    _boxInteraction.DefaultHints(show: false);
                }
                
                Singleton<HighlightManager>.Instance.SetHighlight(null);
                Singleton<SFXManager>.Instance.PlayDroppingBoxSFX();

                StartCoroutine(DoCooldown());
                return true;
            }
            return false;
        }

    }
}
