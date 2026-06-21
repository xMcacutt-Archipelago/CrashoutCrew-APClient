using System.Collections;
using System.Linq;
using Aggro.Core;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrashoutCrew_APClient.Patches;

[HarmonyPatch(typeof(Title))]
public class Title_Patches
{
    [HarmonyPatch(nameof(Title.Start))]
    [HarmonyPrefix]
    public static void Start(Title __instance)
    {
        var connectButton = __instance.releaseButtons[0].GetComponent<Button>();
        var hostButton = __instance.releaseButtons[1].gameObject;
        
        var Canvas = connectButton.transform.parent.parent;
        
        var settingsPanel = GameObject.Find("settings-ui");
        var loginPanel = Object.Instantiate(settingsPanel, Canvas, true);
        
        __instance.releaseButtons = __instance.releaseButtons.Where(x => x.name != "HostGame").ToArray();
        hostButton.SetActive(false);
        
        Object.Destroy(connectButton.GetComponentInChildren<LocalizedText>(true));
        
        var tmp = connectButton.GetComponentInChildren<TextMeshProUGUI>(true);
        tmp.text = "CONNECT";
        
        connectButton.onClick = new Button.ButtonClickedEvent();
        connectButton.onClick.AddListener(() =>
        {
            Debug.Log("CONNECT");
            loginPanel.GetComponent<SettingsUI>().OpenSettings();
        });
    }
}