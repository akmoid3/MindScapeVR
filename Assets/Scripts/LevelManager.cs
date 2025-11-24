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
                EnableAudioIcons(true);

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
                EnableAudioIcons(false);



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
                EnableAudioIcons(false);

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

    public void EnableAudioIcons(bool enable)
    {
        if(audioInSceneList.Count==0) return;

        foreach (GameObject soundObject in audioInSceneList)
        {
            RandomizeAudio randomizeAudio = soundObject.GetComponent<RandomizeAudio>();
            if(randomizeAudio != null)
            {
                randomizeAudio.EnableMeshAndCollider(enable);
                randomizeAudio.enabled = !enable;
            }
        }
    }
    public GameObject GetFreeCam()
    {
        return freeCamObject;
    }
}