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
    [SerializeField] private List<GameObject> audioInSceneList = new List<GameObject>();
    [SerializeField] private InteractionController interactionController;

    
    [SerializeField] private Camera transitionCamera;
    
    private Coroutine transitionRoutine;

    private void Start()
    {
        transitionCamera.gameObject.SetActive(false);
        speechManager = speech != null ? speech.GetComponent<MindfulnessAudioManager>() : null;

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
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        switch (state)
        {
            case State.Editing:
                editorUI.SetActive(true);
                mainMenuButtonsUI.SetActive(false);
                exitPlayModeButton.SetActive(false);

                speech.SetActive(false);
                if (speechManager != null) speechManager.StopSpeech();

                if (interactionController != null) interactionController.EnableInteraction(true);

                EnableAudioObjectCollider(true);
                SetRandomAudioActive(false);

                transitionRoutine = StartCoroutine(TransitionToDesktop(editingMode: true));
                break;

            case State.Playing:
                editorUI.SetActive(false);
                mainMenuButtonsUI.SetActive(false);
                exitPlayModeButton.SetActive(true);

                speech.SetActive(true);
                if (speechManager != null) speechManager.StartSpeech();

                if (interactionController != null) interactionController.EnableInteraction(false);

                EnableAudioObjectCollider(false);
                SetRandomAudioActive(true);

                transitionRoutine = StartCoroutine(TransitionToVR());
                break;

            case State.MainMenu:
                editorUI.SetActive(false);
                mainMenuButtonsUI.SetActive(true);
                exitPlayModeButton.SetActive(false);

                speech.SetActive(false);
                if (speechManager != null) speechManager.StopSpeech();

                if (interactionController != null) interactionController.EnableInteraction(false);

                EnableAudioObjectCollider(false);
                SetRandomAudioActive(false);

                transitionRoutine = StartCoroutine(TransitionToDesktop(editingMode: false));
                break;
        }
    }


    private void DisableDesktopCamerasImmediately()
    {
        if (freeCamObject != null)
        {
            var cam = freeCamObject.GetComponentInChildren<Camera>(true);
            if (cam != null) cam.enabled = false;
            freeCamObject.SetActive(false);
        }

        if (rotatingCamera != null)
        {
            var cam = rotatingCamera.GetComponentInChildren<Camera>(true);
            if (cam != null) cam.enabled = false;
            rotatingCamera.SetActive(false);
        }
    }

    private void EnableFreeCam()
    {
        if (freeCamObject == null) return;

        freeCamObject.SetActive(true);
        var cam = freeCamObject.GetComponentInChildren<Camera>(true);
        if (cam != null) cam.enabled = true;
    }

    private void EnableRotatingCamera()
    {
        if (rotatingCamera == null) return;

        rotatingCamera.SetActive(true);
        var cam = rotatingCamera.GetComponentInChildren<Camera>(true);
        if (cam != null) cam.enabled = true;
    }


    private IEnumerator TransitionToVR()
    {
        if (transitionCamera != null)
        {
            transitionCamera.gameObject.SetActive(true);
            transitionCamera.enabled = true;
            transitionCamera.depth = 100;
        }

        DisableDesktopCamerasImmediately();
        if (playerObject != null) playerObject.SetActive(false);

        yield return new WaitForSeconds(0.3f);

        yield return SwitchToVRMode();

        yield return new WaitForSeconds(0.5f);

        if (playerObject != null) playerObject.SetActive(true);

        yield return new WaitForEndOfFrame();
        if (transitionCamera != null)
            transitionCamera.gameObject.SetActive(false);
    }

    private IEnumerator TransitionToDesktop(bool editingMode)
    {
        if (playerObject != null) playerObject.SetActive(false);

        yield return SwitchToDesktopMode();

        yield return null;

        if (editingMode)
        {
            EnableFreeCam();
            if (rotatingCamera != null) rotatingCamera.SetActive(false);
        }
        else
        {
            EnableRotatingCamera();
            if (freeCamObject != null) freeCamObject.SetActive(false);
        }
    }

    private IEnumerator SwitchToVRMode()
    {
        if (XRGeneralSettings.Instance != null &&
            XRGeneralSettings.Instance.Manager != null &&
            XRGeneralSettings.Instance.Manager.activeLoader == null)
        {
            Debug.Log("Inizializzazione VR...");
            yield return XRGeneralSettings.Instance.Manager.InitializeLoader();
        }

        if (XRGeneralSettings.Instance != null &&
            XRGeneralSettings.Instance.Manager != null &&
            XRGeneralSettings.Instance.Manager.activeLoader != null)
        {
            XRGeneralSettings.Instance.Manager.StartSubsystems();
            Debug.Log("Modalità VR Attiva");
        }
        else
        {
            Debug.LogError("Errore: Visore non rilevato o inizializzazione fallita.");
            EnableFreeCam();
        }
    }

    private IEnumerator SwitchToDesktopMode()
    {
        if (XRGeneralSettings.Instance != null &&
            XRGeneralSettings.Instance.Manager != null &&
            XRGeneralSettings.Instance.Manager.isInitializationComplete)
        {
            Debug.Log("Spegnimento VR...");
            XRGeneralSettings.Instance.Manager.StopSubsystems();
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
        }

        yield return null;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Modalità Desktop Attiva (Mouse Sbloccato)");
    }

    public void OnPlayButtonPressed() => StateManager.Instance.UpdateState(State.Playing);
    public void OnEditButtonPressed() => StateManager.Instance.UpdateState(State.Editing);
    public void OnMenuButtonPressed() => StateManager.Instance.UpdateState(State.MainMenu);

    public void AddSoundToList(GameObject soundObject)
    {
        if (soundObject == null) return;
        if (audioInSceneList == null) return;
        audioInSceneList.Add(soundObject);
    }

    public void RemoveSoundFromList(GameObject soundObject)
    {
        if (soundObject == null) return;
        if (audioInSceneList == null) return;
        audioInSceneList.Remove(soundObject);
    }

    public void EnableAudioObjectCollider(bool enable)
    {
        if (audioInSceneList == null) return;

        foreach (GameObject soundObject in audioInSceneList)
        {
            if (soundObject == null) continue;
            RandomizeAudio randomize = soundObject.GetComponent<RandomizeAudio>();
            if (randomize != null) randomize.EnableCollider(enable);
        }
    }

    public void SetRandomAudioActive(bool active)
    {
        if (audioInSceneList == null) return;

        foreach (GameObject soundObject in audioInSceneList)
        {
            if (soundObject == null) continue;
            RandomizeAudio randomize = soundObject.GetComponent<RandomizeAudio>();
            if (randomize == null) continue;

            if (active) randomize.StartSoundRandomly();
            else randomize.StopRandomSound();
        }
    }

    public GameObject GetFreeCam() => freeCamObject;

    public void ClearAudioList()
    {
        if (audioInSceneList != null)
            audioInSceneList.Clear();
    }
}