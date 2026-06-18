using System.Collections.Generic;
using System.Linq;
using CrashoutCrew_APClient;
using HarmonyLib;
using Mono.Cecil;
using UnityEngine;
using Random = System.Random;

[HarmonyPatch(typeof(GameManager))]
public static class GameManager_Patches
{
    [HarmonyPatch(nameof(GameManager.GetContracts))]
    [HarmonyPostfix]
    public static void GetContracts(GameManager __instance, ref ContractObject[] __result)
    {
        if (GameUtil.isDemo)
        {
            __result = __instance.demoContracts;
            return;
        }
        var random = new Random(4);
        var allOrders = Resources.FindObjectsOfTypeAll<ShiftOrderObject>();
        // var allOrders = __instance.contracts.SelectMany(x => x.orders.ToHashSet()).ToHashSet().ToArray();

        var contracts2 = ArchipelagoHandler.Instance?.slotData?.ContractCountTwo ?? 5;
        var contracts3 = ArchipelagoHandler.Instance?.slotData?.ContractCountThree ?? 5;
        var contracts4 = ArchipelagoHandler.Instance?.slotData?.ContractCountFour ?? 5;
        __result =  ContractGenerator.GenerateContracts(random, allOrders, contracts2, contracts3, contracts4);
    }
}