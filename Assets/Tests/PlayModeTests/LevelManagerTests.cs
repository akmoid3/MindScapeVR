using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public class LevelManager_Reflection100Tests
{
    private readonly List<GameObject> _spawned = new();

    private GameObject Spawn(string name, bool active = true)
    {
        var go = new GameObject(name);
        go.SetActive(active);
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

    private static object GetPrivateField(object instance, string fieldName)
    {
        var f = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(f, Is.Not.Null, $"Field not found: {instance.GetType().Name}.{fieldName}");
        return f.GetValue(instance);
    }

    private void SetupLevelManagerRefs(LevelManager lm)
    {
        var freeCam = Spawn("freeCamObject");
        freeCam.SetActive(false);
        freeCam.AddComponent<Camera>();

        var player = Spawn("playerObject");
        player.SetActive(true);

        var editorUI = Spawn("editorUI");
        var speech = Spawn("speech");
        var rotatingCamera = Spawn("rotatingCamera");
        rotatingCamera.SetActive(false);
        rotatingCamera.AddComponent<Camera>();

        var mainMenuButtonsUI = Spawn("mainMenuButtonsUI");
        var exitPlayModeButton = Spawn("exitPlayModeButton");

        SetPrivateField(lm, "freeCamObject", freeCam);
        SetPrivateField(lm, "playerObject", player);
        SetPrivateField(lm, "editorUI", editorUI);
        SetPrivateField(lm, "speech", speech);
        SetPrivateField(lm, "rotatingCamera", rotatingCamera);
        SetPrivateField(lm, "mainMenuButtonsUI", mainMenuButtonsUI);
        SetPrivateField(lm, "exitPlayModeButton", exitPlayModeButton);

        SetPrivateField(lm, "audioInSceneList", new List<GameObject>());

        // optional deps left null to cover null branches
        SetPrivateField(lm, "interactionController", null);
    }

    [UnityTest]
    public IEnumerator LevelManager_FullFlow_WithStateManager_Buttons_Editing_Playing_MainMenu_AndOnDestroy()
    {
        // Create StateManager
        var smGo = Spawn("StateManager");
        var sm = smGo.AddComponent<StateManager>();
        yield return null; // let StateManager.Start set MainMenu

        // Create LevelManager and configure BEFORE its Start runs
        var lmGo = Spawn("LevelManager");
        var lm = lmGo.AddComponent<LevelManager>();
        SetupLevelManagerRefs(lm);

        // let LevelManager.Start run (subscribe + initial HandleStateChange)
        yield return null;

        // --- Cover button handlers (210-212)
        // OnEditButtonPressed -> Editing
        lm.OnEditButtonPressed();
        Assert.That(sm.CurrentState, Is.EqualTo(State.Editing));
        yield return null;
        yield return null;

        // OnPlayButtonPressed -> Playing (will trigger XR and likely log error on desktop)
        LogAssert.Expect(LogType.Error, "Errore: Visore non rilevato o inizializzazione fallita.");
        lm.OnPlayButtonPressed();
        Assert.That(sm.CurrentState, Is.EqualTo(State.Playing));
        yield return null;
        yield return null;

        // OnMenuButtonPressed -> MainMenu
        lm.OnMenuButtonPressed();
        Assert.That(sm.CurrentState, Is.EqualTo(State.MainMenu));
        yield return null;
        yield return null;

        // --- Also cover direct state updates (already implicitly covered, but keep explicit)
        sm.UpdateState(State.Editing);
        yield return null;

        // Public APIs coverage
        var audio1 = Spawn("audio1");
        audio1.AddComponent<RandomizeAudio>();
        lm.AddSoundToList(audio1);

        var list = (List<GameObject>)GetPrivateField(lm, "audioInSceneList");
        list.Add(null);

        Assert.DoesNotThrow(() => lm.EnableAudioObjectCollider(true));
        Assert.DoesNotThrow(() => lm.SetRandomAudioActive(true));
        Assert.DoesNotThrow(() => lm.SetRandomAudioActive(false));

        // Guards for null input / null list
        lm.RemoveSoundFromList(null);
        SetPrivateField(lm, "audioInSceneList", null);
        lm.RemoveSoundFromList(audio1);
        lm.ClearAudioList();

        // restore list so no surprises
        SetPrivateField(lm, "audioInSceneList", new List<GameObject>());

        // OnDestroy unsubscribe
        Object.Destroy(lmGo);
        yield return null;

        Object.Destroy(smGo);
        yield return null;
    }
}