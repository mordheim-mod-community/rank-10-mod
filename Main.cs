using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityModManagerNet;

namespace Rank10Mod
{
    public class Main
    {
        public static UnityModManager.ModEntry mod;

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            mod = modEntry;

            return true; // If false the mod will show an error.
        }
    }

    // Start new campaign at rank 10
    [HarmonyPatch(typeof(SaveManager))]
    [HarmonyPatch("NewCampaign")]
    static class SaveManager_NewCampaign_Patch
    {
        public static bool startedNewCampaign = false;
        static void Prefix(ref int forcedWarRank)
        {
            try
            {
                forcedWarRank = 10;
                startedNewCampaign = true;
            }
            catch (Exception e)
            {
                Main.mod.Logger.Error(e.ToString());
            }
        }
    }

    // Set warband rank selection to 10 in the new campaign popup
    [HarmonyPatch(typeof(WarbandRankPopupView))]
    [HarmonyPatch("Show")]
    [HarmonyPatch(new Type[] { typeof(Action<bool, int>), typeof(bool) })]
    static class WarbandRankPopupView_Show_Patch
    {
        static void Postfix(ref WarbandRankPopupView __instance)
        {
            try
            {
                __instance.rank.selections.Add("10");
                __instance.rank.SetCurrentSel(2);
                __instance.rank.SetButtonsVisible(false);
            }
            catch (Exception e)
            {
                Main.mod.Logger.Error(e.ToString());
            }
        }
    }

    // Generate recruiting units at rank 10
    [HarmonyPatch(typeof(Unit))]
    [HarmonyPatch("GenerateUnit")]
    static class Unit_GenerateUnit_Patch
    {
        static void Prefix(ref int rank)
        {
            try
            {
                rank = 10;
            }
            catch (Exception e)
            {
                Main.mod.Logger.Error(e.ToString());
            }
        }
    }

    // Remove injury from recruiting units superior to rank 1
    [HarmonyPatch(typeof(HireUnitInjuryData))]
    [HarmonyPatch("GetRandomRatio")]
    static class HireUnitInjuryData_GetRandomRatio_Patch
    {
        static void Prefix(ref System.Collections.Generic.List<HireUnitInjuryData> datas)
        {
            try
            {
                datas = PandoraSingleton<DataFactory>.Instance.InitData<HireUnitInjuryData>("unit_rank", "0");
            }
            catch (Exception e)
            {
                Main.mod.Logger.Error(e.ToString());
            }
        }
    }

    // Add 500000 gold at the start of a new campaign
    [HarmonyPatch(typeof(WarbandChest))]
    [HarmonyPatch("AddItem")]
    static class WarbandChest_AddItem_Patch
    {
        static void Prefix(ref ItemId itemId, ref int count)
        {
            try
            {
                if (SaveManager_NewCampaign_Patch.startedNewCampaign && itemId == ItemId.GOLD)
                {
                    count = 500000;
                    SaveManager_NewCampaign_Patch.startedNewCampaign = false;
                }
            }
            catch (Exception e)
            {
                Main.mod.Logger.Error(e.ToString());
            }
        }
    }

    // Make all runes available in the inventory
    [HarmonyPatch(typeof(InventoryModule))]
    [HarmonyPatch("SetList")]
    [HarmonyPatch(new Type[] { typeof(System.Collections.Generic.List<RuneMark>), typeof(System.Collections.Generic.List<RuneMark>), typeof(UnityAction<RuneMark>), typeof(UnityAction<RuneMark>), typeof(string) })]
    static class InventoryModule_SetList_Patch
    {
        static void Prefix(ref System.Collections.Generic.List<RuneMark> runeList, ref System.Collections.Generic.List<RuneMark> notAvailableRuneList, ref string reason)
        {
            try
            {
                runeList.Select(r => {
                    r.Reason = "";
                    return r;
                }).ToList();
                notAvailableRuneList.Select(r => {
                    r.Reason = "";
                    return r;
                }).ToList();

                if (reason == "na_enchant_no_slot")
                {
                    return;
                }

                runeList.AddRange(notAvailableRuneList);
                notAvailableRuneList = new System.Collections.Generic.List<RuneMark>(); 
            }
            catch (Exception e)
            {
                Main.mod.Logger.Error(e.ToString());
            }
        }
    }

    // Store the shop module for use in other patches
    [HarmonyPatch(typeof(ShopModule))]
    static class ShopModule_Patch
    {
        static public ShopModule instance;

        [HarmonyPatch("Init")]
        static void Postfix(ref ShopModule __instance)
        {
            try
            {
                instance = __instance;
            }
            catch (Exception e)
            {
                Main.mod.Logger.Error(e.ToString());
            }
        }
    }


    // Refresh market
    [HarmonyPatch(typeof(TabsModule))]
    [HarmonyPatch("SetCurrentTab")]
    static class TabsModule_SetCurrentTab_Patch
    {
        static void Postfix(ref HideoutManager.State state)
        {
            try
            {
                if (state == HideoutManager.State.SHOP)
                {
                    var hideoutManager = PandoraSingleton<HideoutManager>.Instance;
                    hideoutManager.messagePopup.Show(" Market", " Do you want to refresh the market?", new Action<bool>((bool confirm) => 
                    {
                        if (confirm)
                        {
                            hideoutManager.Market.RefreshMarket(MarketEventId.NONE, true);
                            var latestArrivals = PandoraSingleton<HideoutTabManager>.Instance.GetModuleLeft<LatestArrivalModule>(ModuleId.LATEST_ARRIVAL);
                            var addedItems = hideoutManager.Market.GetAddedItems();
                            latestArrivals.Set(hideoutManager.Market.CurrentEventId, addedItems, null, null);
                            ShopModule_Patch.instance.SetTab(ShopModuleTab.BUY);
                        }
                    }), false, false);
                }
            }
            catch (Exception e)
            {
                Main.mod.Logger.Error(e.ToString());
            }
        }
    }

    // Allow adding runes to items even if they already have one or if their quality is too low
    [HarmonyPatch(typeof(Item))]
    static class Item_Patch
    {
        [HarmonyPatch("AddRuneMark")]
        static bool Prefix(Item __instance, ref RuneMarkId runeMarkId, ref RuneMarkQualityId runeQualityId, ref AllegianceId allegianceId)
        {
            try
            {
                Traverse.Create(__instance).Property("itemSave").Property("runeMarkId").SetValue((int)runeMarkId);
                Traverse.Create(__instance).Property("itemSave").Property("runeMarkQualityId").SetValue((int)runeQualityId);
                Traverse.Create(__instance).Property("itemSave").Property("runeMarkId").SetValue((int)allegianceId);
                Traverse.Create(__instance).Property("RuneMark").SetValue(new RuneMark(runeMarkId, runeQualityId, allegianceId, __instance.TypeData.Id));
                return true;
            }
            catch (Exception e)
            {
                Main.mod.Logger.Error(e.ToString());
            }

            return false;
        }
    }

    // Remove time for learning a new skill
    [HarmonyPatch(typeof(Unit))]
    [HarmonyPatch("StartLearningSkill")]
    static class Unit_Patch
    {

        static void Prefix(ref Unit __instance, ref SkillData skillData)
        {
            try
            {
                Traverse.Create(skillData).Property("Time").SetValue(0);
            }
            catch (Exception e)
            {
                Main.mod.Logger.Error(e.ToString());
            }
        }

        static void Postfix(ref Unit __instance)
        {
            try
            {
                __instance.UpdateSkillTraining();
            }
            catch (Exception e)
            {
                Main.mod.Logger.Error(e.ToString());
            }
        }
    }

    // Enable mutation reset
    [HarmonyPatch(typeof(HideoutInventory))]
    static class HideoutInventory_Patch
    {
        [HarmonyPatch("OnWheelMutationSlotConfirmed")]
        static void Postfix(ref HideoutInventory __instance, int mutationIdx)
        {
            try
            {
                var hideoutManager = PandoraSingleton<HideoutManager>.Instance;
                var mutation = hideoutManager.currentUnit.unit.Mutations[mutationIdx];
                var hideoutInventory = __instance;
                var unitController = hideoutManager.currentUnit;
                var unit = unitController.unit;
                var mutations = Traverse.Create(unit).Property("Mutations").GetValue<List<Mutation>>();
                {
                    hideoutManager.messagePopup.Show(" Mutation Reset", " Do you want to reset this mutation? This will remove your mutation and give you a new one.", new Action<bool>((bool confirm) =>
                    {
                        if (confirm)
                        {
                            mutations.RemoveAt(mutationIdx);
                            unit.UnitSave.mutations.Remove(mutation.Data.Id);
                            unit.UnequipAllItems();
                            var previousItems = new List<Item>();
                            unit.AddRandomMutation(previousItems);
                            unit.ApplyChanges();
                            hideoutInventory.OnApplyChanges();
                            unitController.RefreshBodyParts();
                            hideoutManager.StateMachine.Update();
                            hideoutManager.SaveChanges();
                        }
                    }), false, false);
                }
                
            }
            catch (Exception e)
            {
                Main.mod.Logger.Error(e.ToString());
            }
        }
    }

}
