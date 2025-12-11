using System;
using UnityEngine;


public enum State
{
    MainMenu,
    Editing,
    Playing
}

public class StateManager : MonoBehaviour
{
    public static StateManager Instance { get; private set; }
    public State CurrentState { get; private set; }
    public event Action<State> OnStateChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        UpdateState(State.MainMenu);
    }

    public void UpdateState(State state)
    {
        CurrentState = state;
        OnStateChanged?.Invoke(state);
    }
}