using UnityEngine;

public static class GameSettings
{
    const string VolumeKey = "MasterVolume";
    const string FullscreenKey = "Fullscreen";

    public static float Volume
    {
        get => PlayerPrefs.GetFloat(VolumeKey, 1f);
        set
        {
            PlayerPrefs.SetFloat(VolumeKey, value);
            AudioListener.volume = value;
        }
    }

    public static bool Fullscreen
    {
        get => PlayerPrefs.GetInt(FullscreenKey, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
            Screen.fullScreen = value; // no effect in the Editor, works in builds
        }
    }

    // Add more settings the same way, e.g. a turn speed the robot controller reads:
    // public static float TurnSpeed { get => PlayerPrefs.GetFloat("TurnSpeed", 1f); set => PlayerPrefs.SetFloat("TurnSpeed", value); }

    public static void Save() => PlayerPrefs.Save();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void ApplyOnStartup()
    {
        AudioListener.volume = Volume;
        Screen.fullScreen = Fullscreen;
    }
}