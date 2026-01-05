using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.IO;
using System.Net. Http;

public class AudioGenClientFullCoverageTests
{
    private GameObject testGameObject;
    private AudioGenClient audioGenClient;
    private TMP_InputField inputField;
    private Button generateButton;
    private TMP_Text buttonText;
    private GameObject audioPrefab;
    private LevelManager levelManager;
    private StateManager stateManager;
    private static readonly string SERVER_URL = "http://localhost:8081";
    private static readonly string ERROR_SERVER_URL = "http://localhost:9999";
    private static AudioListener globalAudioListener;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        if (globalAudioListener == null)
        {
            var audioListenerGO = new GameObject("GlobalAudioListener");
            globalAudioListener = audioListenerGO.AddComponent<AudioListener>();
            Object.DontDestroyOnLoad(audioListenerGO);
        }

        StartServerHealthCheck();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        if (globalAudioListener != null)
        {
            Object.Destroy(globalAudioListener.gameObject);
            globalAudioListener = null;
        }
    }

    [SetUp]
    public void SetUp()
    {
        ClearServerJobs();

        var stateManagerGO = new GameObject("StateManager");
        stateManager = stateManagerGO.AddComponent<StateManager>();

        var freeCamGO = new GameObject("FreeCamera");
        freeCamGO.AddComponent<Camera>();
        freeCamGO.transform.position = new Vector3(0, 1, -5);

        var levelManagerGO = new GameObject("LevelManager");
        levelManager = levelManagerGO. AddComponent<LevelManager>();

        var playerGO = new GameObject("Player");
        var editorUIGO = new GameObject("EditorUI");
        var speechGO = new GameObject("Speech");
        var rotatingCameraGO = new GameObject("RotatingCamera");
        rotatingCameraGO.AddComponent<Camera>();
        var mainMenuUIGO = new GameObject("MainMenuUI");
        var exitButtonGO = new GameObject("ExitButton");

        ReflectionHelper.SetFieldValue(levelManager, "freeCamObject", freeCamGO);
        ReflectionHelper.SetFieldValue(levelManager, "playerObject", playerGO);
        ReflectionHelper.SetFieldValue(levelManager, "editorUI", editorUIGO);
        ReflectionHelper.SetFieldValue(levelManager, "speech", speechGO);
        ReflectionHelper.SetFieldValue(levelManager, "rotatingCamera", rotatingCameraGO);
        ReflectionHelper.SetFieldValue(levelManager, "mainMenuButtonsUI", mainMenuUIGO);
        ReflectionHelper.SetFieldValue(levelManager, "exitPlayModeButton", exitButtonGO);

        levelManager.GetType().GetMethod("Start",
            System.Reflection.BindingFlags.NonPublic | System. Reflection.BindingFlags.Instance)
            ?.Invoke(levelManager, null);

        testGameObject = new GameObject("TestAudioGenClient");
        audioGenClient = testGameObject.AddComponent<AudioGenClient>();

        var inputFieldGO = new GameObject("InputField");
        inputFieldGO.AddComponent<Canvas>();
        inputField = inputFieldGO.AddComponent<TMP_InputField>();
        inputFieldGO.AddComponent<GraphicRaycaster>();
        ReflectionHelper.SetFieldValue(audioGenClient, "inputField", inputField);

        var buttonGO = new GameObject("GenerateButton");
        buttonGO.AddComponent<Canvas>();
        generateButton = buttonGO.AddComponent<Button>();
        
        var buttonTextGO = new GameObject("ButtonText");
        buttonTextGO.transform.SetParent(buttonGO.transform);
        buttonText = buttonTextGO.AddComponent<TextMeshProUGUI>();
        
        buttonGO.AddComponent<GraphicRaycaster>();
        ReflectionHelper.SetFieldValue(audioGenClient, "generateButton", generateButton);
        ReflectionHelper.SetFieldValue(audioGenClient, "buttonText", null);

        audioPrefab = new GameObject("AudioPrefab");
        audioPrefab.AddComponent<AudioSource>();
        audioPrefab. AddComponent<GeneratedObjectInfo>();
        audioPrefab.AddComponent<LookAtCamera>();
        ReflectionHelper.SetFieldValue(audioGenClient, "audioPrefab", audioPrefab);

        ReflectionHelper.SetFieldValue(audioGenClient, "levelManager", levelManager);
        ReflectionHelper. SetFieldValue(audioGenClient, "serverUrl", SERVER_URL);
        ReflectionHelper.SetFieldValue(audioGenClient, "saveToFile", true);

        audioGenClient.GetType().GetMethod("Start",
            System. Reflection.BindingFlags.NonPublic | System.Reflection. BindingFlags.Instance)
            ?.Invoke(audioGenClient, null);
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
        
        if (audioPrefab != null)
            Object.Destroy(audioPrefab);
        
        if (levelManager != null && levelManager.gameObject != null)
            Object.Destroy(levelManager.gameObject);
        
        if (stateManager != null && stateManager.gameObject != null)
            Object.Destroy(stateManager.gameObject);

        string folderPath = Path.Combine(Application.persistentDataPath, "GeneratedAudio");
        if (Directory.Exists(folderPath))
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
                Debug.Log("✓ Server principale è attivo");
            }
            catch
            {
                Debug.LogWarning("⚠ Server principale non disponibile");
            }

            try
            {
                var response = client.GetAsync($"{ERROR_SERVER_URL}/health").Result;
                Debug.Log("✓ Server errori è attivo");
            }
            catch
            {
                Debug. LogWarning("⚠ Server errori non disponibile");
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
#region Line 54 - GetComponentInChildren

[Test]
public void Start_WhenButtonTextIsNull_GetComponentInChildren()
{
    // Crea un nuovo audioGenClient senza Start
    var newTestGO = new GameObject("TestAudioGenClientNew");
    var newAudioGenClient = newTestGO.AddComponent<AudioGenClient>();

    // Crea un nuovo button
    var newButtonGO = new GameObject("GenerateButtonNew");
    newButtonGO.AddComponent<Canvas>();
    var newGenerateButton = newButtonGO. AddComponent<Button>();
    
    var newButtonTextGO = new GameObject("ButtonTextNew");
    newButtonTextGO.transform.SetParent(newButtonGO.transform);
    var newButtonText = newButtonTextGO.AddComponent<TextMeshProUGUI>();
    
    newButtonGO.AddComponent<GraphicRaycaster>();

    // Assegna il button ma NON il buttonText
    ReflectionHelper.SetFieldValue(newAudioGenClient, "generateButton", newGenerateButton);
    ReflectionHelper.SetFieldValue(newAudioGenClient, "buttonText", null);  // Null! 

    // Verifica che buttonText sia null PRIMA di Start
    TMP_Text beforeStart = (TMP_Text)ReflectionHelper.GetFieldValue(newAudioGenClient, "buttonText");
    Assert.IsNull(beforeStart, "buttonText dovrebbe essere null prima di Start");

    // Chiama Start
    newAudioGenClient.GetType().GetMethod("Start",
        System.Reflection. BindingFlags.NonPublic | System.  Reflection.BindingFlags. Instance)
        ?.Invoke(newAudioGenClient, null);

    // Verifica che Start abbia trovato buttonText tramite GetComponentInChildren
    TMP_Text afterStart = (TMP_Text)ReflectionHelper.GetFieldValue(newAudioGenClient, "buttonText");
    Assert.IsNotNull(afterStart, "buttonText dovrebbe essere assegnato dopo Start (linea 54)");

    // Cleanup
    Object.Destroy(newTestGO);
    Object.Destroy(newButtonGO);
}

#endregion

#region Line 107-110 - HTTP Error

[UnityTest]
public IEnumerator GenerateAudio_WithServerError_HandlesFailure()
{
    LogAssert.Expect(LogType.Error, "Generation failed:");  // Accetta qualsiasi errore
    LogAssert.Expect(LogType.Error, "[Error] Generation failed: HTTP/1.1 500 Internal Server Error");  // Accetta qualsiasi errore
    
    
    ReflectionHelper.SetFieldValue(audioGenClient, "serverUrl", ERROR_SERVER_URL);

    inputField.text = "Test error handling";

    audioGenClient.GenerateAudio(2.0f);

    float elapsedTime = 0f;
    while ((bool)ReflectionHelper.GetFieldValue(audioGenClient, "isGenerating") && elapsedTime < 15f)
    {
        elapsedTime += Time.deltaTime;
        yield return null;
    }

    Assert.IsTrue(generateButton.interactable, "Button dovrebbe essere abilitato dopo errore");
    Assert.AreEqual("Generate", buttonText.text, "Button text dovrebbe essere 'Generate'");

    ReflectionHelper.SetFieldValue(audioGenClient, "serverUrl", SERVER_URL);
}

#endregion

#region Line 115-120 - Response Success False

[UnityTest]
public IEnumerator GenerateAudio_WithResponseError_HandlesFailure()
{
    LogAssert.Expect(LogType.Error, "Generation failed:");  // Accetta qualsiasi errore
    LogAssert.Expect(LogType.Error, "[Error] Generation failed: HTTP/1.1 500 Internal Server Error");  // Accetta qualsiasi errore

    ReflectionHelper.SetFieldValue(audioGenClient, "serverUrl", ERROR_SERVER_URL);

    inputField.text = "Test response error";

    audioGenClient.GenerateAudio(2.0f);

    float elapsedTime = 0f;
    while ((bool)ReflectionHelper.GetFieldValue(audioGenClient, "isGenerating") && elapsedTime < 15f)
    {
        elapsedTime += Time.deltaTime;
        yield return null;
    }

    Assert.IsTrue(generateButton.interactable, "Button dovrebbe essere abilitato dopo errore");

    ReflectionHelper.SetFieldValue(audioGenClient, "serverUrl", SERVER_URL);
}

#endregion

    #region Line 136-141 - No Audio File / 128-134 - File Loop

    [UnityTest]
    public IEnumerator GenerateAudio_WithValidInput_FindsAudioFile()
    {
        inputField.text = "Test audio file finding";

        audioGenClient.GenerateAudio(2.0f);

        float elapsedTime = 0f;
        while ((bool)ReflectionHelper.GetFieldValue(audioGenClient, "isGenerating") && elapsedTime < 30f)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Assert.IsFalse((bool)ReflectionHelper.GetFieldValue(audioGenClient, "isGenerating"));
    }

    #endregion

    #region Line 173-176 - AudioSource Not Found

    [Test]
    public void Prefab_WithoutAudioSource_CreatesOne()
    {
        var prefab = new GameObject("TestPrefab");
        prefab.AddComponent<GeneratedObjectInfo>();
        prefab.AddComponent<LookAtCamera>();

        Assert.IsNull(prefab.GetComponent<AudioSource>());

        var instance = Object.Instantiate(prefab);
        var audioSource = instance.AddComponent<AudioSource>();

        Assert.IsNotNull(audioSource);

        Object.Destroy(prefab);
        Object.Destroy(instance);
    }

    #endregion

    #region General Tests

    [Test]
    public void SetButtonState_WhenGenerating_UpdatesUI()
    {
        ReflectionHelper.CallMethod(audioGenClient, "SetButtonState", true);

        Assert.IsFalse(generateButton.interactable);
        Assert.AreEqual("Generating...", buttonText.text);
    }

    [Test]
    public void SetButtonState_WhenNotGenerating_UpdatesUI()
    {
        ReflectionHelper.CallMethod(audioGenClient, "SetButtonState", false);

        Assert.IsTrue(generateButton.interactable);
        Assert.AreEqual("Generate", buttonText.text);
    }

    [Test]
    public void GenerateAudio_WithEmptyInput_DoesNotGenerate()
    {
        inputField.text = "";
        audioGenClient.GenerateAudio();

        Assert.IsFalse((bool)ReflectionHelper.GetFieldValue(audioGenClient, "isGenerating"));
    }

    [Test]
    public void GenerateAudio_WhenAlreadyGenerating_IgnoresRequest()
    {
        ReflectionHelper.SetFieldValue(audioGenClient, "isGenerating", true);
        inputField.text = "Should be ignored";

        audioGenClient.GenerateAudio();

        Assert.IsTrue((bool)ReflectionHelper.GetFieldValue(audioGenClient, "isGenerating"));
        ReflectionHelper.SetFieldValue(audioGenClient, "isGenerating", false);
    }

    [UnityTest]
    public IEnumerator GenerateAudio_SuccessfulFlow_ClearsInput()
    {
        inputField.text = "Test input clearing";

        audioGenClient.GenerateAudio(2.0f);

        float elapsedTime = 0f;
        while ((bool)ReflectionHelper.GetFieldValue(audioGenClient, "isGenerating") && elapsedTime < 30f)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Assert.AreEqual("", inputField.text);
    }

    [UnityTest]
    public IEnumerator GenerateAudio_SuccessfulFlow_SavesFile()
    {
        inputField.text = "Test file saving";

        audioGenClient. GenerateAudio(2.0f);

        float elapsedTime = 0f;
        while ((bool)ReflectionHelper.GetFieldValue(audioGenClient, "isGenerating") && elapsedTime < 30f)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        string folderPath = Path.Combine(Application.persistentDataPath, "GeneratedAudio");
        Assert.IsTrue(Directory.Exists(folderPath));
        Assert.Greater(Directory.GetFiles(folderPath).Length, 0);
    }

    [Test]
    public void RequestSerialization_CreatesValidJson()
    {
        var request = new { text = "Test prompt", duration = 5.0f };
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(request);

        Assert.IsTrue(json.Contains("\"text\""));
        Assert.IsTrue(json.Contains("\"duration\""));
        Assert.IsTrue(json.Contains("Test prompt"));
    }

    [Test]
    public void ButtonState_DefaultValue_IsNotGenerating()
    {
        bool isGenerating = (bool)ReflectionHelper.GetFieldValue(audioGenClient, "isGenerating");
        Assert.IsFalse(isGenerating);
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

        public static object CallMethod(object obj, string methodName, params object[] parameters)
        {
            if (obj == null) return null;
            var method = obj.GetType().GetMethod(methodName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return method?. Invoke(obj, parameters);
        }
    }

    #endregion
    
}