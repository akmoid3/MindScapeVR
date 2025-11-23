using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionController : MonoBehaviour
{
   [Header("References")]
    public Camera mainCamera;
    public LayerMask selectableLayer;

    [Header("Drag Settings")]
    public float dragDistance = 5f; 
    public float dragSmooth = 0.0f;  

    [Header("Rotation")]
    public float rotationSpeed = 120f;

    [Header("Scaling")]
    public float scaleSpeed = 0.1f;
    public float minScale = 0.001f;
    public float maxScale = 10f;

    private CameraController controls;

    private Vector2 pointerPosition;
    private bool dragging;
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

        controls.Builder.Select.started += _ => OnSelect();

        controls.Builder.Drag.started  += _ => dragging = true;
        controls.Builder.Drag.canceled += _ => dragging = false;

        controls.Builder.Rotate.performed += ctx =>
            rotateInput = ctx.ReadValue<float>();
        controls.Builder.Rotate.canceled  += _ =>
            rotateInput = 0f;

        controls.Builder.Scale.performed += ctx =>
            scaleInput = ctx.ReadValue<float>();
        controls.Builder.Scale.canceled  += _ =>
            scaleInput = 0f;
    }

    private void OnEnable()  => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
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
            Debug.Log("Selezionato: " + selectedObject.name);

            float dist = Vector3.Distance(mainCamera.transform.position, hit.point);
            dragDistance = dist;
        }
        else
        {
            selectedObject = null;
            Debug.Log("Click ma nessun oggetto selezionabile");
        }
    }

    private void HandleDragging()
    {
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
        if (selectedObject == null || Mathf.Abs(rotateInput) < 0.0001f)
            return;

        float angle = rotateInput * rotationSpeed * Time.deltaTime;
        selectedObject.transform.Rotate(Vector3.up, angle, Space.World);
    }

    private void HandleScaling()
    {
        if (selectedObject == null || Mathf.Abs(scaleInput) < 0.0001f)
            return;

        float factor = 1f + scaleInput * scaleSpeed;
        Vector3 s = selectedObject.transform.localScale * factor;

        s.x = Mathf.Clamp(s.x, minScale, maxScale);
        s.y = Mathf.Clamp(s.y, minScale, maxScale);
        s.z = Mathf.Clamp(s.z, minScale, maxScale);

        selectedObject.transform.localScale = s;
    }
}