using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Management; 

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
        
        StartCoroutine(SwitchToDesktopMode());

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
        StopAllCoroutines();

        switch (state)
        {
            case State.Editing:
                StartCoroutine(SwitchToDesktopMode());

                editorUI.SetActive(true);
                speech.SetActive(false);
                if(speechManager != null) speechManager.StopSpeech();
                exitPlayModeButton.SetActive(false);
                mainMenuButtonsUI.SetActive(false);
                
                ShowAudioIcons(true);
                SetRandomAudioActive(false);
                break;

            case State.Playing:
                StartCoroutine(SwitchToVRMode());

                speech.SetActive(true);
                exitPlayModeButton.SetActive(true);
                if(speechManager != null) speechManager.StartSpeech();
                
                editorUI.SetActive(false);
                mainMenuButtonsUI.SetActive(false);

                ShowAudioIcons(false);
                SetRandomAudioActive(true);
                break;
            
            case State.MainMenu:
                StartCoroutine(SwitchToDesktopMode());

                rotatingCamera.SetActive(true);
                
                editorUI.SetActive(false);
                exitPlayModeButton.SetActive(false);
                if(speechManager != null) speechManager.StopSpeech();
                speech.SetActive(false);
                mainMenuButtonsUI.SetActive(true);

                ShowAudioIcons(false);
                SetRandomAudioActive(false);
                break;
        }
    }


    private IEnumerator SwitchToVRMode()
    {
        freeCamObject.SetActive(false);
        rotatingCamera.SetActive(false);

        if (XRGeneralSettings.Instance.Manager.activeLoader == null)
        {
            Debug.Log("Inizializzazione VR...");
            yield return XRGeneralSettings.Instance.Manager.InitializeLoader();
        }

        if (XRGeneralSettings.Instance.Manager.activeLoader != null)
        {
            XRGeneralSettings.Instance.Manager.StartSubsystems();
            
            playerObject.SetActive(true);
            Debug.Log("Modalità VR Attiva");
        }
        else
        {
            Debug.LogError("Errore: Visore non rilevato o inizializzazione fallita.");
            freeCamObject.SetActive(true);
        }
    }

    private IEnumerator SwitchToDesktopMode()
    {
        playerObject.SetActive(false);

        if (XRGeneralSettings.Instance.Manager.isInitializationComplete)
        {
            Debug.Log("Spegnimento VR...");
            XRGeneralSettings.Instance.Manager.StopSubsystems();
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
        }

        if (StateManager.Instance.CurrentState == State.Editing)
        {
            freeCamObject.SetActive(true);
        }

        yield return null; 
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        Debug.Log("Modalità Desktop Attiva (Mouse Sbloccato)");
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
                if (active) randomize.StartSoundRandomly();
                else randomize.StopRandomSound();
            }
        }
    }

    public GameObject GetFreeCam()
    {
        return freeCamObject;
    }
}