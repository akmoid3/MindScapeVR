using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Newtonsoft.Json;
using TMPro;
using GLTFast;

public class HunyuanWorldClient : MonoBehaviour
{
    [Header("Server Settings")]
    [SerializeField] private string serverUrl = "http://localhost:5000";

    [Header("UI References")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button generateButton;
    [SerializeField] private TMP_Text buttonText;

    [Header("Scene Settings")]
    [SerializeField] private Material skyboxMaterial;
    [SerializeField] private Material sceneMaterial;

    [Header("Generation Parameters")]
    [SerializeField] private string negativePrompt = "";
    [SerializeField] private string sceneClass = "outdoor";
    [SerializeField] private int seed = 42;

    private bool isGenerating = false;
    private Material originalSkybox;
    private string currentButtonState = "Generate";

    [SerializeField] private GameObject sceneObj;
    [SerializeField] private GameObject planeObj;
    [SerializeField] private GameObject deleteSceneButton;

    [Serializable]
    private class GenerateRequest
    {
        public string prompt;
        public string negative_prompt;
        public string classes;
        public int seed;
    }

    [Serializable]
    private class GenerateResponse
    {
        public bool success;
        public string job_id;
    }

    private void Start()
    {
        originalSkybox = RenderSettings.skybox;

        if (skyboxMaterial == null)
        {
            skyboxMaterial = new Material(Shader.Find("Skybox/Panoramic"));
            skyboxMaterial.SetFloat("_Exposure", 1.0f);
            skyboxMaterial.SetFloat("_Rotation", 0f);
        }

        if (generateButton != null)
        {
            generateButton.onClick.AddListener(() => GenerateScene());

            if (buttonText == null)
                buttonText = generateButton.GetComponentInChildren<TMP_Text>();
        }

        SetButtonState(false);
    }

    private void Update()
    {
        if (deleteSceneButton != null)
        {
            if (sceneObj != null && ! deleteSceneButton.activeSelf)
            {
                deleteSceneButton.SetActive(true);
                planeObj. SetActive(false);
            }
            else if (sceneObj == null && deleteSceneButton.activeSelf)
            {
                deleteSceneButton.SetActive(false);
                planeObj.SetActive(true);
            }
        }
    }

    public void GenerateScene()
    {
        if (isGenerating)
            return;

        if (! string.IsNullOrEmpty(inputField.text))
        {
            StartCoroutine(GenerateSceneCoroutine());
        }
        else
        {
            Debug.Log("Prompt is not valid");
        }
    }

    private void SetButtonState(bool generating)
    {
        isGenerating = generating;

        if (generateButton != null)
            generateButton.interactable = ! generating;

        if (buttonText != null)
            buttonText.text = generating ? "Generating..." : "Generate";
    }

    private void SetButtonState(string state)
    {
        currentButtonState = state;

        if (buttonText != null)
            buttonText.text = state;

        if (generateButton != null)
            generateButton.interactable = (state == "Generate");

        isGenerating = (state != "Generate");
    }

    private IEnumerator GenerateSceneCoroutine()
    {
        SetButtonState("Generating Scene...");

        GenerateRequest request = new GenerateRequest
        {
            prompt = inputField.text,
            negative_prompt = negativePrompt,
            classes = sceneClass,
            seed = seed
        };

        string json = JsonConvert.SerializeObject(request);
        byte[] body = System.Text.Encoding. UTF8.GetBytes(json);

        using (UnityWebRequest www = new UnityWebRequest(serverUrl + "/generate_scene", "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(body);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.timeout = 600;

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug. LogError($"Generation failed: {www.error}");
                SetButtonState(false);
                yield break;
            }

            GenerateResponse response = JsonConvert. DeserializeObject<GenerateResponse>(www.downloadHandler.text);
            string jobId = response.job_id;

            Debug.Log($"Scene generation started. Job ID: {jobId}");

            yield return StartCoroutine(DownloadAndLoadSkybox(jobId));

            yield return StartCoroutine(DownloadAndLoadModel(jobId));
        }

        inputField.text = "";
        SetButtonState(false);
    }

    private IEnumerator DownloadAndLoadSkybox(string jobId)
    {
        SetButtonState("Loading Skybox.. .");
        string skyboxUrl = $"{serverUrl}/api/file/{jobId}/sky_image_sr.png";
        Debug.Log($"Downloading skybox.. .");

        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(skyboxUrl))
        {
            www.timeout = 400;
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug. LogError($"Skybox download failed: {www.error}");
                yield break;
            }

            Texture2D skyboxTexture = DownloadHandlerTexture.GetContent(www);

            if (skyboxMaterial != null)
            {
                skyboxMaterial.SetTexture("_MainTex", skyboxTexture);
                RenderSettings.skybox = skyboxMaterial;
                DynamicGI.UpdateEnvironment();
                Debug.Log("Skybox applied successfully!");
            }
        }
    }

    private IEnumerator DownloadAndLoadModel(string jobId)
    {
        
        string modelUrl = $"{serverUrl}/api/file/{jobId}/mesh_layer0.glb";
        Debug.Log($"Downloading model...");

        using (UnityWebRequest www = UnityWebRequest.Get(modelUrl))
        {
            www.timeout = 400;

            var operation = www.SendWebRequest();
            while (!operation.isDone)
            {
                Debug.Log($"Download progress: {www.downloadProgress * 100:F1}%");
                SetButtonState($"Loading Model: {www.downloadProgress * 100:F1}%");
                yield return new WaitForSeconds(0.5f);
            }

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Model download failed: {www.error}");
                SetButtonState($"Model load failed: {www.error}");
                yield break;
            }

            string folderPath = Path.Combine(Application.persistentDataPath, "Scenes");
            Directory. CreateDirectory(folderPath);
            string filePath = Path. Combine(folderPath, $"{jobId}_mesh.glb");
            File.WriteAllBytes(filePath, www.downloadHandler.data);

            GameObject container = new GameObject($"Scene_{jobId}");

            container.transform.position = Vector3.zero;
            container.transform.rotation = Quaternion.Euler(270f, 0f, 0f);
            container.transform. localScale = new Vector3(15f, 15f, 15f);

            GltfAsset gltfAsset = container.AddComponent<GltfAsset>();

            var task = gltfAsset.Load(filePath);
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.Result)
            {
                Debug.Log($"Scene loaded successfully from: {filePath}");

                if (sceneMaterial != null)
                {
                    ApplyMaterialToScene(container);
                }

                sceneObj = container;
            }
            else
            {
                Debug.LogError($"Failed to load scene");
                Destroy(container);
            }
        }
    }

    private void ApplyMaterialToScene(GameObject sceneRoot)
    {
        MeshRenderer[] renderers = sceneRoot.GetComponentsInChildren<MeshRenderer>();

        foreach (MeshRenderer renderer in renderers)
        {
            Material[] materials = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = sceneMaterial;
            }
            renderer.sharedMaterials = materials;
        }

        Debug.Log($"Applied custom material to {renderers.Length} renderers");
    }

    public void RestoreOriginalSkybox()
    {
        if (originalSkybox != null)
        {
            RenderSettings.skybox = originalSkybox;
            DynamicGI.UpdateEnvironment();
            Debug.Log("Original skybox restored");
        }
    }

    public void DestroyScene()
    {
        if (sceneObj != null)
        {
            DestroyImmediate(sceneObj);
            sceneObj = null;
        }
    }
}