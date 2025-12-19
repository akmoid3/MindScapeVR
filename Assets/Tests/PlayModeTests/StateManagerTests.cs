using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class StateManagerTests
{
    [SetUp]
    public void SetUp()
    {
        // Pulizia tra i test (importante perché Instance è static)
        if (StateManager.Instance != null)
        {
            Object.DestroyImmediate(StateManager.Instance.gameObject);
        }
    }

    [TearDown]
    public void TearDown()
    {
        if (StateManager.Instance != null)
        {
            Object.DestroyImmediate(StateManager.Instance.gameObject);
        }
    }

    [UnityTest]
    public IEnumerator Start_SetsInitialState_ToMainMenu()
    {
        var go = new GameObject("StateManager");
        var sm = go.AddComponent<StateManager>();

        // Aspetta 1 frame per far eseguire Start()
        yield return null;

        Assert.AreEqual(State.MainMenu, sm.CurrentState);
        Assert.AreSame(sm, StateManager.Instance);
    }

    [Test]
    public void UpdateState_ChangesCurrentState()
    {
        var go = new GameObject("StateManager");
        var sm = go.AddComponent<StateManager>();

        sm.UpdateState(State.Editing);

        Assert.AreEqual(State.Editing, sm.CurrentState);
    }

    [Test]
    public void UpdateState_InvokesEvent_WithNewState()
    {
        var go = new GameObject("StateManager");
        var sm = go.AddComponent<StateManager>();

        State received = default;
        int calls = 0;

        sm.OnStateChanged += s =>
        {
            received = s;
            calls++;
        };

        sm.UpdateState(State.Playing);

        Assert.AreEqual(1, calls);
        Assert.AreEqual(State.Playing, received);
    }

    [UnityTest]
    public IEnumerator Awake_Singleton_DestroysSecondInstance()
    {
        var go1 = new GameObject("StateManager_1");
        var sm1 = go1.AddComponent<StateManager>();

        // Lascia eseguire Awake
        yield return null;

        Assert.AreSame(sm1, StateManager.Instance);

        var go2 = new GameObject("StateManager_2");
        var sm2 = go2.AddComponent<StateManager>();

        // Destroy(gameObject) avviene a fine frame, quindi aspettiamo
        yield return null;

        // In Unity, dopo Destroy l'oggetto risulta "null" (Unity null)
        Assert.IsTrue(sm2 == null || sm2.Equals(null));
        Assert.AreSame(sm1, StateManager.Instance);
    }
}