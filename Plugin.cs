using System.ComponentModel;
using BepInEx;
using UnityEngine;
using MyBox;
using static VLB.Consts;
using UnityEngine.SceneManagement;
using System;
using BepInEx.Configuration;
using Lean.Pool;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using HarmonyLib;

namespace SmartPocketBoxes
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {

        public static Plugin Instance;

        public PlayerInteraction _interaction;
        private BoxGenerator _boxGenerator;
        public BoxInteraction _boxInteraction;

        private ConfigEntry<KeyboardShortcut> putBoxAwayKey;
        private ConfigEntry<KeyboardShortcut> toggleOpenBoxOnFloorKey;
        private ConfigEntry<KeyboardShortcut> consolidateRackKey;
        private ConfigEntry<bool> consolidateRackFeature;
        private ConfigEntry<KeyboardShortcut> spawnBoxKey;
        private ConfigEntry<bool> autoConsolidate;
        public ConfigEntry<bool> enableBackpack;
        public ConfigEntry<KeyboardShortcut> backpackInteract;

        public Backpack backpack;

        private void Awake()
        {
            Instance = this;

            SetupConfigs();

            if (autoConsolidate.Value)
            {
                Harmony.CreateAndPatchAll(typeof(BoxInteractionPatch));
                Harmony.CreateAndPatchAll(typeof(Backpack));
            }

            if(enableBackpack.Value)
            {
                Harmony.CreateAndPatchAll(typeof(BackpackRackPatches));
            }

            SceneManager.activeSceneChanged += (Scene s1, Scene s2) =>
            {
                _interaction = Singleton<PlayerInteraction>.Instance;
                _boxGenerator = Singleton<BoxGenerator>.Instance;

                if(_interaction != null)
                {
                    _boxInteraction = _interaction.GetComponent<BoxInteraction>();

                    backpack = _boxInteraction.GetOrAddComponent<Backpack>();
                }
            };
        }

        private void SetupConfigs()
        {
            putBoxAwayKey = Config.Bind(
                "Keybinds",
                "Put Box Away Key",
                new KeyboardShortcut(KeyCode.T),
                "Use this key to put an empty box back in your pocket."
            );

            toggleOpenBoxOnFloorKey = Config.Bind(
                "Keybinds",
                "Toggle Open Box On Floor Key",
                new KeyboardShortcut(KeyCode.V),
                "Use this to open/close boxes while they are on the floor."
            );

            consolidateRackKey = Config.Bind(
                "Keybinds",
                "Consolidate Rack Key",
                new KeyboardShortcut(KeyCode.X),
                "Use this to consilidate a rack into as few boxes as possible."
            );

            autoConsolidate = Config.Bind(
                "Features",
                "Automatically Consolidate Racks",
                true,
                "Automatically consolidate product when placing boxes on storage racks"
            );

            consolidateRackFeature = Config.Bind(
                "Features",
                "Consolidate Rack Feature",
                true,
                "Use this to enable/disable the consolidate rack feature."
            );

            spawnBoxKey = Config.Bind(
                "Keybinds",
                "Spawn Box",
                new KeyboardShortcut(KeyCode.Mouse1),
                "Use this to spawn an empty box while looking at a product label."
            );

            enableBackpack = Config.Bind(
                "Features",
                "Backpack",
                true,
                "Essentially a portable storage rack"
            );

            backpackInteract = Config.Bind(
                "Keybinds",
                "Backpack Interact",
                new KeyboardShortcut(KeyCode.B, new KeyCode[] { KeyCode.LeftControl }),
                "Use this to interact with backpack. If box in hand, insert. If not, take out."
            );
        }

        private void Update()
        {
            if (_interaction == null || _boxGenerator == null) return;

            if(!backpack.setup)
            {
                backpack.Setup(this, Logger);
                return;
            }

            if(backpackInteract.Value.IsDown())
            {
                return;
            }

            Box held_box = _boxInteraction.m_Box;

            if (putBoxAwayKey.Value.IsDown() && held_box != null)
            {
                if(held_box.Data.Product == null)
                {
                    ThrowBoxIntoTrashBin();
                }
                return;
            }

            if(spawnBoxKey.Value.IsDown() && held_box == null)
            {
                SpawnEmptyBoxByLabel(GetAimedLabel());
                return;
            }

            if(Input.GetMouseButtonDown(1) && held_box != null)
            {
                HandleBoxToBoxTransfer(GetAimedBox(), held_box);
                return;
            }

            if(held_box != null && Input.GetMouseButtonDown(0))
            {
                HandleBoxToBoxTransfer(held_box, GetAimedBox());
                return;
            }

            if(toggleOpenBoxOnFloorKey.Value.IsDown())
            {
                ToggleOpenBox(GetAimedBox());
                return;
            }
            
            if(consolidateRackFeature.Value == true && consolidateRackKey.Value.IsDown())
            {
                Label aimed_label = GetAimedLabel();
                if(aimed_label != null & aimed_label.m_RackSlot != null)
                {
                    Logger.LogError("test2");
                    ConsolidateRack(aimed_label.m_RackSlot);
                }
                return;
            }
        }

        public void ConsolidateRack(RackSlot slot)
        {
            Logger.LogError($"START RACK CONSOLIDATION | {slot.Data.RackedBoxDatas[0].Product.ProductName}");

            RackSlotData slot_data = slot.Data;

            if (slot_data.RackedBoxDatas.Count == 0) return;

            ProductSO slot_product = slot_data.RackedBoxDatas[0].Product;
            int product_per_box = slot_product.GridLayoutInBox.productCount;

            int product_to_consolidate = slot_data.TotalProductCount;

            int boxes_required = (int)Math.Ceiling((double)product_to_consolidate / product_per_box);
            int boxes_to_remove = slot_data.BoxCount - boxes_required;
            int amount_consolidated = 0;

            for (int i = 0; i < boxes_to_remove; i++)
            {
                Box box = slot.TakeBoxFromRack();

                Singleton<InventoryManager>.Instance.RemoveBox(box.Data);
                LeanPool.Despawn(box.gameObject, 0f);
                box.ResetBox();

                Destroy(box.gameObject);
            }

            List<Box> slot_boxes = new List<Box>();
            foreach (Box box in slot.m_Boxes)
            {
                slot_boxes.Add(box);
            }
            slot_boxes.Reverse();

            foreach(Box box in slot_boxes)
            {
                int left_to_consolidate = product_to_consolidate - amount_consolidated;
                int consolidate_now = product_per_box;

                if(consolidate_now > left_to_consolidate)
                {
                    consolidate_now = left_to_consolidate;
                }

                box.DespawnProducts();

                box.m_Data.ProductCount = consolidate_now;
                amount_consolidated += consolidate_now;
            }

            slot.SetLabel();
            Logger.LogError($"CONSOLIDATED {product_to_consolidate} INTO {boxes_required} BOXES.");
        }
        private void ToggleOpenBox(Box box)
        {
            if (box != null)
            {
                if (box.IsOpen)
                {
                    box.CloseBox();
                }
                else
                {
                    box.OpenBox();
                }
            }
        }

        private void SpawnEmptyBoxByLabel(Label label)
        {
            if (label == null) return;
            ProductSO product = null;
            if (label.DisplaySlot == null)
            {
                product = IDManager.Instance.ProductSO(label.m_RackSlot.m_Data.ProductID);
            }
            else
            {
                product = IDManager.Instance.ProductSO(label.DisplaySlot.ProductID);
            }

            if (product == null) return;

            BoxData data = new BoxData();
            data.Size = product.GridLayoutInBox.boxSize;

            Box box = _boxGenerator.SpawnBox(new Vector3(0, 0), Quaternion.identity, data);
            Singleton<InventoryManager>.Instance.AddBox(box.Data);

            _interaction.m_CurrentInteractable = box;
            _interaction.Interact();
        }

        private void HandleBoxToBoxTransfer(Box source, Box destination)
        {
            Logger.LogError("StartBoxTransfer");
            if (source == null || destination == null || !source.IsOpen || !destination.IsOpen || source.Product == null) return;
            if (destination.HasProducts && destination.Product != source.Product) {
                Singleton<WarningSystem>.Instance.RaiseInteractionWarning(InteractionWarningType.PRODUCTS_MUSTBE_SAME, Array.Empty<string>());
                return;
            } else if (source.Size != destination.Size) {
                Singleton<WarningSystem>.Instance.RaiseInteractionWarning(InteractionWarningType.BOX_DOESNT_MATCH, Array.Empty<string>());
                return;
            } else if (destination.Full) {
                Singleton<WarningSystem>.Instance.RaiseInteractionWarning(InteractionWarningType.FULL_BOX, Array.Empty<string>());
                return;
            }

            int product_id = source.Product.ID;

            Product product = source.GetProductFromBox();
            if (product == null) return;

            destination.AddProduct(product_id, product);
            Singleton<SFXManager>.Instance.PlayPlacingProductSFX();
        }

        public Box GetAimedBox()
        {
            GameObject obj = GetRaycastObject();
            if(obj == null) return null;
            return obj.GetComponent<Box>();
        }

        private Label GetAimedLabel()
        {
            GameObject obj = GetRaycastObject();
            if (obj == null) return null;
            return obj.GetComponent<Label>();
        }

        public RackSlot GetAimedRackSlot()
        {
            GameObject obj = GetRaycastObject();
            if (obj == null) return null;
            return obj.GetComponent<RackSlot>();
        }

        private GameObject GetRaycastObject()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;

            // Check if the ray hits any object
            if (Physics.Raycast(ray, out hitInfo, _interaction.m_InteractionDistance))
            {
                return hitInfo.collider.gameObject;
            }
            return null;
        }

        //taken from boxinteraction
        private void ThrowBoxIntoTrashBin()
        {
            _boxInteraction.ThrowIntoTrashBin();
        }
    }
}
