using UnityEngine;

public class FreeSceneCamera : MonoBehaviour
{
    [Header("Movement Settings")] public float moveSpeed = 10f;
    public float boostMultiplier = 4f;
    public float acceleration = 12f;
    public float damping = 10f;

    [Header("Look Settings")] public float lookSensitivity = 0.15f;
    public float minPitch = -89f;
    public float maxPitch = 89f;


    private CameraController controls;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private Vector2 panInput;
    private float scrollInput;

    private bool rightClickHeld;
    private bool middleClickHeld;
    private bool boostHeld;

    private Vector3 currentVelocity;
    private float pitch;
    private float yaw;

    private void Awake()
    {
        controls = new CameraController();

        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        controls.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        controls.Player.Look.canceled += ctx => lookInput = Vector2.zero;

        controls.Player.RightClick.started += _ => rightClickHeld = true;
        controls.Player.RightClick.canceled += _ => rightClickHeld = false;

        controls.Player.MiddleClick.started += _ => middleClickHeld = true;
        controls.Player.MiddleClick.canceled += _ => middleClickHeld = false;


        controls.Player.Sprint.started += _ => boostHeld = true;
        controls.Player.Sprint.canceled += _ => boostHeld = false;
    }

    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();

    private void Start()
    {
        Vector3 e = transform.rotation.eulerAngles;
        yaw = e.y;
        pitch = e.x;
    }

    private void Update()
    {
        HandleLook();
        HandleMovement();
    }

    private void HandleLook()
    {
        if (rightClickHeld)
        {
            yaw += lookInput.x * lookSensitivity;
            pitch -= lookInput.y * lookSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
        else if (middleClickHeld)
        {
            Vector3 right = transform.right * -lookInput.x * 0.02f;
            Vector3 up = transform.up * -lookInput.y * 0.02f;
            transform.position += right + up;
        }
    }

    private void HandleMovement()
    {
        if (!rightClickHeld)
            return;

        Vector3 targetVelocity =
            (transform.forward * moveInput.y +
             transform.right * moveInput.x);

        if (boostHeld)
            targetVelocity *= boostMultiplier;

        currentVelocity = Vector3.Lerp(
            currentVelocity,
            targetVelocity * moveSpeed,
            Time.deltaTime * acceleration
        );

        transform.position += currentVelocity * Time.deltaTime;

        currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, Time.deltaTime * damping);
    }
}