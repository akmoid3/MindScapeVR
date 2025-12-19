using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class MindfulnessAudioManagerTests
{
    private GameObject _listenerGO;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        // Evita warning: "There are no audio listeners in the scene"
        if (Object.FindAnyObjectByType<AudioListener>() == null)
        {
            _listenerGO = new GameObject("TestAudioListener");
            _listenerGO.AddComponent<AudioListener>();
        }

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (_listenerGO != null)
            Object.Destroy(_listenerGO);

        yield return null;
    }

    private static AudioClip CreateSilentClip(string name, float seconds = 0.05f, int frequency = 44100)
    {
        int samples = Mathf.CeilToInt(seconds * frequency);
        return AudioClip.Create(name, samples, 1, frequency, false);
    }

    private static void SetPrivateField<T>(object obj, string fieldName, T value)
    {
        var f = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.IsNotNull(f, $"Field '{fieldName}' not found via reflection.");
        f.SetValue(obj, value);
    }

    private static T GetPrivateField<T>(object obj, string fieldName)
    {
        var f = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.IsNotNull(f, $"Field '{fieldName}' not found via reflection.");
        return (T)f.GetValue(obj);
    }

    [UnityTest]
    public IEnumerator Awake_AddsAudioSource_IfMissing()
    {
        var go = new GameObject("MindfulnessAudioManager");
        Assert.IsNull(go.GetComponent<AudioSource>());

        go.AddComponent<MindfulnessAudioManager>();
        yield return null;

        Assert.IsNotNull(go.GetComponent<AudioSource>());

        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator StartSpeech_StartsPlayingAudio_WithFirstNonNullClip()
    {
        var go = new GameObject("MindfulnessAudioManager");
        var mgr = go.AddComponent<MindfulnessAudioManager>();
        yield return null;

        var clip1 = CreateSilentClip("clip1", 0.2f);

        // mettiamo anche un null per verificare lo skip
        SetPrivateField(mgr, "audioClips", new List<AudioClip> { null, clip1 });
        SetPrivateField(mgr, "timeBetweenClips", 0f);

        mgr.StartSpeech();

        // Attendi un frame: la coroutine parte e deve chiamare Play()
        yield return null;

        var audio = go.GetComponent<AudioSource>();
        Assert.AreSame(clip1, audio.clip);
        Assert.IsTrue(audio.isPlaying);

        // speechCoroutine dovrebbe essere attiva mentre sta suonando
        Assert.IsNotNull(GetPrivateField<Coroutine>(mgr, "speechCoroutine"));

        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator StopSpeech_StopsAudio_AndClearsCoroutine()
    {
        var go = new GameObject("MindfulnessAudioManager");
        var mgr = go.AddComponent<MindfulnessAudioManager>();
        yield return null;

        var clip = CreateSilentClip("clip", 0.5f);
        SetPrivateField(mgr, "audioClips", new List<AudioClip> { clip });
        SetPrivateField(mgr, "timeBetweenClips", 1f);

        mgr.StartSpeech();
        yield return null; // allow Play

        var audio = go.GetComponent<AudioSource>();
        Assert.IsTrue(audio.isPlaying);
        Assert.IsNotNull(GetPrivateField<Coroutine>(mgr, "speechCoroutine"));

        mgr.StopSpeech();

        Assert.IsFalse(audio.isPlaying);
        Assert.IsNull(GetPrivateField<Coroutine>(mgr, "speechCoroutine"));

        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator StartSpeechCoroutine_CompletesAndClearsSpeechCoroutine()
    {
        var go = new GameObject("MindfulnessAudioManager");
        var mgr = go.AddComponent<MindfulnessAudioManager>();
        yield return null;

        var clip1 = CreateSilentClip("clip1", 0.05f);
        var clip2 = CreateSilentClip("clip2", 0.05f);

        SetPrivateField(mgr, "audioClips", new List<AudioClip> { clip1, null, clip2 });
        SetPrivateField(mgr, "timeBetweenClips", 0.01f);

        mgr.StartSpeech();

        // Aspetta abbastanza per: clip1 + pausa + clip2 + pausa
        yield return new WaitForSeconds(0.05f + 0.01f + 0.05f + 0.01f + 0.05f);

        Assert.IsNull(GetPrivateField<Coroutine>(mgr, "speechCoroutine"));

        Object.Destroy(go);
    }
    
    [UnityTest]
    public IEnumerator Start_AddsAudioSource_IfMissing_AfterAwake()
    {
        // 1) Crea inattivo così Start() non parte subito
        var go = new GameObject("MindfulnessAudioManager_StartNull");
        go.SetActive(false);

        // 2) Aggiungi manager -> Awake() gira anche se inattivo? (su GameObject inattivo Awake non gira)
        // Quindi: per forzare la sequenza Awake->(rimuovi)->Start, facciamo:
        // - attivo per far girare Awake
        // - poi disattivo prima del frame di Start (Start gira al primo frame in cui è attivo)
        go.SetActive(true);
        var mgr = go.AddComponent<MindfulnessAudioManager>();

        // Awake su componenti aggiunti a GO attivo viene chiamato subito
        var audioFromAwake = go.GetComponent<AudioSource>();
        Assert.IsNotNull(audioFromAwake, "Awake should have ensured an AudioSource exists.");

        // 3) Disattiva per impedire Start nel prossimo frame, rimuovi AudioSource
        go.SetActive(false);
        Object.DestroyImmediate(audioFromAwake);
        Assert.IsNull(go.GetComponent<AudioSource>(), "AudioSource must be missing before Start().");

        // 4) Riattiva e aspetta 1 frame -> Start() deve aggiungere AudioSource (branch null)
        go.SetActive(true);
        yield return null;

        Assert.IsNotNull(go.GetComponent<AudioSource>(), "Start() should add an AudioSource when missing.");

        Object.Destroy(go);
    }
    
}