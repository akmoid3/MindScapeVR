using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Newtonsoft.Json;
using TMPro;

public class Hunyuan3DClient : MonoBehaviour
{
    [SerializeField] private string serverUrl = "http://localhost:5000";
    [SerializeField] private Transform modelParent;
    [SerializeField] private float modelScale = 0.5f;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button generateButton;
    [SerializeField] private TMP_Text buttonText;

    private bool isGenerating = false;

    [Serializable]
    private class GenerateRequest
    {
        public string prompt;
        public int steps;
        public bool generate_texture;
    }

    [Serializable]
    private class GenerateResponse
    {
        public bool success;
        public FileInfo[] files;
    }

    [Serializable]
    private class FileInfo
    {
        public string filename;
        public string type;
        public bool textured;
        public string download_url;
    }

    private void Start()
    {
        if (generateButton != null)
        {
            generateButton.onClick.AddListener(() => GenerateModel());

            if (buttonText == null)
                buttonText = generateButton.GetComponentInChildren<TMP_Text>();
        }

        SetButtonState(false);
    }

    public void GenerateModel()
    {
        if (isGenerating)
            return;

        if (!string.IsNullOrEmpty(inputField.text))
        {
            StartCoroutine(GenerateCoroutine());
        }
        else
        {
            Debug.Log("Input not valid");
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

    private IEnumerator GenerateCoroutine()
    {
        SetButtonState(true);

        GenerateRequest request = new GenerateRequest { prompt = inputField.text, steps = 30, generate_texture = true };
        string json = JsonConvert.SerializeObject(request);
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest www = new UnityWebRequest(serverUrl + "/generate", "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(body);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.timeout = 300;

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Generation failed: {www.error}");
                SetButtonState(false);
                yield break;
            }

            GenerateResponse response = JsonConvert.DeserializeObject<GenerateResponse>(www.downloadHandler.text);

            FileInfo meshFile = null;
            foreach (var file in response.files)
            {
                if (file.type == "mesh")
                {
                    meshFile = file;
                    break;
                }
            }

            if (meshFile == null)
            {
                Debug.LogError("No mesh found");
                SetButtonState(false);
                yield break;
            }

            string downloadUrl = serverUrl + meshFile.download_url;
            using (UnityWebRequest downloadWww = UnityWebRequest.Get(downloadUrl))
            {
                yield return downloadWww.SendWebRequest();

                if (downloadWww.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Download failed: {downloadWww.error}");
                    SetButtonState(false);
                    yield break;
                }

                string folderPath = Path.Combine(Application.persistentDataPath, "Models");
                Directory.CreateDirectory(folderPath);
                string filePath = Path.Combine(folderPath, meshFile.filename);
                File.WriteAllBytes(filePath, downloadWww.downloadHandler.data);

                GameObject container = new GameObject($"Model_{meshFile.filename}");
                container.AddComponent<BoxCollider>();
                
                int selectableLayer = LayerMask.NameToLayer("SelectableObjects");
                if (selectableLayer == -1)
                {
                    Debug.LogError("Layer 'SelectableObjects' doesnt exists.");
                }
                else
                {
                    container.layer = selectableLayer;
                }
                if (modelParent != null)
                    container.transform.SetParent(modelParent);
                container.transform.localScale = Vector3.one * modelScale;

                var gltf = container.AddComponent<GLTFast.GltfAsset>();
                gltf.Url = "file://" + filePath;
                
                Debug.Log($"Model loaded from: {filePath}");
            }
        }

        inputField.text = "";
        SetButtonState(false);
    }
}