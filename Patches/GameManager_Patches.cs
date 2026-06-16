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
        var random = new Random(2);
        var allOrders = Resources.FindObjectsOfTypeAll<ShiftOrderObject>();
        // var allOrders = __instance.contracts.SelectMany(x => x.orders.ToHashSet()).ToHashSet().ToArray();
        var twoOrderContracts = __instance.contracts
            .Where(x => x.orders.Length == 2 && x.type == ContractType.Explicit)
            .OrderBy(_ => random.Next()).Take(ArchipelagoHandler.Instance?.slotData?.ContractCountTwo ?? 4);
        var threeOrderContracts = __instance.contracts.Skip(1)
            .Where(x => x.orders.Length == 3 && x.type == ContractType.Explicit)
            .OrderBy(_ => random.Next()).Take(ArchipelagoHandler.Instance?.slotData?.ContractCountThree ?? 12);
        var fourOrderContracts = __instance.contracts
            .Where(x => x.orders.Length == 4 && x.type == ContractType.Explicit)
            .OrderBy(_ => random.Next()).Take(ArchipelagoHandler.Instance?.slotData?.ContractCountFour ?? 4);
        var contracts = new List<ContractObject>();
        contracts.AddRange(twoOrderContracts);
        contracts.AddRange(threeOrderContracts);
        contracts.AddRange(fourOrderContracts);
        var randomizedContracts = new List<ContractObject>();
        foreach (var contract in contracts)
        {
            do
            {
                RandomizeContract(random, allOrders, contract);
            } while (randomizedContracts.Any(existingContract =>
                     {
                         var existing = existingContract.orders.Select(order => order.name).ToHashSet();
                         var dupes = contract.orders.Count(order => !existing.Add(order.name));
                         return dupes > 1;
                     }));
            randomizedContracts.Add(contract);
        }
        __result = randomizedContracts.ToArray();
    }

    private static void RandomizeContract(Random random, ShiftOrderObject[] allOrders, ContractObject contract)
    {
        var originalOrders = contract.orders.ToArray();
        var newOrders = allOrders.OrderBy(x => random.Next()).Take(contract.orders.Length).ToArray();
        var replacements = originalOrders
            .Zip(newOrders, (oldOrder, newOrder) => new { oldOrder, newOrder })
            .ToDictionary(x => x.oldOrder, x => x.newOrder);
        contract.orders = newOrders;
        contract.title = contract.orders.GenerateName(random);
        contract.bellsRequired = 0;
        foreach (var order in contract.shift1.orders)
            order.order = replacements.TryGetValue(order.order, out var replacement) ? replacement : order.order;
        foreach (var order in contract.shift2.orders)
            order.order = replacements.TryGetValue(order.order, out var replacement) ? replacement : order.order;
        foreach (var order in contract.shift3.orders)
            order.order = replacements.TryGetValue(order.order, out var replacement) ? replacement : order.order;
        foreach (var order in contract.shift4.orders)
            order.order = replacements.TryGetValue(order.order, out var replacement) ? replacement : order.order;
        foreach (var order in contract.shift5.orders)
            order.order = replacements.TryGetValue(order.order, out var replacement) ? replacement : order.order;
    }
}