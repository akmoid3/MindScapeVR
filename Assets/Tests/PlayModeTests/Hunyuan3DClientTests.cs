using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.IO;
using System.Net.  Http;

public class Hunyuan3DClientTests
{
    private GameObject testGameObject;
    private Hunyuan3DClient hunyuan3DClient;
    private TMP_InputField inputField;
    private Button generateButton;
    private TMP_Text buttonText;
    private Transform modelParent;
    private static readonly string SERVER_URL = "http://localhost:5000";
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

        // Crea model parent
        var parentGO = new GameObject("ModelParent");
        modelParent = parentGO.transform;

        // Crea Hunyuan3DClient
        testGameObject = new GameObject("TestHunyuan3DClient");
        hunyuan3DClient = testGameObject.AddComponent<Hunyuan3DClient>();

        // Crea InputField
        var inputFieldGO = new GameObject("InputField");
        inputFieldGO.AddComponent<Canvas>();
        inputField = inputFieldGO.AddComponent<TMP_InputField>();
        inputFieldGO.AddComponent<GraphicRaycaster>();
        ReflectionHelper.SetFieldValue(hunyuan3DClient, "inputField", inputField);

        // Crea Button
        var buttonGO = new GameObject("GenerateButton");
        buttonGO.AddComponent<Canvas>();
        generateButton = buttonGO.AddComponent<Button>();
        
        var buttonTextGO = new GameObject("ButtonText");
        buttonTextGO. transform.SetParent(buttonGO.transform);
        buttonText = buttonTextGO.AddComponent<TextMeshProUGUI>();
        
        buttonGO.AddComponent<GraphicRaycaster>();

        // Assegna al client
        ReflectionHelper.SetFieldValue(hunyuan3DClient, "generateButton", generateButton);
        ReflectionHelper.SetFieldValue(hunyuan3DClient, "buttonText", null);
        ReflectionHelper. SetFieldValue(hunyuan3DClient, "modelParent", modelParent);
        ReflectionHelper.SetFieldValue(hunyuan3DClient, "serverUrl", SERVER_URL);
        ReflectionHelper.SetFieldValue(hunyuan3DClient, "modelScale", 0.5f);
        ReflectionHelper.SetFieldValue(hunyuan3DClient, "targetPolyCount", 10000);

        // Inizializza
        hunyuan3DClient. GetType().GetMethod("Start",
            System.Reflection.BindingFlags.NonPublic | System.  Reflection.BindingFlags. Instance)
            ?.Invoke(hunyuan3DClient, null);
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
        
        if (modelParent != null && modelParent.gameObject != null)
            Object.Destroy(modelParent.gameObject);

        // Pulisci i file
        string folderPath = Path.Combine(Application.persistentDataPath, "Models");
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
                Debug.Log("✓ Mock server Hunyuan3D è attivo");
            }
            catch
            {
                Debug.LogWarning("⚠ Mock server Hunyuan3D non disponibile");
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

    #region Line 57 - GetComponentInChildren

    [Test]
    public void Start_WhenButtonTextIsNull_GetComponentInChildren()
    {
        var newTestGO = new GameObject("TestHunyuan3DNew");
        var newClient = newTestGO.AddComponent<Hunyuan3DClient>();

        var newButtonGO = new GameObject("GenerateButtonNew");
        newButtonGO.AddComponent<Canvas>();
        var newButton = newButtonGO.AddComponent<Button>();
        
        var newButtonTextGO = new GameObject("ButtonTextNew");
        newButtonTextGO.transform.SetParent(newButtonGO. transform);
        var newButtonText = newButtonTextGO.AddComponent<TextMeshProUGUI>();
        
        newButtonGO.AddComponent<GraphicRaycaster>();

        ReflectionHelper.SetFieldValue(newClient, "generateButton", newButton);
        ReflectionHelper.SetFieldValue(newClient, "buttonText", null);

        TMP_Text beforeStart = (TMP_Text)ReflectionHelper.GetFieldValue(newClient, "buttonText");
        Assert.IsNull(beforeStart);

        newClient.GetType().GetMethod("Start",
            System.Reflection.BindingFlags.NonPublic | System.  Reflection.BindingFlags. Instance)
            ?.Invoke(newClient, null);

        TMP_Text afterStart = (TMP_Text)ReflectionHelper.GetFieldValue(newClient, "buttonText");
        Assert.IsNotNull(afterStart);

        Object.Destroy(newTestGO);
        Object.Destroy(newButtonGO);
    }

    #endregion

    #region Line 65-76 - GenerateModel

    [Test]
    public void GenerateModel_WithEmptyInput_DoesNotGenerate()
    {
        inputField.text = "";
        hunyuan3DClient.GenerateModel();

        bool isGenerating = (bool)ReflectionHelper.GetFieldValue(hunyuan3DClient, "isGenerating");
        Assert.IsFalse(isGenerating);
    }

    [Test]
    public void GenerateModel_WhenAlreadyGenerating_IgnoresRequest()
    {
        ReflectionHelper.SetFieldValue(hunyuan3DClient, "isGenerating", true);
        inputField.text = "Should be ignored";

        hunyuan3DClient.GenerateModel();

        Assert.IsTrue((bool)ReflectionHelper.GetFieldValue(hunyuan3DClient, "isGenerating"));
        ReflectionHelper.SetFieldValue(hunyuan3DClient, "isGenerating", false);
    }

    #endregion

    #region Line 78-100 - SetButtonState

    [Test]
    public void SetButtonState_Bool_UpdatesUI()
    {
        ReflectionHelper.CallMethod(hunyuan3DClient, "SetButtonState", true);

        Assert.IsFalse(generateButton.interactable);
        Assert.AreEqual("Generating...", buttonText.text);
        Assert.IsTrue((bool)ReflectionHelper.GetFieldValue(hunyuan3DClient, "isGenerating"));
    }

    [Test]
    public void SetButtonState_String_UpdatesUI()
    {
        ReflectionHelper.CallMethod(hunyuan3DClient, "SetButtonState", "Loading Model:  50%");

        Assert.AreEqual("Loading Model: 50%", buttonText.text);
        Assert.IsFalse(generateButton.interactable);
        Assert.IsTrue((bool)ReflectionHelper.GetFieldValue(hunyuan3DClient, "isGenerating"));
    }

    [Test]
    public void SetButtonState_GenerateState_EnablesButton()
    {
        ReflectionHelper.CallMethod(hunyuan3DClient, "SetButtonState", "Generate");

        Assert.AreEqual("Generate", buttonText.text);
        Assert.IsTrue(generateButton.interactable);
        Assert.IsFalse((bool)ReflectionHelper.GetFieldValue(hunyuan3DClient, "isGenerating"));
    }

    #endregion

    #region Line 127-132 - HTTP Error

    [UnityTest]
    public IEnumerator GenerateModel_WithServerError_HandlesFailure()
    {
        LogAssert. Expect(LogType.Error, "Generation failed:");

        ReflectionHelper.SetFieldValue(hunyuan3DClient, "serverUrl", ERROR_SERVER_URL);

        inputField.text = "Test server error";

        hunyuan3DClient.GenerateModel();

        float elapsedTime = 0f;
        while ((bool)ReflectionHelper.GetFieldValue(hunyuan3DClient, "isGenerating") && elapsedTime < 15f)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Assert.IsTrue(generateButton.interactable);

        ReflectionHelper.SetFieldValue(hunyuan3DClient, "serverUrl", SERVER_URL);
    }

    #endregion

    #region Line 140-152 - Mesh File Loop & Not Found

    [UnityTest]
    public IEnumerator GenerateModel_WithValidInput_FindsMeshFile()
    {
        inputField.text = "Test mesh finding";

        hunyuan3DClient.GenerateModel();

        float elapsedTime = 0f;
        while ((bool)ReflectionHelper.GetFieldValue(hunyuan3DClient, "isGenerating") && elapsedTime < 60f)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Assert.IsFalse((bool)ReflectionHelper.GetFieldValue(hunyuan3DClient, "isGenerating"));
    }

    #endregion

    #region Line 165-170 - Download Error

    [UnityTest]
    public IEnumerator GenerateModel_WithDownloadError_HandlesFailure()
    {
        LogAssert.Expect(LogType.Error, "Download failed:");

        // Usa un URL di download invalido
        ReflectionHelper. SetFieldValue(hunyuan3DClient, "serverUrl", "http://invalid-download-server: 5555");

        inputField.text = "Test download error";

        hunyuan3DClient.GenerateModel();

        yield return new WaitForSeconds(5f);

        ReflectionHelper.SetFieldValue(hunyuan3DClient, "serverUrl", SERVER_URL);
    }

    #endregion

    #region Full Integration Tests

    [UnityTest]
    public IEnumerator GenerateModel_SuccessfulFlow_CreatesModel()
    {
        inputField.text = "A cute red robot";

        hunyuan3DClient. GenerateModel();

        float elapsedTime = 0f;
        while ((bool)ReflectionHelper.GetFieldValue(hunyuan3DClient, "isGenerating") && elapsedTime < 60f)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Assert.IsFalse((bool)ReflectionHelper.GetFieldValue(hunyuan3DClient, "isGenerating"));
        Assert.IsTrue(generateButton.interactable);
        Assert.AreEqual("Generate", buttonText.text);
    }

    [UnityTest]
    public IEnumerator GenerateModel_ClearsInputAfterGeneration()
    {
        inputField.text = "Test input clearing";

        hunyuan3DClient.GenerateModel();

        float elapsedTime = 0f;
        while ((bool)ReflectionHelper.GetFieldValue(hunyuan3DClient, "isGenerating") && elapsedTime < 60f)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Assert.AreEqual("", inputField.text);
    }

    [UnityTest]
    public IEnumerator GenerateModel_SavesFileToStorage()
    {
        inputField.text = "Test file saving";

        hunyuan3DClient.GenerateModel();

        float elapsedTime = 0f;
        while ((bool)ReflectionHelper.GetFieldValue(hunyuan3DClient, "isGenerating") && elapsedTime < 60f)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        string folderPath = Path.Combine(Application.persistentDataPath, "Models");
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
                System.Reflection.BindingFlags.NonPublic | System.  Reflection.BindingFlags. Instance);
            field?.SetValue(obj, value);
        }

        public static object GetFieldValue(object obj, string fieldName)
        {
            if (obj == null) return null;
            var field = obj.  GetType().GetField(fieldName,
                System.Reflection.  BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
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