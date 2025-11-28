using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject freeCamObject;    
    [SerializeField] private GameObject playerObject;     
    [SerializeField] private GameObject editorUI;         
    [SerializeField] private GameObject speech;
    [SerializeField] private GameObject rotatingCamera;
    [SerializeField] private MindfulnessAudioManager speechManager;
    [SerializeField] private GameObject mainMenuButtonsUI;
    [SerializeField] private GameObject exitPlayModeButton;
    [SerializeField] private List<GameObject> audioInSceneList;
        
    private void Start()
    {
        
        speechManager = speech.GetComponent<MindfulnessAudioManager>();
        
        StateManager.Instance.OnStateChanged += HandleStateChange;

        HandleStateChange(StateManager.Instance.CurrentState);
    }

    private void OnDestroy()
    {
        if (StateManager.Instance != null)
            StateManager.Instance.OnStateChanged -= HandleStateChange;
    }

    private void HandleStateChange(State state)
    {
        switch (state)
        {
            case State.Editing:
                freeCamObject.SetActive(true);
                playerObject.SetActive(false);
                rotatingCamera.SetActive(false);
                editorUI.SetActive(true);
                speech.SetActive(false);
                if(speechManager != null)
                    speechManager.StopSpeech();
                exitPlayModeButton.SetActive(false);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                
                mainMenuButtonsUI.SetActive(false);
                ShowAudioIcons(true);
                SetRandomAudioActive(false);

                break;

            case State.Playing:
                freeCamObject.SetActive(false);
                playerObject.SetActive(true);
                rotatingCamera.SetActive(false);
                speech.SetActive(true);
                exitPlayModeButton.SetActive(true);
                
                if(speechManager != null)
                    speechManager.StartSpeech();
                
                editorUI.SetActive(false);
                speech.SetActive(true);
                mainMenuButtonsUI.SetActive(false);

                ShowAudioIcons(false);
                SetRandomAudioActive(true);



                break;
            
            case State.MainMenu:
                rotatingCamera.SetActive(true);
                freeCamObject.SetActive(false);
                playerObject.SetActive(false);
                editorUI.SetActive(false);
                exitPlayModeButton.SetActive(false);
                
                if(speechManager != null)
                    speechManager.StopSpeech();
                speech.SetActive(false);
                mainMenuButtonsUI.SetActive(true);

                ShowAudioIcons(false);
                SetRandomAudioActive(false);

                break;
        }
    }

    public void OnPlayButtonPressed()
    {
        StateManager.Instance.UpdateState(State.Playing);
    }

    public void OnEditButtonPressed()
    {
        StateManager.Instance.UpdateState(State.Editing);
    }

    public void OnMenuButtonPressed()
    {
        StateManager.Instance.UpdateState(State.MainMenu);
    }

    public void AddSoundToList(GameObject soundObject)
    {
        if(soundObject == null) return;
        if (audioInSceneList == null) return;

        audioInSceneList.Add(soundObject);
    }

    public void RemoveSoundFromList(GameObject soundObject)
    {
        if (soundObject == null) return;
        if (audioInSceneList == null) return;

        audioInSceneList.Remove(soundObject);
    }

    public void ShowAudioIcons(bool show)
    {
        foreach (GameObject soundObject in audioInSceneList)
        {
            RandomizeAudio randomize = soundObject.GetComponent<RandomizeAudio>();
            if (randomize != null)
                randomize.EnableMeshAndCollider(show);
        }
    }

    public void SetRandomAudioActive(bool active)
    {
        foreach (GameObject soundObject in audioInSceneList)
        {
            RandomizeAudio randomize = soundObject.GetComponent<RandomizeAudio>();
            if (randomize != null)
            {
                if (active)
                {
                    randomize.StartSoundRandomly();
                }
                else
                {
                    randomize.StopRandomSound();
                }
            }
        }
    }



    public GameObject GetFreeCam()
    {
        return freeCamObject;
    }
}