using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI; // Necessario per gestire il Toggle

public class InteractionController : MonoBehaviour
{
    [Header("References")] 
    public Camera mainCamera;
    public LayerMask selectableLayer;

    [Header("Drag Settings")] 
    public float dragDistance = 5f;
    public float dragSmooth = 10f;

    [Header("Rotation")] 
    public float rotationSpeed = 120f;

    [Header("Scaling")] 
    public float scaleSpeed = 0.1f;
    public float minScale = 0.001f;
    public float maxScale = 10f;

    [Header("UI References")]
    [SerializeField] private GameObject audioUI;
    [SerializeField] private GameObject deleteUI;

    private CameraController controls;
    private Vector2 pointerPosition;
    
    private bool dragging;
    private bool isClickingUI;
    private float rotateInput;
    private float scaleInput;

    private GameObject selectedObject;
    private Vector3 targetDragPosition;

    private void Awake()
    {
        controls = new CameraController();

        controls.Builder.PointerPosition.performed += ctx =>
            pointerPosition = ctx.ReadValue<Vector2>();
        controls.Builder.PointerPosition.canceled += _ =>
            pointerPosition = Vector2.zero;

        controls.Builder.Select.started += _ => 
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                isClickingUI = true; 
            }
            else
            {
                isClickingUI = false; 
                OnSelect();
            }
        };

        controls.Builder.Select.canceled += _ => 
        {
            isClickingUI = false; 
        };

        controls.Builder.Drag.performed += _ => { dragging = true; };
        controls.Builder.Drag.canceled += _ => { dragging = false; };

        controls.Builder.Rotate.performed += ctx =>
            rotateInput = ctx.ReadValue<float>();
        controls.Builder.Rotate.canceled += _ =>
            rotateInput = 0f;

        controls.Builder.Scale.performed += ctx =>
            scaleInput = ctx.ReadValue<float>();
        controls.Builder.Scale.canceled += _ =>
            scaleInput = 0f;
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void Start()
    {
        if (audioUI) audioUI.SetActive(false);
        if (deleteUI) deleteUI.SetActive(false);
    }
    
    public void EnableInteraction(bool enable)
    {
        if (enable)
        {
            controls.Enable();
        }
        else
        {
            controls.Disable();
            
            // dragging = false;
            // rotateInput = 0f;
            // scaleInput = 0f;
            
            selectedObject = null;
            if(audioUI) audioUI.SetActive(false);
            if(deleteUI) deleteUI.SetActive(false);
        }
    }

    private void Update()
    {
        HandleDragging();
        HandleRotation();
        HandleScaling();
    }

    private void OnSelect()
    {

        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(pointerPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, selectableLayer))
        {
            selectedObject = hit.collider.gameObject;
            float dist = Vector3.Distance(mainCamera.transform.position, hit.point);
            dragDistance = dist;

            if (selectedObject.CompareTag("Audio") && audioUI)
            {
                audioUI.SetActive(true);

                AudioSource source = selectedObject.GetComponent<AudioSource>();
                Toggle uiToggle = audioUI.GetComponentInChildren<Toggle>();

                if (source != null && uiToggle != null)
                {
                    uiToggle.SetIsOnWithoutNotify(source.loop);
                }
            }
            else
            {
                audioUI.SetActive(false);
            }

            deleteUI.SetActive(true);
        }
        else
        {
            selectedObject = null;
            audioUI.SetActive(false);
            deleteUI.SetActive(false);
        }
    }

    private void HandleDragging()
    {
        if (isClickingUI) return;

        if (!dragging || selectedObject == null || mainCamera == null)
            return;

        Ray ray = mainCamera.ScreenPointToRay(pointerPosition);
        Vector3 desiredPos = ray.origin + ray.direction * dragDistance;

        targetDragPosition = Vector3.Lerp(
            selectedObject.transform.position,
            desiredPos,
            Time.deltaTime * dragSmooth
        );

        selectedObject.transform.position = targetDragPosition;
    }

    private void HandleRotation()
    {
        if (IsTypingInInputField()) return; 

        if (selectedObject == null || Mathf.Abs(rotateInput) < 0.0001f)
            return;

        float angle = rotateInput * rotationSpeed * Time.deltaTime;
        selectedObject.transform.Rotate(Vector3.up, angle, Space.World);
    }

    private void HandleScaling()
    {
        if (IsTypingInInputField()) return;

        if (selectedObject == null || Mathf.Abs(scaleInput) < 0.0001f)
            return;

        float factor = 1f + scaleInput * scaleSpeed;
        Vector3 s = selectedObject.transform.localScale * factor;

        s.x = Mathf.Clamp(s.x, minScale, maxScale);
        s.y = Mathf.Clamp(s.y, minScale, maxScale);
        s.z = Mathf.Clamp(s.z, minScale, maxScale);

        selectedObject.transform.localScale = s;
    }

    
    private bool IsTypingInInputField()
    {
        if (EventSystem.current == null) return false;

        GameObject selectedObj = EventSystem.current.currentSelectedGameObject;

        if (selectedObj == null) return false;

        return selectedObj.GetComponent<TMP_InputField>() != null;
    }

    public void DestroySelectedObject()
    {
        if (selectedObject == null) return;

        if (selectedObject.CompareTag("Audio"))
        {
            LevelManager levelManager = GetComponent<LevelManager>();
            if (levelManager != null)
            {
                levelManager.RemoveSoundFromList(selectedObject);
            }
            audioUI.SetActive(false);
        }

        deleteUI.SetActive(false);
        DestroyImmediate(selectedObject);
        selectedObject = null;
    }

    public void PlayAudio()
    {
        if (selectedObject == null) return;
        AudioSource audioSource = selectedObject.GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.PlayOneShot(audioSource.clip);
        }
    }

    public void SetAudioLoop(bool enableLoop)
    {
        if (selectedObject == null) return;
        
        AudioSource audioSource = selectedObject.GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.loop = enableLoop;
            Debug.Log($"Loop impostato a: {enableLoop} per {selectedObject.name}");
            
        }
    }

    public void DisableUI()
    {
        audioUI.SetActive(false);
        deleteUI.SetActive(false);
    }
}