using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class RandomizeAudioTests
{
    private static AudioClip CreateSilentClip(float seconds = 0.1f, int frequency = 44100)
    {
        int samples = Mathf.CeilToInt(seconds * frequency);
        var clip = AudioClip.Create("silent", samples, 1, frequency, false);
        // silent by default
        return clip;
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
    public IEnumerator Start_AssignsAudioSource_FromGetComponent()
    {
        var go = new GameObject("RandomizeAudio");
        var audio = go.AddComponent<AudioSource>();
        var ra = go.AddComponent<RandomizeAudio>();

        yield return null; // Start()

        var source = GetPrivateField<AudioSource>(ra, "source");
        Assert.AreSame(audio, source);

        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator EnableCollider_TogglesColliderEnabled()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); // has MeshRenderer + BoxCollider
        var ra = go.AddComponent<RandomizeAudio>();
        go.AddComponent<AudioSource>();

        yield return null; // Start() caches collider

        ra.EnableCollider(false);
        Assert.IsFalse(go.GetComponent<Collider>().enabled);

        ra.EnableCollider(true);
        Assert.IsTrue(go.GetComponent<Collider>().enabled);

        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator StartSoundRandomly_WhenLoopTrue_PlaysImmediately()
    {
        var go = new GameObject("RandomizeAudio");
        var audio = go.AddComponent<AudioSource>();
        audio.clip = CreateSilentClip();
        audio.loop = true;

        var ra = go.AddComponent<RandomizeAudio>();
        yield return null; // Start()

        ra.StartSoundRandomly();

        Assert.IsTrue(audio.isPlaying);

        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator StartSoundRandomly_WhenNotLoop_StartsCoroutine_AndSetsSoundRoutine()
    {
        var go = new GameObject("RandomizeAudio");
        var audio = go.AddComponent<AudioSource>();
        audio.clip = CreateSilentClip();
        audio.loop = false;

        var ra = go.AddComponent<RandomizeAudio>();
        yield return null; // Start()

        ra.StartSoundRandomly();

        // coroutine gets assigned immediately
        var routine = GetPrivateField<Coroutine>(ra, "soundRoutine");
        Assert.IsNotNull(routine);

        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator StartSoundRandomly_DoesNotStartSecondCoroutine_WhenAlreadyRunning()
    {
        var go = new GameObject("RandomizeAudio");
        var audio = go.AddComponent<AudioSource>();
        audio.clip = CreateSilentClip();
        audio.loop = false;

        var ra = go.AddComponent<RandomizeAudio>();
        yield return null; // Start()

        ra.StartSoundRandomly();
        var routine1 = GetPrivateField<Coroutine>(ra, "soundRoutine");
        Assert.IsNotNull(routine1);

        ra.StartSoundRandomly();
        var routine2 = GetPrivateField<Coroutine>(ra, "soundRoutine");

        Assert.AreSame(routine1, routine2);

        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator StopRandomSound_StopsCoroutine_AndStopsAudio()
    {
        var go = new GameObject("RandomizeAudio");
        var audio = go.AddComponent<AudioSource>();
        audio.clip = CreateSilentClip();
        audio.loop = false;

        var ra = go.AddComponent<RandomizeAudio>();
        yield return null; // Start()

        // Start coroutine path
        ra.StartSoundRandomly();
        Assert.IsNotNull(GetPrivateField<Coroutine>(ra, "soundRoutine"));

        // Force audio to be "playing" to verify Stop() is called
        audio.Play();
        Assert.IsTrue(audio.isPlaying);

        ra.StopRandomSound();

        Assert.IsNull(GetPrivateField<Coroutine>(ra, "soundRoutine"));
        Assert.IsFalse(audio.isPlaying);

        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator StartSoundRandomlyCoroutine_WaitsAndPlays_SetsVolumeInRange()
    {
        var go = new GameObject("RandomizeAudio");
        var audio = go.AddComponent<AudioSource>();
        audio.clip = CreateSilentClip(seconds: 0.2f);
        audio.loop = false;

        var ra = go.AddComponent<RandomizeAudio>();
        yield return null; // Start()

        // Make delays tiny so the test finishes quickly
        SetPrivateField(ra, "minDelay", 0.01f);
        SetPrivateField(ra, "maxDelay", 0.02f);

        // Start the coroutine directly so we can stop after first play
        var enumerator = ra.StartSoundRandomlyCoroutine();
        ra.StartCoroutine(enumerator);

        // Wait a bit more than maxDelay to allow first iteration to execute
        yield return new WaitForSeconds(0.05f);

        Assert.That(audio.volume, Is.InRange(0.1f, 1.0f));
        Assert.IsTrue(audio.isPlaying);

        // Cleanup: stop all coroutines started by this MonoBehaviour
        ra.StopAllCoroutines();
        Object.Destroy(go);
    }
}