using GLTFast;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class SceneGenerationRequest
{
    public string prompt;
    public string negative_prompt = "";
    public List<string> labels_fg1 = new List<string>();
    public List<string> labels_fg2 = new List<string>();
    public string classes = "outdoor";
    public int seed = 42;
    public bool export_drc = false;
}

[System.Serializable]
public class GLBFile
{
    public string filename;
    public string download_url;
    public int size;
}

[System.Serializable]
public class SceneGenerationResponse
{
    public bool success;
    public string job_id;
    public string download_url;
    public List<GLBFile> glb_files;
}

public class HunyuanWorldClient : MonoBehaviour
{
    [Header("Server Settings")]
    [SerializeField] private string serverUrl = "http://localhost:5000";
    [SerializeField] private int downloadTimeout = 300; // 5 minutes for large files

    [Header("Generation Settings")]
    [SerializeField] private string prompt = "A beautiful outdoor scene";
    [SerializeField] private string negativePrompt = "";
    [SerializeField] private string sceneClass = "outdoor";
    [SerializeField] private int seed = 42;

    [Header("Model Loading")]
    [SerializeField] private Transform modelParent;
    [SerializeField] private bool autoLoadModel = true;
    [SerializeField] private bool loadSkybox = true;
    [SerializeField] private bool cleanupTempFiles = true;

    [Header("Skybox Settings")]
    [SerializeField] private Material skyboxMaterial;
    [SerializeField] private bool createNewSkyboxMaterial = true;

    private string currentJobId;
    private List<GameObject> loadedModels = new List<GameObject>();
    private List<string> tempFiles = new List<string>();
    private Material originalSkybox;

    private void Start()
    {
        originalSkybox = RenderSettings.skybox;

        if (createNewSkyboxMaterial && skyboxMaterial == null)
        {
            skyboxMaterial = new Material(Shader.Find("Skybox/Panoramic"));
            skyboxMaterial.SetFloat("_Exposure", 1.0f);
            skyboxMaterial.SetFloat("_Rotation", 0f);
        }
    }

    private void OnDestroy()
    {
        CleanupTempFiles();
    }

    public void GenerateScene()
    {
        StartCoroutine(GenerateSceneCoroutine());
    }

    private IEnumerator GenerateSceneCoroutine()
    {
        Debug.Log("🎨 Starting scene generation...");

        SceneGenerationRequest request = new SceneGenerationRequest
        {
            prompt = prompt,
            negative_prompt = negativePrompt,
            classes = sceneClass,
            seed = seed
        };

        string jsonData = JsonConvert.SerializeObject(request);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest www = new UnityWebRequest(serverUrl + "/generate_scene", "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.timeout = 600;

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ Error: {www.error}");
                Debug.LogError($"Response: {www.downloadHandler.text}");
                yield break;
            }

            Debug.Log("✅ Scene generation started successfully!");
            SceneGenerationResponse response = JsonConvert.DeserializeObject<SceneGenerationResponse>(www.downloadHandler.text);

            currentJobId = response.job_id;
            Debug.Log($"📋 Job ID: {currentJobId}");
            if (autoLoadModel)
            {
                if (loadSkybox)
                {
                    yield return StartCoroutine(LoadSkyboxImage(currentJobId));
                }

                yield return StartCoroutine(DownloadAndLoadGLB(currentJobId));
            }
        }
    }

    private IEnumerator LoadSkyboxImage(string jobId)
    {
        string skyboxUrl = $"{serverUrl}/api/file/{jobId}/sky_image_sr.png";
        Debug.Log($"🌅 Downloading skybox from: {skyboxUrl}");

        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(skyboxUrl))
        {
            www.timeout = downloadTimeout;
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ Error downloading skybox: {www.error}");
                yield break;
            }

            Debug.Log("✅ Skybox downloaded successfully!");

            Texture2D skyboxTexture = DownloadHandlerTexture.GetContent(www);

            if (skyboxMaterial != null)
            {
                skyboxMaterial.SetTexture("_MainTex", skyboxTexture);
                RenderSettings.skybox = skyboxMaterial;
                DynamicGI.UpdateEnvironment();
                Debug.Log("🌅 Skybox applied successfully!");
            }
            else
            {
                Debug.LogError("❌ Skybox material is null!");
            }
        }
    }

    private IEnumerator DownloadAndLoadGLB(string jobId)
    {
        string url = $"{serverUrl}/api/file/{jobId}/mesh_layer0.glb";
        Debug.Log($"📥 Downloading GLB from: {url}");
        string filename = "mesh_layer_0";

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.timeout = downloadTimeout;

            // Show progress for large files
            var operation = www.SendWebRequest();
            while (!operation.isDone)
            {
                Debug.Log($"📥 Download progress: {www.downloadProgress * 100:F1}%");
                yield return new WaitForSeconds(0.5f);
            }

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ Error downloading GLB: {www.error}");
                yield break;
            }


            // Save to persistent storage
            string tempPath = Path.Combine(Application.persistentDataPath, $"{currentJobId}_{filename}");

            try
            {
                File.WriteAllBytes(tempPath, www.downloadHandler.data);
                tempFiles.Add(tempPath);
                Debug.Log($"💾 Saved to: {tempPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to save GLB: {e.Message}");
                yield break;
            }

            // Load GLB with error handling
            yield return StartCoroutine(LoadGLBFromFile(tempPath, filename));
        }
    }

    private IEnumerator LoadGLBFromFile(string filePath, string filename)
    {
        Debug.Log($"📦 Loading GLB: {filename}");

        GameObject container = new GameObject($"Scene_{filename}");

        if (modelParent != null)
        {
            container.transform.SetParent(modelParent);
        }

        container.transform.localPosition = Vector3.zero;
        container.transform.localRotation = Quaternion.identity;
        container.transform.localScale = Vector3.one;

        GltfAsset gltfAsset = container.AddComponent<GltfAsset>();

        // Load with callback to check success
        bool loadSuccess = false;
        string loadError = null;

        var task = gltfAsset.Load(filePath);

        while (!task.IsCompleted)
        {
            yield return null;
        }

        if (task.IsFaulted || task.Exception != null)
        {
            loadError = task.Exception?.ToString() ?? "Unknown error";
            Debug.LogError($"❌ GLB load failed: {loadError}");
            Destroy(container);
            yield break;
        }

        loadSuccess = task.Result;

        if (loadSuccess)
        {
            loadedModels.Add(container);
            Debug.Log($"✅ GLB loaded successfully: {filename}");
        }
        else
        {
            Debug.LogError($"❌ GLB load returned false: {filename}");
            Destroy(container);
        }
    }

    public void LoadSceneFromJobId(string jobId, bool withSkybox = true)
    {
        StartCoroutine(LoadSceneFromJobIdCoroutine(jobId, withSkybox));
    }

    private IEnumerator LoadSceneFromJobIdCoroutine(string jobId, bool withSkybox)
    {
        currentJobId = jobId;
        ClearLoadedModels();

        if (withSkybox)
        {
            yield return StartCoroutine(LoadSkyboxImage(jobId));
        }

        string meshUrl = $"{serverUrl}/api/file/{jobId}/mesh_layer0.glb";

        GameObject container = new GameObject($"Scene_{jobId}");
        if (modelParent != null)
        {
            container.transform.SetParent(modelParent);
        }
        container.transform.localPosition = Vector3.zero;
        container.transform.localRotation = Quaternion.identity;
        container.transform.localScale = Vector3.one;

        GltfAsset gltfAsset = container.AddComponent<GltfAsset>();

        var task = gltfAsset.Load(meshUrl);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Result)
        {
            loadedModels.Add(container);
            Debug.Log($"✅ Scene loaded from job: {jobId}");
        }
        else
        {
            Debug.LogError($"❌ Failed to load scene from job: {jobId}");
            Destroy(container);
        }
    }

    public void ClearLoadedModels()
    {
        Debug.Log("🧹 Clearing loaded models...");

        foreach (var model in loadedModels)
        {
            if (model != null)
                Destroy(model);
        }
        loadedModels.Clear();
    }

    private void CleanupTempFiles()
    {
        if (!cleanupTempFiles) return;

        Debug.Log($"🧹 Cleaning up {tempFiles.Count} temporary files...");

        foreach (string filePath in tempFiles)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"⚠️ Failed to delete temp file {filePath}: {e.Message}");
            }
        }

        tempFiles.Clear();
    }

    public void RestoreOriginalSkybox()
    {
        if (originalSkybox != null)
        {
            RenderSettings.skybox = originalSkybox;
            DynamicGI.UpdateEnvironment();
            Debug.Log("🌅 Original skybox restored");
        }
    }

    public void GenerateSceneWithPrompt(string customPrompt, string customClass = "outdoor", int customSeed = 42)
    {
        prompt = customPrompt;
        sceneClass = customClass;
        seed = customSeed;
        GenerateScene();
    }

#if UNITY_EDITOR
    [ContextMenu("Generate Scene")]
    private void TestGenerate()
    {
        GenerateScene();
    }

    [ContextMenu("Load Test Scene")]
    private void TestLoadScene()
    {
        LoadSceneFromJobId("ef7d0667-e40e-4af3-8b28-76f6d19d22b3", true);
    }

    [ContextMenu("Clear Models")]
    private void TestClear()
    {
        ClearLoadedModels();
    }

    [ContextMenu("Restore Skybox")]
    private void TestRestoreSkybox()
    {
        RestoreOriginalSkybox();
    }

    [ContextMenu("Cleanup Temp Files")]
    private void TestCleanup()
    {
        CleanupTempFiles();
    }
#endif
}