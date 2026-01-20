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
        public string serverUrl = "http://localhost:5000";
        public float pollingInterval = 2f;
        public int requestTimeout = 1000;
    }

    public AppConfig Config { get; private set; }

    private string ConfigFilePath => Path.Combine(Application.persistentDataPath, "config. json");

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
        // Se il file non esiste, crealo con valori di default
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
                Debug.Log($"Config caricato.  Server URL: {Config. serverUrl}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Errore nel caricamento config: {e.Message}");
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

    // Metodo per aggiornare l'URL a runtime
    public void SetServerUrl(string newUrl)
    {
        Config.serverUrl = newUrl;
        SaveConfig();
    }

    // Apre la cartella dove si trova il config (utile per l'utente)
    public void OpenConfigFolder()
    {
        Application.OpenURL("file://" + Application.persistentDataPath);
    }
}