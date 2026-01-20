using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Newtonsoft.Json;
using TMPro;

public class AudioGenClient : MonoBehaviour
{
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private string serverUrl = "http://localhost:8081";
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button generateButton;
    [SerializeField] private TMP_Text buttonText;

    [SerializeField] private GameObject audioPrefab;

    private bool isGenerating = false;

    [Serializable]
    private class GenerateRequest
    {
        public string text;
        public float duration;
    }

    [Serializable]
    private class GenerateResponse
    {
        public string job_id;
        public bool success;
        public string error;
        public FileInfo[] files;
    }

    [Serializable]
    private class FileInfo
    {
        public string type;
        public string download_url;
        public string filename;
    }

    private void Start()
    {
        if (generateButton != null)
        {
            generateButton.onClick.AddListener(() => GenerateAudio());

            if (buttonText == null)
                buttonText = generateButton.GetComponentInChildren<TMP_Text>();
        }

        SetButtonState(false);

        if (ConfigManager.Instance)
            serverUrl = ConfigManager.Instance.Config.audiogenServerUrl;
    }

    public void GenerateAudio(float duration = 5.0f)
    {
        if (isGenerating)
            return;

        string prompt = inputField.text;

        if (!string.IsNullOrEmpty(prompt))
        {
            StartCoroutine(GenerateCoroutine(prompt, duration));
        }
        else
        {
            Debug.Log("Input is not valid");
        }
    }

    private void SetButtonState(bool generating)
    {
        isGenerating = generating;

        if (generateButton != null)
            generateButton.interactable = !generating;

        if (buttonText != null)
            buttonText.text = generating ? "Generating..." : "Generate";
    }

    private IEnumerator GenerateCoroutine(string description, float duration)
    {
        SetButtonState(true);


        GenerateRequest request = new GenerateRequest { text = description, duration = duration };
        string json = JsonConvert.SerializeObject(request);
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest www = new UnityWebRequest(serverUrl + "/generate", "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(body);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.timeout = 180;

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Generation failed: {www.error}");
                SetButtonState(false);
                yield break;
            }

            GenerateResponse response = JsonConvert.DeserializeObject<GenerateResponse>(www.downloadHandler.text);

            if (!response.success)
            {
                Debug.LogError($"Generation failed: {response.error}");
                SetButtonState(false);
                yield break;
            }

            string job_id = response.job_id;

            string audioUrl = null;
            string filename = null;
            foreach (var file in response.files)
            {
                if (file.type == "audio")
                {
                    audioUrl = serverUrl + file.download_url;
                    filename = file.filename;
                    break;
                }
            }

            if (audioUrl == null)
            {
                Debug.LogError("No audio file found");
                SetButtonState(false);
                yield break;
            }

            using (UnityWebRequest downloadWww = UnityWebRequestMultimedia.GetAudioClip(audioUrl, AudioType.WAV))
            {
                yield return downloadWww.SendWebRequest();

                if (downloadWww.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Download failed: {downloadWww.error}");
                    SetButtonState(false);
                    yield break;
                }


                string folderPath = Path.Combine(Application.persistentDataPath, "GeneratedAudio");
                Directory.CreateDirectory(folderPath);

                string filePath = Path.Combine(folderPath, job_id);
                File.WriteAllBytes(filePath, downloadWww.downloadHandler.data);
                Debug.Log($"Saved: {filePath}");

                AudioClip clip = DownloadHandlerAudioClip.GetContent(downloadWww);
                if (clip != null)
                {
                    GameObject go = Instantiate(audioPrefab);

                    var info = go.AddComponent<GeneratedObjectInfo>();
                    info.type = GenerationType.Audio;
                    info.fileName = job_id;
                    info.isLooping = false;

                    AudioSource audioSource = go.GetComponent<AudioSource>();
                    if (audioSource == null)
                    {
                        audioSource = go.AddComponent<AudioSource>();
                    }

                    audioSource.clip = clip;
                    audioSource.spatialBlend = 1f;
                    audioSource.minDistance = 1f;
                    audioSource.maxDistance = 20f;
                    //audioSource.Play();

                    if (levelManager)
                    {
                        levelManager.AddSoundToList(go);
                        LookAtCamera lookAtCamera = go.GetComponent<LookAtCamera>();
                        lookAtCamera.Cam(levelManager.GetFreeCam());
                    }
                }
            }

            inputField.text = "";
        }

        SetButtonState(false);
    }
}