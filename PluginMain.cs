using System;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using TemplatePlugin;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Windows;

namespace CrashoutCrew_APClient
{
    [BepInPlugin(TemplatePluginInfo.PLUGIN_GUID, TemplatePluginInfo.PLUGIN_NAME, Version)]
    public class PluginMain : BaseUnityPlugin
    {
        public static ConfigEntry<bool>? EnableDebugLogging;
        public static ConfigEntry<bool>? FilterLog;
        public static ConfigEntry<float>? MessageInTime;
        public static ConfigEntry<float>? MessageHoldTime;
        public static ConfigEntry<float>? MessageOutTime;
        public const string GameName = TemplatePluginInfo.GAME_NAME;
        private const string Version = "1.0.0";

        private readonly Harmony _harmony = new Harmony(TemplatePluginInfo.PLUGIN_GUID);
        public static ManualLogSource? logger;
        public static SaveDataHandler SaveDataHandler = new();

        void Awake()
        {
            logger = Logger;
            _harmony.PatchAll();
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += (scene, mode) => { };
            
            EnableDebugLogging = Config.Bind(
                "Logging",
                "EnableDebugLogging",
                false,
                "Enables or disables debug logging in the Archipelago Console."
            );
            
            FilterLog = Config.Bind(
                "Logging",
                "FilterLog",
                false,
                "Filter the archipelago log to only show messages relevant to you."
            );
            
            MessageInTime = Config.Bind(
                "Logging",
                "MessageInTime",
                0.25f,
                "How long messages take to animate in."
            );
            
            MessageHoldTime = Config.Bind(
                "Logging",
                "MessageHoldTime",
                3f,
                "How long messages stay in the log before animating out."
            );
            
            MessageOutTime = Config.Bind(
                "Logging",
                "MessageOutTime",
                0.5f,
                "How long messages take to animate out."
            );
        }
    }
}