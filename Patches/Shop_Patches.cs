using System;
using Aggro.Core;
using HarmonyLib;
using Mirror;
using UnityEngine;

namespace CrashoutCrew_APClient.Patches;

[HarmonyPatch(typeof(Shop))]
public static class Shop_Patches
{
    [HarmonyPatch(nameof(Shop.ServerGenerateShopStock))]
    public static bool Prefix(Shop __instance)
    {
        if (!NetworkServer.active) 
            return false;
        Shop._holders.Clear();
        __instance.entity.GetObjects<ShopHolder>(Shop._holders);
        ++__instance._serverStockGenCount;
        var random =
            MathUtil.GetRandom(Hash.Calculate(__instance._serverSeed, __instance._serverStockGenCount));
        Shop._holders.Randomize<ShopHolder>(random.NextInt());
        Shop._current.Clear();
        Shop._required.Clear();
        Shop._saleCandidates.Clear();
        for (var index1 = 0; index1 < Shop._holders.Count; ++index1)
        {
            ShopItemObject? key = null;
            if (Shop._required.Count == 0)
            {
                var shuffleGeneration1 = __instance._serverDeck.shuffleGeneration;
                while (__instance._serverDeck.cardCount > 0)
                {
                    if (__instance._serverDeck.shuffleGeneration >= shuffleGeneration1 + 2)
                    {
                        Debug.LogWarning("[SHOP] Could not find an item from inventory!",
                            __instance.inventory);
                        break;
                    }

                    var shopCard = __instance._serverDeck.DrawCard();
                    ShopItemObject? shopItemObject = null;
                    switch (shopCard.type)
                    {
                        case Shop.ItemType.Object:
                            if (__instance.CanAddShopItem(shopCard.obj, index1))
                            {
                                shopItemObject = shopCard.obj;
                                break;
                            }

                            continue;
                        case Shop.ItemType.Random:
                            var shuffleGeneration2 = __instance._serverRandomDeck.shuffleGeneration;
                            while (__instance._serverRandomDeck.cardCount > 0)
                            {
                                if (__instance._serverRandomDeck.shuffleGeneration >= shuffleGeneration2 + 2)
                                {
                                    Debug.LogWarning("[SHOP] Could not find an item from random inventory!",
                                        __instance.inventory);
                                    break;
                                }

                                var candidate = __instance._serverRandomDeck.DrawCard();
                                if (!__instance.CanAddShopItem(candidate, index1)) 
                                    continue;
                                shopItemObject = candidate;
                                break;
                            }

                            break;
                        default:
                            throw new InvalidEnumException();
                    }

                    if (shopItemObject != null && shopItemObject.hasRequiredNumberInShop)
                    {
                        for (int index2 = 0; index2 < shopItemObject.requiredNumberInShop - 1; ++index2)
                            Shop._required.Enqueue(shopItemObject);
                    }

                    key = shopItemObject;
                    break;
                }
            }
            else
                key = Shop._required.Dequeue();

            if (key == null)
                return false;
            Shop._current.TryGetValue(key, out var num);
            Shop._current[key] = num + 1;
            Shop._holders[index1].ServerSetItem(key, __instance.ServerItemPurchased);
            if (key.type == ShopItemType.Station)
                Shop._saleCandidates.Add(Shop._holders[index1]);
        }

        if (Shop._saleCandidates.Count <= 0)
            return false;
        Shop._saleCandidates[random.NextInt(0, Shop._saleCandidates.Count)]
            .ServerSetOnSale();
        return false;
    }
}