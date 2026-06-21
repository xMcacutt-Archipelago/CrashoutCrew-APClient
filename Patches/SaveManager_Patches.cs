using Aggro.Core;
using HarmonyLib;
using UnityEngine;

namespace CrashoutCrew_APClient.Patches;

[HarmonyPatch(typeof(SaveManager))]
public class SaveManager_Patches
{
    [HarmonyPatch(nameof(SaveManager.GetFilePath))]
    [HarmonyPrefix]
    public static bool GetFilePath(ref string __result)
    {
        __result = $"{Application.persistentDataPath}/saves/Archipelago.sav";
        return false;
    }
    
    
    [HarmonyPatch(nameof(SaveManager.GetBackupFilePath))]
    [HarmonyPrefix]
    public static bool GetBackupFilePath(ref string __result)
    {
        __result = $"{Application.persistentDataPath}/saves/Archipelago_backup.sav";
        return false;
    }
}