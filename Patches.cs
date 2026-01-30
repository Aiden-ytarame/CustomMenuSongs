using System.Collections.Generic;
using HarmonyLib;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Localization;

namespace CustomMenuSongs;

[HarmonyPatch(typeof(DataManager))]
public static class DataManagerPatch
{
    [HarmonyPatch(nameof(DataManager.OnEnable))]
    [HarmonyPostfix]
    static void PostStart(DataManager __instance)
    {
        if (Plugin.CustomSongs.Count == 0) return;

        JSONNode shuffle = JSONNode.Parse("{ name : Custom  Shuffle,values : Custom_Shuffle}");
        shuffle["name"] = "Custom Shuffle";
        DataManager.inst.interfaceSettings["MenuMusic"].Add(shuffle);
        
        foreach (var customSong in Plugin.CustomSongs)
        {
            JSONNode node = JSONNode.Parse("{ name : val,values : val}");
            node["name"] = customSong.name;
            node["values"] = customSong.name;

            DataManager.inst.interfaceSettings["MenuMusic"].Add(node);
        }
    }
}


[HarmonyPatch(typeof(AudioManager))]
public static class AudioManagerPatch
{
    private static bool _hasInitialized;
    
    [HarmonyPatch(nameof(AudioManager.OnAwake))]
    [HarmonyPostfix]
    static void PostStart(AudioManager __instance)
    {
        if (_hasInitialized) return;
        _hasInitialized = true;
        
        Plugin.CustomSongs.AddRange(__instance.library.musicClips["menu"].music);
   
        List<AudioClip> songs = new(__instance.library.musicClips["arcade_dream"].music);
        songs.AddRange(Plugin.CustomSongs);
   
        __instance.library.musicClips.Add(
            "MenuShuffle", 
            new SoundLibrary.MusicGroup(){music = songs.ToArray(), AlwaysRandom = true});
        
        __instance.library.musicClips.Add(
            "CustomMenu", 
            new SoundLibrary.MusicGroup(){music = Plugin.CustomSongs.ToArray(), AlwaysRandom = true});
        
        foreach (var customSong in Plugin.CustomSongs)
        {
              __instance.library.musicClips.Add(
                customSong.name, 
                new SoundLibrary.MusicGroup(){music = [customSong] });
        }

      
       
        //UIEventManager.Inst.ExecUIEvent("apply_menu_music");
    }

    
    [HarmonyPatch(nameof(AudioManager.PlayMusic), typeof(string), typeof(bool))]
    [HarmonyPrefix]
    static bool PrePlayMusic(AudioManager __instance, string _musicName)
    {
        if (__instance.currentSongGroup == "corruption" && _musicName == "arcade_dream")
        {
            __instance.currentSongGroup = "";
            return !Plugin.ChooseSong();
        }
        
        return true;
    }
}

[HarmonyPatch(typeof(UIElement))]
internal static class SplashScreenPatch
{
    [HarmonyPatch(nameof(UIElement.ExecUIEvent))]
    [HarmonyPrefix]
    internal static bool ChooseSong(string _func)
    {
        if (_func != "apply_menu_music")
        {
            return true;
        }
      
        if (AudioManager.Inst.currentSongGroup == "menu_intro")
        {
            return false;
        }

        return !Plugin.ChooseSong();
    }
}