using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.IO;
using System.Net.Http;

public class HunyuanWorldClientTests
{
    private GameObject testGameObject;
    private HunyuanWorldClient hunyuanWorldClient;
    private TMP_InputField inputField;
    private Button generateButton;
    private TMP_Text buttonText;
    private Material skyboxMaterial;
    private Material sceneMaterial;
    private GameObject sceneObj;
    private GameObject planeObj;
    private GameObject deleteSceneButton;
    private static readonly string SERVER_URL = "http://localhost:8000";
    private static readonly string ERROR_SERVER_URL = "http://localhost:9999";
    private static AudioListener audioListener;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        if (audioListener == null)
        {
            var audioListenerGO = new GameObject("GlobalAudioListener");
            audioListener = audioListenerGO.AddComponent<AudioListener>();
            Object.DontDestroyOnLoad(audioListenerGO);
        }

        StartServerHealthCheck();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        if (audioListener != null)
        {
            Object.Destroy(audioListener. gameObject);
            audioListener = null;
        }
    }

    [SetUp]
    public void SetUp()
    {
        ClearServerJobs();

        // Crea HunyuanWorldClient
        testGameObject = new GameObject("TestHunyuanWorldClient");
        hunyuanWorldClient = testGameObject.AddComponent<HunyuanWorldClient>();

        // Crea InputField
        var inputFieldGO = new GameObject("InputField");
        inputFieldGO.AddComponent<Canvas>();
        inputField = inputFieldGO.AddComponent<TMP_InputField>();
        inputFieldGO.AddComponent<GraphicRaycaster>();
        ReflectionHelper.SetFieldValue(hunyuanWorldClient, "inputField", inputField);

        // Crea Button
        var buttonGO = new GameObject("GenerateButton");
        buttonGO.AddComponent<Canvas>();
        generateButton = buttonGO.AddComponent<Button>();
        
        var buttonTextGO = new GameObject("ButtonText");
        buttonTextGO. transform.SetParent(buttonGO. transform);
        buttonText = buttonTextGO.AddComponent<TextMeshProUGUI>();
        
        buttonGO.AddComponent<GraphicRaycaster>();

        // Crea scene objects
        sceneObj = new GameObject("SceneObj");
        planeObj = new GameObject("PlaneObj");
        deleteSceneButton = new GameObject("DeleteSceneButton");

        // Crea materials
        skyboxMaterial = new Material(Shader.Find("Standard"));
        sceneMaterial = new Material(Shader.Find("Standard"));

        // Assegna al client
        ReflectionHelper.SetFieldValue(hunyuanWorldClient, "generateButton", generateButton);
        ReflectionHelper.SetFieldValue(hunyuanWorldClient, "buttonText", null);
        ReflectionHelper. SetFieldValue(hunyuanWorldClient, "skyboxMaterial", skyboxMaterial);
        ReflectionHelper. SetFieldValue(hunyuanWorldClient, "sceneMaterial", sceneMaterial);
        ReflectionHelper.SetFieldValue(hunyuanWorldClient, "sceneObj", null);
        ReflectionHelper. SetFieldValue(hunyuanWorldClient, "planeObj", planeObj);
        ReflectionHelper.SetFieldValue(hunyuanWorldClient, "deleteSceneButton", deleteSceneButton);
        ReflectionHelper.SetFieldValue(hunyuanWorldClient, "serverUrl", SERVER_URL);
        ReflectionHelper.SetFieldValue(hunyuanWorldClient, "negativePrompt", "");
        ReflectionHelper.SetFieldValue(hunyuanWorldClient, "sceneClass", "outdoor");
        ReflectionHelper. SetFieldValue(hunyuanWorldClient, "seed", 42);

        // Inizializza
        hunyuanWorldClient. GetType().GetMethod("Start",
            System.Reflection.BindingFlags.NonPublic | System. Reflection.BindingFlags.Instance)
            ?.Invoke(hunyuanWorldClient, null);
    }

    [TearDown]
    public void TearDown()
    {
        if (testGameObject != null)
            Object.Destroy(testGameObject);
        
        if (inputField != null && inputField.gameObject != null)
            Object.Destroy(inputField.gameObject);
        
        if (generateButton != null && generateButton.gameObject != null)
            Object.Destroy(generateButton.gameObject);
        
        if (sceneObj != null)
            Object.Destroy(sceneObj);
        
        if (planeObj != null)
            Object. Destroy(planeObj);
        
        if (deleteSceneButton != null)
            Object.Destroy(deleteSceneButton);

        // Pulisci i file
        string folderPath = Path.Combine(Application.persistentDataPath, "Scenes");
        if (Directory. Exists(folderPath))
        {
            try
            {
                Directory.Delete(folderPath, true);
            }
            catch { }
        }
    }

    #region Server Management

    private void StartServerHealthCheck()
    {
        using (HttpClient client = new HttpClient())
        {
            try
            {
                var response = client.GetAsync($"{SERVER_URL}/health").Result;
                Debug.Log("✓ Mock server HunyuanWorld è attivo");
            }
            catch
            {
                Debug.LogWarning("⚠ Mock server HunyuanWorld non disponibile");
            }
        }
    }

    private void ClearServerJobs()
    {
        using (HttpClient client = new HttpClient())
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{SERVER_URL}/clear");
                client.SendAsync(request).Wait();
            }
            catch { }
        }
    }

    #endregion

    #region Line 73 - GetComponentInChildren

    [Test]
    public void Start_WhenButtonTextIsNull_GetComponentInChildren()
    {
        var newTestGO = new GameObject("TestHunyuanWorldNew");
        var newClient = newTestGO.AddComponent<HunyuanWorldClient>();

        var newButtonGO = new GameObject("GenerateButtonNew");
        newButtonGO.AddComponent<Canvas>();
        var newButton = newButtonGO.AddComponent<Button>();
        
        var newButtonTextGO = new GameObject("ButtonTextNew");
        newButtonTextGO.transform.SetParent(newButtonGO.transform);
        var newButtonText = newButtonTextGO.AddComponent<TextMeshProUGUI>();
        
        newButtonGO.AddComponent<GraphicRaycaster>();

        ReflectionHelper.SetFieldValue(newClient, "generateButton", newButton);
        ReflectionHelper.SetFieldValue(newClient, "buttonText", null);

        TMP_Text beforeStart = (TMP_Text)ReflectionHelper.GetFieldValue(newClient, "buttonText");
        Assert.IsNull(beforeStart);

        newClient.GetType().GetMethod("Start",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(newClient, null);

        TMP_Text afterStart = (TMP_Text)ReflectionHelper.GetFieldValue(newClient, "buttonText");
        Assert.IsNotNull(afterStart);

        Object.Destroy(newTestGO);
        Object.Destroy(newButtonGO);
    }

    #endregion

    #region Line 98-109 - GenerateScene

    [Test]
    public void GenerateScene_WithEmptyInput_DoesNotGenerate()
    {
        inputField.text = "";
        hunyuanWorldClient.GenerateScene();

        bool isGenerating = (bool)ReflectionHelper.GetFieldValue(hunyuanWorldClient, "isGenerating");
        Assert.IsFalse(isGenerating);
    }

    [Test]
    public void GenerateScene_WhenAlreadyGenerating_IgnoresRequest()
    {
        ReflectionHelper.SetFieldValue(hunyuanWorldClient, "isGenerating", true);
        inputField.text = "Should be ignored";

        hunyuanWorldClient.GenerateScene();

        Assert.IsTrue((bool)ReflectionHelper.GetFieldValue(hunyuanWorldClient, "isGenerating"));
        ReflectionHelper.SetFieldValue(hunyuanWorldClient, "isGenerating", false);
    }

    #endregion

    #region Line 111-133 - SetButtonState

    [Test]
    public void SetButtonState_Bool_UpdatesUI()
    {
        // ✅ Usa il nuovo overload per bool
        ReflectionHelper.CallMethod(hunyuanWorldClient, "SetButtonState", true);

        Assert.IsFalse(generateButton.interactable);
        Assert.AreEqual("Generating...", buttonText.text);
        Assert.IsTrue((bool)ReflectionHelper.GetFieldValue(hunyuanWorldClient, "isGenerating"));
    }

    [Test]
    public void SetButtonState_String_UpdatesUI()
    {
        // ✅ Usa il nuovo overload per string
        ReflectionHelper.CallMethod(hunyuanWorldClient, "SetButtonState", "Loading Skybox.. .");

        Assert.AreEqual("Loading Skybox.. .", buttonText.text);
        Assert.IsFalse(generateButton.interactable);
        Assert.IsTrue((bool)ReflectionHelper.GetFieldValue(hunyuanWorldClient, "isGenerating"));
    }

    [Test]
    public void SetButtonState_GenerateState_EnablesButton()
    {
        // ✅ Usa il nuovo overload per string
        ReflectionHelper.CallMethod(hunyuanWorldClient, "SetButtonState", "Generate");

        Assert.AreEqual("Generate", buttonText.text);
        Assert.IsTrue(generateButton.interactable);
        Assert.IsFalse((bool)ReflectionHelper.GetFieldValue(hunyuanWorldClient, "isGenerating"));
    }

    #endregion

    #region Line 159-164 - HTTP Error

    [UnityTest]
    public IEnumerator GenerateScene_WithServerError_HandlesFailure()
    {
        LogAssert. Expect(LogType.Error, "Generation failed:");

        ReflectionHelper.SetFieldValue(hunyuanWorldClient, "serverUrl", ERROR_SERVER_URL);

        inputField.text = "Test server error";

        hunyuanWorldClient.GenerateScene();

        float elapsedTime = 0f;
        while ((bool)ReflectionHelper.GetFieldValue(hunyuanWorldClient, "isGenerating") && elapsedTime < 15f)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Assert.IsTrue(generateButton.interactable);

        ReflectionHelper.SetFieldValue(hunyuanWorldClient, "serverUrl", SERVER_URL);
    }

    #endregion

    #region Line 191-195 - Skybox Download Error

    [UnityTest]
    public IEnumerator GenerateScene_WithSkyboxDownloadError_HandlesFailure()
    {
        LogAssert.Expect(LogType.Error, "Skybox download failed:");

        ReflectionHelper.SetFieldValue(hunyuanWorldClient, "serverUrl", ERROR_SERVER_URL);

        inputField.text = "Test skybox error";

        hunyuanWorldClient.GenerateScene();

        yield return new WaitForSeconds(5f);

        ReflectionHelper.SetFieldValue(hunyuanWorldClient, "serverUrl", SERVER_URL);
    }

    #endregion

    #region Line 226-231 - Model Download Error

    [UnityTest]
    public IEnumerator GenerateScene_WithModelDownloadError_HandlesFailure()
    {
        LogAssert. Expect(LogType.Error, "Model download failed:");

        ReflectionHelper.SetFieldValue(hunyuanWorldClient, "serverUrl", ERROR_SERVER_URL);

        inputField.text = "Test model error";

        hunyuanWorldClient.GenerateScene();

        yield return new WaitForSeconds(5f);

        ReflectionHelper.SetFieldValue(hunyuanWorldClient, "serverUrl", SERVER_URL);
    }

    #endregion

    #region Line 79-94 - Update Method

    [Test]
    public void Update_WhenSceneExists_ShowsDeleteButton()
    {
        ReflectionHelper.SetFieldValue(hunyuanWorldClient, "sceneObj", sceneObj);
        deleteSceneButton.SetActive(false);
        planeObj.SetActive(true);

        hunyuanWorldClient.GetType().GetMethod("Update",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(hunyuanWorldClient, null);

        Assert.IsTrue(deleteSceneButton.activeSelf);
        Assert.IsFalse(planeObj. activeSelf);
    }

    [Test]
    public void Update_WhenSceneDoesNotExist_HidesDeleteButton()
    {
        ReflectionHelper.SetFieldValue(hunyuanWorldClient, "sceneObj", null);
        deleteSceneButton.SetActive(true);
        planeObj.SetActive(false);

        hunyuanWorldClient.GetType().GetMethod("Update",
            System.Reflection.BindingFlags.NonPublic | System. Reflection.BindingFlags.Instance)
            ?.Invoke(hunyuanWorldClient, null);

        Assert.IsFalse(deleteSceneButton.activeSelf);
        Assert.IsTrue(planeObj.activeSelf);
    }

    #endregion

    #region Line 286-294 - RestoreOriginalSkybox

    [Test]
    public void RestoreOriginalSkybox_RestoresSkybox()
    {
        Material originalSkybox = RenderSettings.skybox;
        ReflectionHelper.SetFieldValue(hunyuanWorldClient, "originalSkybox", originalSkybox);

        hunyuanWorldClient.RestoreOriginalSkybox();

        Assert.AreEqual(originalSkybox, RenderSettings.skybox);
    }

    #endregion

    #region Line 296-303 - DestroyScene

    [Test]
    public void DestroyScene_DestroysSceneObject()
    {
        var testScene = new GameObject("TestScene");
        ReflectionHelper.SetFieldValue(hunyuanWorldClient, "sceneObj", testScene);

        hunyuanWorldClient. DestroyScene();

        Assert.IsNull((GameObject)ReflectionHelper.GetFieldValue(hunyuanWorldClient, "sceneObj"));
    }

    #endregion

    #region Full Integration Tests

    [UnityTest]
    public IEnumerator GenerateScene_SuccessfulFlow_GeneratesScene()
    {
        inputField.text = "A beautiful outdoor garden";

        hunyuanWorldClient.GenerateScene();

        float elapsedTime = 0f;
        while ((bool)ReflectionHelper.GetFieldValue(hunyuanWorldClient, "isGenerating") && elapsedTime < 120f)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Assert.IsFalse((bool)ReflectionHelper.GetFieldValue(hunyuanWorldClient, "isGenerating"));
        Assert.IsTrue(generateButton.interactable);
    }

    [UnityTest]
    public IEnumerator GenerateScene_ClearsInputAfterGeneration()
    {
        inputField.text = "Test input clearing";

        hunyuanWorldClient.GenerateScene();

        float elapsedTime = 0f;
        while ((bool)ReflectionHelper.GetFieldValue(hunyuanWorldClient, "isGenerating") && elapsedTime < 120f)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Assert.AreEqual("", inputField.text);
    }

    [UnityTest]
    public IEnumerator GenerateScene_SavesFileToStorage()
    {
        inputField.text = "Test file saving";

        hunyuanWorldClient.GenerateScene();

        float elapsedTime = 0f;
        while ((bool)ReflectionHelper.GetFieldValue(hunyuanWorldClient, "isGenerating") && elapsedTime < 120f)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        string folderPath = Path.Combine(Application.persistentDataPath, "Scenes");
        Assert.IsTrue(Directory.Exists(folderPath));
        Assert.Greater(Directory.GetFiles(folderPath).Length, 0);
    }

    #endregion

    #region Helper Class

private static class ReflectionHelper
{
    public static void SetFieldValue(object obj, string fieldName, object value)
    {
        if (obj == null) return;
        var field = obj.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System. Reflection.BindingFlags.Instance);
        field?.SetValue(obj, value);
    }

    public static object GetFieldValue(object obj, string fieldName)
    {
        if (obj == null) return null;
        var field = obj. GetType().GetField(fieldName,
            System.Reflection. BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field?.GetValue(obj);
    }

    // ✅ NUOVO: Overload per bool
    public static object CallMethod(object obj, string methodName, bool parameter)
    {
        if (obj == null) return null;
        var method = obj.GetType().GetMethod(methodName,
            System.Reflection. BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            new System.Type[] { typeof(bool) },
            null);
        return method?.Invoke(obj, new object[] { parameter });
    }

    // ✅ NUOVO: Overload per string
    public static object CallMethod(object obj, string methodName, string parameter)
    {
        if (obj == null) return null;
        var method = obj.GetType().GetMethod(methodName,
            System.Reflection. BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            new System.Type[] { typeof(string) },
            null);
        return method?.Invoke(obj, new object[] { parameter });
    }

    // ✅ ORIGINALE: Per parametri generici (non usare con SetButtonState)
    public static object CallMethod(object obj, string methodName, params object[] parameters)
    {
        if (obj == null) return null;
        
        // Costruisci array di tipi dai parametri
        System.Type[] types = new System.Type[parameters. Length];
        for (int i = 0; i < parameters. Length; i++)
        {
            types[i] = parameters[i]?.GetType() ?? typeof(object);
        }
        
        var method = obj.GetType().GetMethod(methodName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection. BindingFlags.Instance,
            null,
            types,
            null);
        return method?. Invoke(obj, parameters);
    }
}

#endregion
}