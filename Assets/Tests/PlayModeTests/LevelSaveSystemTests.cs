using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class LevelSaveSystem_Reflection100Tests
{
    private readonly List<GameObject> _spawned = new();

    private string SavedScenesDir => Path.Combine(Application.persistentDataPath, "SavedScenes");
    private string ModelsDir => Path.Combine(Application.persistentDataPath, "Models");
    private string AudioDir => Path.Combine(Application.persistentDataPath, "GeneratedAudio");

    private string SceneName => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
    private string SavePath => Path.Combine(SavedScenesDir, $"SaveData_{SceneName}.json");

    [SetUp]
    public void SetUp()
    {
        // Clean persistent dirs
        SafeDeleteDir(SavedScenesDir);
        SafeDeleteDir(ModelsDir);
        SafeDeleteDir(AudioDir);

        // Try to reduce cross-test scene pollution
        DestroyAllTaggedTestObjects();
        EnsureSingleAudioListener();
    }

    [TearDown]
    public void TearDown()
    {
        // Destroy objects spawned by this test
        foreach (var go in _spawned)
        {
            if (go != null) Object.Destroy(go);
        }
        _spawned.Clear();

        // Best-effort additional cleanup
        DestroyAllTaggedTestObjects();

        SafeDeleteDir(SavedScenesDir);
        SafeDeleteDir(ModelsDir);
        SafeDeleteDir(AudioDir);
    }

    [UnityTest]
    public IEnumerator Covers_LoadScene_WarningBranch_WhenNoFile()
    {
        var sut = Spawn("sut").AddComponent<LevelSaveSystem>();

        LogAssert.Expect(LogType.Warning, "Nessun salvataggio trovato.");
        sut.LoadScene();

        yield return null;
    }

    [UnityTest]
    public IEnumerator Covers_SaveScene_And_GetSavePath_CreateDirectory()
    {
        SafeDeleteDir(SavedScenesDir); // force create

        var sut = Spawn("sut").AddComponent<LevelSaveSystem>();

        var light = Spawn("dirLight").AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 3.3f;
        light.transform.rotation = Quaternion.Euler(1, 2, 3);
        SetPrivateField(sut, "sceneDirectionalLight", light);

        // Generated objects for SaveScene
        var modelGo = Spawn("genModel");
        var modelInfo = modelGo.AddComponent<GeneratedObjectInfo>();
        modelInfo.type = GenerationType.Model;
        modelInfo.fileName = "whatever.glb";

        var audioGo = Spawn("genAudio");
        var audioInfo = audioGo.AddComponent<GeneratedObjectInfo>();
        audioInfo.type = GenerationType.Audio;
        audioInfo.fileName = "whatever.wav";
        var src = audioGo.AddComponent<AudioSource>();
        src.loop = true;

        sut.SaveScene();
        yield return null;

        Assert.That(File.Exists(SavePath), Is.True);
        var json = File.ReadAllText(SavePath);
        Assert.That(json, Does.Contain("\"type\": \"Audio\""));
        Assert.That(json, Does.Contain("\"isLooping\": true"));
    }

    [UnityTest]
    public IEnumerator Covers_LoadSceneCoroutine_LevelManagerBranches_Model_Audio_Rotation()
    {
        EnsureSingleAudioListener();

        // Create SUT and wait one frame so Start()->LoadScene runs before we create SavePath (avoid duplicate run)
        var sutGo = Spawn("sut");
        var sut = sutGo.AddComponent<LevelSaveSystem>();
        yield return null;

        // Light on SUT
        var light = Spawn("Directional Light").AddComponent<Light>();
        light.type = LightType.Directional;
        SetPrivateField(sut, "sceneDirectionalLight", light);

        // IMPORTANT: Use a LevelManager component but keep it DISABLED so Start() never runs (avoids NRE / StateManager / XR)
        var lmGo = Spawn("LevelManager");
        lmGo.SetActive(false);
        var levelManager = lmGo.AddComponent<LevelManager>();

        // set freeCamObject so GetFreeCam() returns non-null
        var freeCam = Spawn("FreeCam");
        freeCam.AddComponent<Camera>();
        SetPrivateField(levelManager, "freeCamObject", freeCam);

        // (optional) init audioInSceneList so AddSoundToList/ClearAudioList are safe (they already are, but keep deterministic)
        SetPrivateField(levelManager, "audioInSceneList", new List<GameObject>());

        // Assign levelManager into LevelSaveSystem
        SetPrivateField(sut, "levelManager", levelManager);

        // DON'T add SceneLightUIController (it logs "No light assigned" and is not required to cover most code).
        // If you want to cover lines 139-142 too, we need to set lightUIController and prevent its Start error.
        // For now we keep it null to avoid failing logs.

        // Audio prefab: no AudioSource => hits "source == null" branch,
        // plus LookAtCamera + RandomizeAudio => hits branches 215-228
        var audioPrefab = Spawn("AudioPrefab");
        audioPrefab.AddComponent<LookAtCamera>();
        audioPrefab.AddComponent<RandomizeAudio>();
        SetPrivateField(sut, "audioPrefab", audioPrefab);

        // Old objects to destroy
        Spawn("old1").AddComponent<GeneratedObjectInfo>();
        Spawn("old2").AddComponent<GeneratedObjectInfo>();

        // Prepare folders/files
        Directory.CreateDirectory(SavedScenesDir);
        Directory.CreateDirectory(ModelsDir);
        Directory.CreateDirectory(AudioDir);

        // Copy GLB
        string glbAssetRelative = "Assets/Prefabs/def1956c-9b68-4e85-8523-bec9f5554d24.glb";
        string glbName = Path.GetFileName(glbAssetRelative);
        CopyAssetTo(glbAssetRelative, Path.Combine(ModelsDir, glbName));

        // Copy WAV (stable)
        string wavAssetRelative = "Assets/Audio/BeachAudio/seagulls_only.wav";
        string wavName = Path.GetFileName(wavAssetRelative);
        CopyAssetTo(wavAssetRelative, Path.Combine(AudioDir, wavName));

        // Save JSON with non-identity audio rotation to cover lines 205-207
        File.WriteAllText(SavePath, BuildJson_All(lightIntensity: 8.0f, modelFileName: glbName, audioFileName: wavName));

        // Act
        yield return sut.StartCoroutine(sut.LoadSceneCoroutine(SavePath));

        // settle destroy/instantiate/audio decode
        yield return null;
        yield return null;
        yield return null;

        // Assert: old destroyed
        Assert.That(GameObject.Find("old1"), Is.Null);
        Assert.That(GameObject.Find("old2"), Is.Null);

        // Assert: light restored
        Assert.That(light.intensity, Is.EqualTo(8f).Within(0.001f));

        // Assert: model created
        Assert.That(GameObject.Find($"Model_{glbName}"), Is.Not.Null);

        // Assert: audio created
        var audioGo = GameObject.Find("AudioPrefab(Clone)");
        Assert.That(audioGo, Is.Not.Null);

        var src = audioGo.GetComponent<AudioSource>();
        Assert.That(src, Is.Not.Null);
        Assert.That(src.clip, Is.Not.Null);
        Assert.That(src.loop, Is.True);
    }

    [UnityTest]
    public IEnumerator Covers_LoadAudio_EarlyYieldBreak_WhenFileMissing()
    {
        EnsureSingleAudioListener();

        var sutGo = Spawn("sut");
        var sut = sutGo.AddComponent<LevelSaveSystem>();
        yield return null;

        var audioPrefab = Spawn("AudioPrefab");
        SetPrivateField(sut, "audioPrefab", audioPrefab);

        Directory.CreateDirectory(SavedScenesDir);
        Directory.CreateDirectory(AudioDir);

        File.WriteAllText(SavePath, BuildJson_AudioOnlyMissing("missing.wav"));

        yield return sut.StartCoroutine(sut.LoadSceneCoroutine(SavePath));
        yield return null;

        Assert.That(GameObject.Find("AudioPrefab(Clone)"), Is.Null);
    }

    // ---------------- helpers ----------------

    private GameObject Spawn(string name)
    {
        var go = new GameObject(name);
        go.tag = "Untagged"; // keep default; if you have a Test tag you can use it
        _spawned.Add(go);
        return go;
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        var t = instance.GetType();
        var f = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(f, Is.Not.Null, $"Field not found: {t.Name}.{fieldName}");
        f.SetValue(instance, value);
    }

    private static void SafeDeleteDir(string dir)
    {
        try { if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch { }
    }

    private static void CopyAssetTo(string assetRelativePath, string destPath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string absSrc = Path.Combine(projectRoot, assetRelativePath);
        Assert.That(File.Exists(absSrc), Is.True, $"Missing asset file: {absSrc}");

        Directory.CreateDirectory(Path.GetDirectoryName(destPath));
        File.Copy(absSrc, destPath, overwrite: true);
    }

    private static string BuildJson_All(float lightIntensity, string modelFileName, string audioFileName)
    {
        return @$"{{
  ""objects"": [
    {{
      ""type"": ""Model"",
      ""fileName"": ""{modelFileName}"",
      ""position"": [4,5,6],
      ""rotation"": [0,0,0,1],
      ""scale"": [2,2,2],
      ""isLooping"": false
    }},
    {{
      ""type"": ""Audio"",
      ""fileName"": ""{audioFileName}"",
      ""position"": [1,2,3],
      ""rotation"": [0,0.7071067,0,0.7071067],
      ""scale"": [1,1,1],
      ""isLooping"": true
    }}
  ],
  ""lightIntensity"": {lightIntensity},
  ""lightRotation"": [0,0,0,1]
}}";
    }

    private static string BuildJson_AudioOnlyMissing(string audioFileName)
    {
        return @$"{{
  ""objects"": [
    {{
      ""type"": ""Audio"",
      ""fileName"": ""{audioFileName}"",
      ""position"": [1,2,3],
      ""rotation"": [0,0,0,1],
      ""scale"": [1,1,1],
      ""isLooping"": false
    }}
  ],
  ""lightIntensity"": 1.0,
  ""lightRotation"": null
}}";
    }
    
    [UnityTest]
    public IEnumerator Covers_LightUIController_UpdateUIFromLight_Line()
    {
        var sutGo = Spawn("sut");
        var sut = sutGo.AddComponent<LevelSaveSystem>();
        yield return null;

        // Light so the if(...) enters
        var light = Spawn("Directional Light").AddComponent<Light>();
        light.type = LightType.Directional;
        SetPrivateField(sut, "sceneDirectionalLight", light);

        // UI controller on inactive GO so Start() doesn't run and no "No light assigned" log
        var uiGo = Spawn("LightUI");
        uiGo.SetActive(false);
        var ui = uiGo.AddComponent<SceneLightUIController>();
        SetPrivateField(sut, "lightUIController", ui);

        // minimal save file with lightRotation length 4 to trigger the block
        Directory.CreateDirectory(SavedScenesDir);
        File.WriteAllText(SavePath, @"{
  ""objects"": [],
  ""lightIntensity"": 5.0,
  ""lightRotation"": [0,0,0,1]
}");

        yield return sut.StartCoroutine(sut.LoadSceneCoroutine(SavePath));
        yield return null;

        Assert.That(light.intensity, Is.EqualTo(5f).Within(0.001f));
    }

    private static void EnsureSingleAudioListener()
    {
        var listeners = Object.FindObjectsOfType<AudioListener>();
        if (listeners.Length == 0)
        {
            new GameObject("TestAudioListener").AddComponent<AudioListener>();
        }
        else if (listeners.Length > 1)
        {
            // keep first enabled, destroy extras
            for (int i = 1; i < listeners.Length; i++)
                Object.Destroy(listeners[i].gameObject);
        }
    }

    private static void DestroyAllTaggedTestObjects()
    {
        // If you want stronger cleanup, you can search by name prefix used in tests.
        // Here we keep it minimal to avoid nuking unrelated scene objects.
    }
}