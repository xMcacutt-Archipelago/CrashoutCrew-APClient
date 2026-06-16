using System;
using Aggro.Core;
using HarmonyLib;
using UnityEngine;

namespace CrashoutCrew_APClient.Patches;

// TODO maybe transpile
[HarmonyPatch(typeof(ContractUI))]
public static class ContractUI_Patches
{
    [HarmonyPatch(nameof(ContractUI.SetUp))]
    public static bool Prefix(ContractUI __instance,
        string title,
        int highBellScore,
        ContractScore highContractScore,
        bool locked,
        bool isDemoLocked,
        int bellsRequired,
        ContractObject.Unlock[] unlocks,
        int contractNum,
        TimeSpan contractTime)
    {
        if (isDemoLocked)
            locked = true;
        __instance.lockedContainer.SetActive(locked);
        __instance.unlockedContainer.SetActive(!locked);
        __instance.demoLockedContainer.SetActive(isDemoLocked);
        __instance.bellsRequiredContainer.SetActive(!isDemoLocked);
        __instance.contractNumText.text = contractNum.ToString() ?? "";
        __instance.contractNumText.enabled = !locked;
        if (locked)
        {
            __instance.bellsRequiredText.text = bellsRequired.ToString();
            __instance.localizedText.gameObject.SetActive(false);
            __instance.lockedText.SetActive(true);
        }
        else
        {
            __instance.localizedText.gameObject.SetActive(true);
            __instance.localizedText.tmp.text = title;
            __instance.lockedText.SetActive(false);
            for (var index = 0; index < __instance.bells.Length; ++index)
                __instance.bells[index].SetActive(index < highBellScore);
            for (var index = 0; index < __instance.scoreBubbles.Length; ++index)
                __instance.scoreBubbles[index].scoreBubble.SetActive(false);
            __instance._unlocks = unlocks;
            if (__instance._unlocks != null)
            {
                foreach (ContractObject.Unlock unlock in __instance._unlocks)
                    __instance._costumeUnlockUIs.Add(UnityEngine.Object
                        .Instantiate<GameObject>(__instance.costumeUnlockPrefab, __instance.cosmeticUnlockContainer)
                        .GetComponent<CostumeUnlockUI>());
            }
            __instance.finalGradeContainer.gameObject.SetActive(highBellScore > 0);
            __instance.finalGradeImage.sprite =
                __instance.gradeSprites[highBellScore == 5 ? (int)(highContractScore + (byte)1) : 0];
            __instance.finalGradeDillyImage.sprite =
                __instance.gradeDillySprites[highBellScore == 5 ? (int)(highContractScore + (byte)1) : 0];
            __instance.finalGradeImage.color =
                GlobalScriptableObject<AggroSettingsObject>.instance.gradeColors[
                    highBellScore == 5 ? (int)(highContractScore + (byte)1) : 0];
            __instance.finalGradeDillyImage.color =
                GlobalScriptableObject<AggroSettingsObject>.instance.gradeColors[
                    highBellScore == 5 ? (int)(highContractScore + (byte)1) : 0];
            if (highBellScore >= 5 && contractTime != TimeSpan.Zero)
                __instance.bestTimeText.text = contractTime.ToString("mm\\:ss\\:ff");
            else
                __instance.bestTimeText.text = "--:--:--";
        }
        return false;
    }
}