using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Aggro.Core;
using UnityEngine;
using VeryVeryValetAPClient;

namespace CrashoutCrew_APClient;

using System.IO;
using Newtonsoft.Json;

public class CustomSaveData
{
    public int ItemIndex;
    public List<string> ShopUnlocks = new();
}

public class SaveDataHandler
{
    public CustomSaveData APSaveData = null!;
    private string _customDataPath = "";

    public void GetSave()
    {
        _customDataPath =
            $"{Application.persistentDataPath}/saves/Archipelago/{ArchipelagoHandler.Instance.Slot}{ArchipelagoHandler.Instance.Seed}.json";
        if (File.Exists(_customDataPath))
        {
            var data = JsonConvert.DeserializeObject<CustomSaveData>(File.ReadAllText(_customDataPath));
            APSaveData = data ?? new CustomSaveData();
        }
        else
            APSaveData = new CustomSaveData();

        SaveGame();
    }

    private IEnumerator InitializeSaveCo()
    {
        if (SaveManager.isInitialized)
            yield break;
        if (SaveManager.DoesGameExist(0))
        {
            var task = SaveManager.InitializeLoadGameAsync(0);
            yield return new WaitForTask(task);
            if (task.Result != LoadResult.Loaded)
            {
                var doContinue = false;
                var doQuit = false;
                AggroInputManager.Enable();
                SaveError error;
                string info;
                switch (task.Result)
                {
                    case LoadResult.UsedBackup:
                        error = SaveError.UseBackup;
                        info = new DateTime(SaveManager.data.timeStampTicks).ToString("g",
                            CultureInfo.CurrentCulture);
                        break;
                    case LoadResult.Failed:
                        error = SaveError.SaveFailed;
                        info = "";
                        break;
                    default:
                        throw new InvalidEnumException();
                }

                PlayerMessageManager.QueueErrorMessage(error, info, (Action)(() => doContinue = true),
                    (Action)(() => doQuit = true));
                while (!doContinue && !doQuit)
                {
                    PlayerMessageManager.ProcessQueuedMessages();
                    yield return null;
                }

                AggroInputManager.Disable();
                if (doQuit)
                {
                    Application.Quit();
                    yield break;
                }

                if (doContinue)
                {
                    switch (task.Result)
                    {
                        case LoadResult.UsedBackup:
                            yield return new WaitForTask(SaveManager.DeletePrimarySaveAsync(0));
                            break;
                        case LoadResult.Failed:
                            yield return new WaitForTask(SaveManager.DeleteGameAsync(0));
                            SaveManager.InitializeNewGame(0);
                            break;
                        default:
                            throw new InvalidEnumException();
                    }

                    yield return new WaitForTask(SaveManager.SaveGameAsync());
                }
            }

            if (SaveManager.data.GetSaveVersion() != 1)
            {
                SaveManager.Uninitialize();
                SaveManager.InitializeNewGame(0);
                yield return new WaitForTask(SaveManager.SaveGameAsync());
            }
            task = null;
        }
        else
        {
            SaveManager.InitializeNewGame(0);
            yield return new WaitForTask(SaveManager.SaveGameAsync());
        }

        foreach (CostumeObject costume in GlobalScriptableObject<CosmeticGlobalData>.instance.costumes)
        {
            if (costume != null && costume.startsUnlocked)
                SaveManager.data.UnlockCostume(costume);
        }
    }

    public void SaveGame()
    {
        if (_customDataPath == "")
        {
            APConsole.Instance.DebugLog(
                $"Failed to save game! SaveDataPath: CustomDataPath: {_customDataPath}");
            return;
        }
        File.WriteAllText(_customDataPath, JsonConvert.SerializeObject(APSaveData));
        SaveManager.SaveGameAsync();
    }
}