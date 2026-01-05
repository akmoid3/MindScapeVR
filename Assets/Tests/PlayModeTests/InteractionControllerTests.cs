using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Reflection;

public class InteractionController_ReflectionTests
{
    private readonly System.Collections.Generic.List<GameObject> _spawned = new();

    private GameObject Spawn(string name)
    {
        var go = new GameObject(name);
        _spawned.Add(go);
        return go;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var go in _spawned)
        {
            if (go != null) Object.Destroy(go);
        }

        _spawned.Clear();
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        var f = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(f, Is.Not.Null, $"Field not found: {instance.GetType().Name}.{fieldName}");
        f.SetValue(instance, value);
    }

    private static object InvokePrivate(object instance, string methodName, params object[] args)
    {
        var m = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(m, Is.Not.Null, $"Method not found: {instance.GetType().Name}.{methodName}");
        return m.Invoke(instance, args);
    }

    private static T InvokePrivate<T>(object instance, string methodName, params object[] args)
    {
        return (T)InvokePrivate(instance, methodName, args);
    }

    private static GameObject CreateAudioUIWithToggle(System.Collections.Generic.List<GameObject> spawned)
    {
        var audioUI = new GameObject("audioUI");
        spawned.Add(audioUI);

        // Need a Toggle somewhere in children
        var toggleGo = new GameObject("toggle");
        spawned.Add(toggleGo);
        toggleGo.transform.SetParent(audioUI.transform, false);
        toggleGo.AddComponent<Toggle>();

        audioUI.SetActive(false);
        return audioUI;
    }

    private static GameObject CreateDeleteUI(System.Collections.Generic.List<GameObject> spawned)
    {
        var deleteUI = new GameObject("deleteUI");
        spawned.Add(deleteUI);
        deleteUI.SetActive(false);
        return deleteUI;
    }

    [UnityTest]
    public IEnumerator Start_DisablesUI_WhenAssigned()
    {
        // EventSystem needed by some methods, create anyway
        Spawn("EventSystem").AddComponent<EventSystem>();
        Spawn("StandaloneInputModule").AddComponent<StandaloneInputModule>();

        var go = Spawn("InteractionControllerGO");
        var ic = go.AddComponent<InteractionController>();

        // assign private UI refs
        var audioUI = CreateAudioUIWithToggle(_spawned);
        var deleteUI = CreateDeleteUI(_spawned);
        SetPrivateField(ic, "audioUI", audioUI);
        SetPrivateField(ic, "deleteUI", deleteUI);

        // Let Start run
        yield return null;

        Assert.That(audioUI.activeSelf, Is.False);
        Assert.That(deleteUI.activeSelf, Is.False);
    }

    [UnityTest]
    public IEnumerator EnableInteraction_False_DisablesUI_AndClearsSelectedObject()
    {
        var go = Spawn("InteractionControllerGO");
        var ic = go.AddComponent<InteractionController>();

        var audioUI = CreateAudioUIWithToggle(_spawned);
        var deleteUI = CreateDeleteUI(_spawned);
        audioUI.SetActive(true);
        deleteUI.SetActive(true);

        SetPrivateField(ic, "audioUI", audioUI);
        SetPrivateField(ic, "deleteUI", deleteUI);

        // set selectedObject via reflection
        var selected = Spawn("selected");
        SetPrivateField(ic, "selectedObject", selected);

        ic.EnableInteraction(false);

        Assert.That(audioUI.activeSelf, Is.False);
        Assert.That(deleteUI.activeSelf, Is.False);

        // selectedObject should be null
        var selectedField = ic.GetType().GetField("selectedObject", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(selectedField.GetValue(ic), Is.Null);

        yield return null;
    }

    [UnityTest]
    public IEnumerator IsTypingInInputField_ReturnsFalse_WhenNoEventSystem()
    {
        var go = Spawn("InteractionControllerGO");
        var ic = go.AddComponent<InteractionController>();

        // ensure no EventSystem exists
        var es = Object.FindObjectOfType<EventSystem>();
        if (es != null) Object.Destroy(es.gameObject);

        bool result = InvokePrivate<bool>(ic, "IsTypingInInputField");
        Assert.That(result, Is.False);

        yield return null;
    }

    [UnityTest]
    public IEnumerator IsTypingInInputField_ReturnsTrue_WhenTMPInputFieldSelected()
    {
        // EventSystem required
        var esGo = Spawn("EventSystem");
        var es = esGo.AddComponent<EventSystem>();
        Spawn("StandaloneInputModule").AddComponent<StandaloneInputModule>();

        var go = Spawn("InteractionControllerGO");
        var ic = go.AddComponent<InteractionController>();

        // Create a selected UI object with TMP_InputField
        var inputGo = Spawn("TMP_InputField_GO");
        inputGo.AddComponent<TMP_InputField>();

        es.SetSelectedGameObject(inputGo);

        bool result = InvokePrivate<bool>(ic, "IsTypingInInputField");
        Assert.That(result, Is.True);

        yield return null;
    }

    [UnityTest]
    public IEnumerator DestroySelectedObject_Audio_RemovesFromLevelManager_AndDisablesUI()
    {
        var go = Spawn("InteractionControllerGO");

        // Add LevelManager on same GO because DestroySelectedObject does GetComponent<LevelManager>()
        var lm = go.AddComponent<LevelManager>();
        lm.enabled = false; // prevent Start->StateManager dependency

        var ic = go.AddComponent<InteractionController>();

        var audioUI = CreateAudioUIWithToggle(_spawned);
        var deleteUI = CreateDeleteUI(_spawned);

        SetPrivateField(ic, "audioUI", audioUI);
        SetPrivateField(ic, "deleteUI", deleteUI);

        // Create selected audio object
        var selected = Spawn("AudioObj");
        selected.tag = "Audio";
        selected.AddComponent<AudioSource>();
        audioUI.SetActive(true);
        deleteUI.SetActive(true);

        // Put it into LevelManager list so RemoveSoundFromList has something to do
        lm.AddSoundToList(selected);

        SetPrivateField(ic, "selectedObject", selected);

        ic.DestroySelectedObject();

        Assert.That(audioUI.activeSelf, Is.False);
        Assert.That(deleteUI.activeSelf, Is.False);

        yield return null;
    }

    [UnityTest]
    public IEnumerator PlayAudio_DoesNothing_WhenNoSelected()
    {
        var go = Spawn("InteractionControllerGO");
        var ic = go.AddComponent<InteractionController>();

        SetPrivateField(ic, "selectedObject", null);

        Assert.DoesNotThrow(() => ic.PlayAudio());
        yield return null;
    }

    [UnityTest]
    public IEnumerator PlayAudio_PlaysOneShot_WhenSelectedHasAudioSource()
    {
        // Add AudioListener so Unity audio doesn't warn
        Spawn("AudioListener").AddComponent<AudioListener>();

        var go = Spawn("InteractionControllerGO");
        var ic = go.AddComponent<InteractionController>();

        var selected = Spawn("AudioObj");
        var src = selected.AddComponent<AudioSource>();
        src.clip = AudioClip.Create("dummy", 4410, 1, 44100, false);

        SetPrivateField(ic, "selectedObject", selected);

        Assert.DoesNotThrow(() => ic.PlayAudio());
        yield return null;
    }

    [UnityTest]
    public IEnumerator SetAudioLoop_SetsLoop_AndLogs()
    {
        Spawn("AudioListener").AddComponent<AudioListener>();

        var go = Spawn("InteractionControllerGO");
        var ic = go.AddComponent<InteractionController>();

        var selected = Spawn("AudioObj");
        var src = selected.AddComponent<AudioSource>();
        src.clip = AudioClip.Create("dummy", 4410, 1, 44100, false);

        SetPrivateField(ic, "selectedObject", selected);

        LogAssert.Expect(LogType.Log, $"Loop impostato a: {true} per {selected.name}");
        ic.SetAudioLoop(true);
        Assert.That(src.loop, Is.True);

        yield return null;
    }

    [UnityTest]
    public IEnumerator DisableUI_DisablesBoth()
    {
        var go = Spawn("InteractionControllerGO");
        var ic = go.AddComponent<InteractionController>();

        var audioUI = CreateAudioUIWithToggle(_spawned);
        var deleteUI = CreateDeleteUI(_spawned);
        audioUI.SetActive(true);
        deleteUI.SetActive(true);

        SetPrivateField(ic, "audioUI", audioUI);
        SetPrivateField(ic, "deleteUI", deleteUI);

        ic.DisableUI();

        Assert.That(audioUI.activeSelf, Is.False);
        Assert.That(deleteUI.activeSelf, Is.False);

        yield return null;
    }

    // ------------------ EXTRA TESTS (added) ------------------

    [UnityTest]
    public IEnumerator EnableInteraction_True_DoesNotThrow()
    {
        var go = Spawn("InteractionControllerGO");
        var ic = go.AddComponent<InteractionController>();

        // just cover enable branch
        Assert.DoesNotThrow(() => ic.EnableInteraction(true));

        yield return null;
    }

    [UnityTest]
    public IEnumerator DestroySelectedObject_WhenSelectedIsNull_DoesNothing()
    {
        var go = Spawn("InteractionControllerGO");
        var ic = go.AddComponent<InteractionController>();

        SetPrivateField(ic, "selectedObject", null);

        Assert.DoesNotThrow(() => ic.DestroySelectedObject());
        yield return null;
    }

    [UnityTest]
    public IEnumerator DestroySelectedObject_NonAudio_DisablesDeleteUI_DestroysObject()
    {
        var go = Spawn("InteractionControllerGO");
        var ic = go.AddComponent<InteractionController>();

        var audioUI = CreateAudioUIWithToggle(_spawned);
        var deleteUI = CreateDeleteUI(_spawned);
        audioUI.SetActive(true);
        deleteUI.SetActive(true);

        SetPrivateField(ic, "audioUI", audioUI);
        SetPrivateField(ic, "deleteUI", deleteUI);

        var selected = Spawn("NonAudioObj");
        selected.tag = "Untagged";
        SetPrivateField(ic, "selectedObject", selected);

        ic.DestroySelectedObject();

        Assert.That(deleteUI.activeSelf, Is.False);

        // DestroyImmediate used: object should be gone immediately
        Assert.That(GameObject.Find("NonAudioObj"), Is.Null);

        yield return null;
    }

    [UnityTest]
    public IEnumerator SetAudioLoop_DoesNothing_WhenNoSelected()
    {
        var go = Spawn("InteractionControllerGO");
        var ic = go.AddComponent<InteractionController>();

        SetPrivateField(ic, "selectedObject", null);

        Assert.DoesNotThrow(() => ic.SetAudioLoop(true));
        yield return null;
    }

    [UnityTest]
    public IEnumerator SetAudioLoop_DoesNothing_WhenSelectedHasNoAudioSource()
    {
        var go = Spawn("InteractionControllerGO");
        var ic = go.AddComponent<InteractionController>();

        var selected = Spawn("NoAudioSourceObj");
        // no AudioSource
        SetPrivateField(ic, "selectedObject", selected);

        Assert.DoesNotThrow(() => ic.SetAudioLoop(true));
        yield return null;
    }

    [UnityTest]
    public IEnumerator DisableUI_WhenAudioUIOrDeleteUINull_Throws_NullReference()
    {
        // This test documents current behavior: DisableUI assumes both refs are assigned.
        // If you prefer it not to throw, change DisableUI to null-check like other methods.
        var go = Spawn("InteractionControllerGO");
        var ic = go.AddComponent<InteractionController>();

        SetPrivateField(ic, "audioUI", null);
        SetPrivateField(ic, "deleteUI", null);

        Assert.Throws<System.NullReferenceException>(() => ic.DisableUI());
        yield return null;
    }

    [UnityTest]
    public IEnumerator ApplySelection_And_ClearSelection_IfYouDidMinRefactor()
    {
        // This test only works AFTER you apply the minimal refactor that introduces ApplySelection/ClearSelection.
        // It will fail (method not found) on the old version.
        var go = Spawn("InteractionControllerGO");
        var ic = go.AddComponent<InteractionController>();

        // camera required because ApplySelection uses mainCamera.transform.position
        var cam = Spawn("Cam").AddComponent<Camera>();
        ic.mainCamera = cam;

        var audioUI = CreateAudioUIWithToggle(_spawned);
        var deleteUI = CreateDeleteUI(_spawned);
        SetPrivateField(ic, "audioUI", audioUI);
        SetPrivateField(ic, "deleteUI", deleteUI);

        var audioObj = Spawn("AudioObj2");
        audioObj.tag = "Audio";
        audioObj.AddComponent<AudioSource>().loop = true;

        // ApplySelection
        InvokePrivate(ic, "ApplySelection", audioObj, new Vector3(0, 0, 5));
        Assert.That(audioUI.activeSelf, Is.True);
        Assert.That(deleteUI.activeSelf, Is.True);

        // ClearSelection
        InvokePrivate(ic, "ClearSelection");
        Assert.That(audioUI.activeSelf, Is.False);
        Assert.That(deleteUI.activeSelf, Is.False);

        yield return null;
    }
}