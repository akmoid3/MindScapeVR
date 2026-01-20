using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using GLTFast;

public class LevelSaveSystem : MonoBehaviour
{
    [Header("Settings")] 
    [SerializeField] private LevelManager levelManager; 
    [SerializeField] private Transform modelParent;
    [SerializeField] private GameObject audioPrefab;
    [SerializeField] private Light sceneDirectionalLight; 
    [SerializeField] private SceneLightUIController lightUIController;
    [SerializeField] private Material environmentMaterial;
    [SerializeField] private GameObject plane;
    [SerializeField] private HunyuanWorldClient hunyuanWorldClient;
    [Serializable]
    public class SceneData
    {
        public List<ObjectData> objects = new List<ObjectData>();
        public float lightIntensity = 1.0f;
        public float[] lightRotation;
        public string environmentName;

    }

    [Serializable]
    public class ObjectData
    {
        public string type;
        public string fileName;
        public float[] position;
        public float[] rotation;
        public float[] scale;
        public bool isLooping;
    }

    

    private void Start()
    {
        LoadScene();
    }

    private string GetSavePath()
    {
        string saveFolder = Path.Combine(Application.persistentDataPath, "SavedScenes");

        if (!Directory.Exists(saveFolder))
        {
            Directory.CreateDirectory(saveFolder);
        }
        
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        return Path.Combine(saveFolder, $"SaveData_{sceneName}.json");
    }

    public void SaveScene()
    {
        SceneData data = new SceneData();
        GeneratedObjectInfo[] generatedObjects = FindObjectsOfType<GeneratedObjectInfo>();

        
        if (sceneDirectionalLight != null)
        {
            data.lightIntensity = sceneDirectionalLight.intensity;
            data.lightRotation = new float[] 
            {
                sceneDirectionalLight.transform.rotation.x,
                sceneDirectionalLight.transform.rotation.y,
                sceneDirectionalLight.transform.rotation.z,
                sceneDirectionalLight.transform.rotation.w
            };
        }
        
        GameObject environment = GameObject.FindGameObjectWithTag("Environment");
        if(environment != null)
            data.environmentName = environment.name;
        
        foreach (var obj in generatedObjects)
        {
            ObjectData objData = new ObjectData
            {
                type = obj.type.ToString(),
                fileName = obj.fileName,
                position = new float[] { obj.transform.position.x, obj.transform.position.y, obj.transform.position.z },
                rotation = new float[]
                {
                    obj.transform.rotation.x, obj.transform.rotation.y, obj.transform.rotation.z,
                    obj.transform.rotation.w
                },
                scale = new float[]
                    { obj.transform.localScale.x, obj.transform.localScale.y, obj.transform.localScale.z }
            };

            if (obj.type == GenerationType.Audio)
            {
                AudioSource source = obj.GetComponent<AudioSource>();
                if (source != null) objData.isLooping = source.loop;
            }

            data.objects.Add(objData);
        }

        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(GetSavePath(), json);
        Debug.Log($"Scena salvata in: {GetSavePath()}");
    }

    public void LoadScene()
    {
        string path = GetSavePath();
        if (!File.Exists(path))
        {
            Debug.LogWarning("Nessun salvataggio trovato.");
            return;
        }

        StartCoroutine(LoadSceneCoroutine(path));
    }

    public IEnumerator LoadSceneCoroutine(string path)
    {
        GeneratedObjectInfo[] oldObjects = FindObjectsOfType<GeneratedObjectInfo>();
        foreach (var old in oldObjects) Destroy(old.gameObject);

        GameObject oldEnvironment = GameObject.FindGameObjectWithTag("Environment");
        if (oldEnvironment != null) Destroy(oldEnvironment);

        if (levelManager != null) levelManager.ClearAudioList();

        yield return null; 

        string json = File.ReadAllText(path);
        SceneData data = JsonConvert.DeserializeObject<SceneData>(json);

        if (sceneDirectionalLight != null && data.lightRotation != null && data.lightRotation.Length == 4)
        {
            sceneDirectionalLight.intensity = data.lightIntensity;

            Quaternion rot = new Quaternion(data.lightRotation[0], data.lightRotation[1], data.lightRotation[2], data.lightRotation[3]);
            sceneDirectionalLight.transform.rotation = rot;

            if (lightUIController != null)
            {
                lightUIController.UpdateUIFromLight();
            }
        }

        if (!string.IsNullOrEmpty(data.environmentName))
        {
            if(plane)
            {
                plane.SetActive(false);
            }
            yield return StartCoroutine(LoadEnvironment(data.environmentName));
        }
        

        foreach (var objData in data.objects)
        {
            if (objData.type == "Model")
                yield return StartCoroutine(LoadModel(objData));
            else if (objData.type == "Audio")
                yield return StartCoroutine(LoadAudio(objData));
        }

        Debug.Log("Caricamento completato.");
    }
    
    private IEnumerator LoadEnvironment(string jobId)
    {
        yield return StartCoroutine(LoadSkybox(jobId));

        yield return StartCoroutine(LoadEnvironmentMesh(jobId));
    }
    
    private IEnumerator LoadSkybox(string jobId)
    {
        string folderPath = Path.Combine(Application.persistentDataPath, "Scenes");
        string filePath = Path. Combine(folderPath, $"{jobId}_skybox.png");

        if (!File.Exists(filePath))
        {
            Debug.LogWarning($"File skybox non trovato:  {filePath}");
            yield break;
        }

        byte[] fileData = File.ReadAllBytes(filePath);
        Texture2D skyboxTexture = new Texture2D(2, 2);
        
        if (skyboxTexture.LoadImage(fileData))
        {
                Material skyboxMaterial = new Material(Shader.Find("Skybox/Panoramic"));
                skyboxMaterial.SetFloat("_Exposure", 1.0f);
                skyboxMaterial.SetFloat("_Rotation", 0f);
                skyboxMaterial.SetTexture("_MainTex", skyboxTexture);
                RenderSettings.skybox = skyboxMaterial;
                DynamicGI.UpdateEnvironment();
                Debug.Log($"Skybox caricata:  {jobId}_skybox.png");
        }
        else
        {
            Debug.LogError($"Errore nel caricamento della skybox: {jobId}");
        }

        yield return null;
    }

    private IEnumerator LoadEnvironmentMesh(string jobId)
    {
        string folderPath = Path.Combine(Application.persistentDataPath, "Scenes");
        string filePath = Path. Combine(folderPath, $"{jobId}_mesh.glb");

        if (!File.Exists(filePath))
        {
            Debug. LogWarning($"File environment mesh non trovato: {filePath}");
            yield break;
        }

        GameObject container = new GameObject(jobId);
        container.tag = "Environment";

        if (modelParent != null) container.transform.SetParent(modelParent);

        container.transform.position = Vector3.zero;
        container.transform. rotation = Quaternion. Euler(270f, 0f, 0f);
        container.transform. localScale = new Vector3(15f, 15f, 15f);

        GltfAsset gltfAsset = container.AddComponent<GltfAsset>();
        
        var task = gltfAsset.Load(filePath);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Result)
        {
            Debug.Log($"Environment mesh caricata: {jobId}_mesh.glb");

            if (environmentMaterial != null)
            {
                ApplyMaterialToScene(container);
            }
            if(hunyuanWorldClient)
                hunyuanWorldClient.sceneObj = container;
        }
        else
        {
            Debug.LogError($"Errore nel caricamento dell'environment mesh: {jobId}");
            Destroy(container);
        }
    }

    private void ApplyMaterialToScene(GameObject sceneRoot)
    {
        MeshRenderer[] renderers = sceneRoot.GetComponentsInChildren<MeshRenderer>();

        foreach (MeshRenderer renderer in renderers)
        {
            Material[] materials = new Material[renderer.sharedMaterials. Length];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = environmentMaterial;
            }
            renderer.sharedMaterials = materials;
        }
    }

    private IEnumerator LoadModel(ObjectData data)
    {
        string folderPath = Path.Combine(Application.persistentDataPath, "Models");
        string filePath = Path.Combine(folderPath, data.fileName);

        if (!File.Exists(filePath)) yield break;

        GameObject container = new GameObject($"Model_{data.fileName}");
        var info = container.AddComponent<GeneratedObjectInfo>();
        info.type = GenerationType.Model;
        info.fileName = data.fileName;

        container.AddComponent<BoxCollider>();
        int selectableLayer = LayerMask.NameToLayer("SelectableObjects");
        if (selectableLayer != -1) container.layer = selectableLayer;

        if (modelParent != null) container.transform.SetParent(modelParent);

        container.transform.position = new Vector3(data.position[0], data.position[1], data.position[2]);
        container.transform.rotation =
            new Quaternion(data.rotation[0], data.rotation[1], data.rotation[2], data.rotation[3]);
        container.transform.localScale = new Vector3(data.scale[0], data.scale[1], data.scale[2]);

        var gltf = container.AddComponent<GltfAsset>();
        gltf.Url = "file://" + filePath;

    }

    private IEnumerator LoadAudio(ObjectData data)
    {
        string folderPath = Path.Combine(Application.persistentDataPath, "GeneratedAudio");
        string filePath = Path.Combine(folderPath, data.fileName);

        if (!File.Exists(filePath)) yield break;

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.WAV))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                GameObject go = Instantiate(audioPrefab);

                var info = go.AddComponent<GeneratedObjectInfo>();
                info.type = GenerationType.Audio;
                info.fileName = data.fileName;

                go.transform.position = new Vector3(data.position[0], data.position[1], data.position[2]);
                go.transform.rotation =
                    new Quaternion(data.rotation[0], data.rotation[1], data.rotation[2], data.rotation[3]);

                AudioSource source = go.GetComponent<AudioSource>();
                if (source == null) source = go.AddComponent<AudioSource>();
                source.clip = clip;
                source.spatialBlend = 1f;

                source.loop = data.isLooping;
                
                LookAtCamera lookAt = go.GetComponent<LookAtCamera>();
                if (lookAt != null && levelManager != null)
                {
                    lookAt.Cam(levelManager.GetFreeCam());
                }

                if (levelManager != null)
                {

                    levelManager.AddSoundToList(go);

                    RandomizeAudio randomizer = go.GetComponent<RandomizeAudio>();
                    if(randomizer != null) randomizer.EnableCollider(false);
                }
            }
        }
    }
}