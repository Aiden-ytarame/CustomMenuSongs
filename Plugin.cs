using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Networking;

namespace CustomMenuSongs;

[BepInPlugin(Guid, Name, Version)]
[BepInProcess("Project Arrhythmia.exe")]

public class Plugin : BaseUnityPlugin
{
    public static List<AudioClip> CustomSongs = new();
    public static ConfigEntry<int> SongChoice;
    internal new static ManualLogSource Logger;
    
    Harmony _harmony;
    const string Guid = "me.ytarame.CustomMenuSong";
    const string Name = "CustomMenuSong";
    const string Version = "1.0.0";

    private void Awake()
    {
        Logger = base.Logger;
        
        string songsPath = Paths.PluginPath + "\\CustomMenuSongs";
        
        if (!Directory.Exists(songsPath))
        {
            Directory.CreateDirectory(songsPath);
        }
        
        string[] oggPaths = Directory.GetFiles(songsPath, "*.ogg");
        string[] mp3Paths = Directory.GetFiles(songsPath, "*.mp3");
        string[] wavPaths = Directory.GetFiles(songsPath, "*.wav");
        
        ParseAudioFiles(oggPaths, AudioType.OGGVORBIS);
        ParseAudioFiles(mp3Paths, AudioType.MPEG);
        ParseAudioFiles(wavPaths, AudioType.WAV);

        
        SongChoice = Config.Bind("Music", "MenuMusicMusicChoice", 0,
            "which song is selected to play in the menu. 0 is default, 1 is shuffle, 2 is custom songs only shuffle, 3+ is a specific custom song");
        
        List<string> songNames = new List<string>(["Default", "Shuffle", "Custom Shuffle"]);
        foreach (var customSong in CustomSongs)
        {
            songNames.Add(customSong.name);
        }
        
        songNames.Add("Corrupted Menu");
        
        string[] names = songNames.ToArray();
        
        
        PaApi.SettingsHelper.RegisterModSettings(Guid, "Menu Song", null, Config, builder =>
        {
            builder.Spacer();
            builder.Slider("Song Choice", SongChoice.Value, 1, x =>
            {
                SongChoice.Value = (int)x;
                ChooseSong();
                
            }, UI_Slider.VisualType.dot, names);
        });
        
        _harmony = new Harmony(Guid);
        _harmony.PatchAll();
        
        // Plugin startup logic
        Logger.LogInfo($"Plugin {Guid} is loaded!");
    }


    private void ParseAudioFiles(string[] audioPaths, AudioType audioType)
    {
        foreach (var audioPath in audioPaths)
        {
            UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(audioPath, audioType);
            www.SendWebRequest();
            
            while (!www.isDone) Task.Delay(5).Wait();

            if (www.result != UnityWebRequest.Result.Success)
            {
                continue;
            }
            
            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
            clip.name = Path.GetFileNameWithoutExtension(audioPath);
            CustomSongs.Add(clip);
        }
    }

    internal static bool ChooseSong()
    {
        int choice = SongChoice.Value;
        
        switch (choice)
        {
            case <= 0:
                return false;
            case 1:
                AudioManager.Inst.PlayMusic("MenuShuffle");
                return true;
            case 2:
                AudioManager.Inst.PlayMusic("CustomMenu");
                return true;
            default:
                int final = choice - 3;
                if (final >= CustomSongs.Count)
                {
                    return false;
                }
                
                AudioManager.Inst.PlayMusic(CustomSongs[final]);
                return true;
        }
    }
}
