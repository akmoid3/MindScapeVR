using System;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

public class ConfigManager : MonoBehaviour
{
    public static ConfigManager Instance { get; private set; }

    [Serializable]
    public class AppConfig
    {
        public string hunyuanWorldServerUrl = "http://localhost:9191";
        public string hunyuan3DServerUrl = "http://localhost:9292";
        public string audiogenServerUrl = "http://localhost:9393";
        
    }

    public AppConfig Config { get; private set; }

    private string ConfigFilePath => Path.Combine(Application.persistentDataPath, "config.json");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadConfig();
    }

    private void LoadConfig()
    {
        if (!File. Exists(ConfigFilePath))
        {
            Config = new AppConfig();
            SaveConfig();
            Debug.Log($"Config creato in: {ConfigFilePath}");
        }
        else
        {
            try
            {
                string json = File. ReadAllText(ConfigFilePath);
                Config = JsonConvert.DeserializeObject<AppConfig>(json);
            }
            catch (Exception e)
            {
                Config = new AppConfig();
            }
        }
    }

    public void SaveConfig()
    {
        try
        {
            string json = JsonConvert.SerializeObject(Config, Formatting.Indented);
            File.WriteAllText(ConfigFilePath, json);
            Debug.Log("Config salvato.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Errore nel salvataggio config:  {e.Message}");
        }
    }


    public void OpenConfigFolder()
    {
        Application.OpenURL("file://" + Application.persistentDataPath);
    }
}