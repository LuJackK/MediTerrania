using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class SwimTryOutMovementController : MonoBehaviour
{
    [SerializeField] public float moveSpeed = 7f;
    [SerializeField] public float sprintMultiplier = 1.7f;
    [SerializeField] public float verticalSpeed = 5f;
    [SerializeField] public float mouseSensitivity = 0.09f;
    [SerializeField] public float gamepadLookSensitivity = 110f;
    [SerializeField] public bool lockCursorOnStart = true;
    [SerializeField] public bool unlockCursorWithEscape = true;
    [SerializeField] public bool moveAlongLookDirection = true;
    [SerializeField] public float controllerRadius = 0.45f;
    [SerializeField] public float controllerHeight = 1.8f;
    [SerializeField] public bool addHabitatMeshColliders = true;
    [SerializeField] public bool constrainToWaterVolume = true;
    [SerializeField] public float waterBoundaryPadding = 1.5f;
    [SerializeField] public float floorPadding = 0.25f;
    [SerializeField] public float surfacePadding = 0.4f;

    private float pitch;
    private float yaw;
    private CharacterController characterController;
    private Bounds waterBounds;
    private float minSwimY;
    private float maxSwimY;
    private bool hasWaterBounds;

    private void Awake()
    {
        Vector3 euler = transform.eulerAngles;
        pitch = Mathf.DeltaAngle(0f, euler.x);
        yaw = euler.y;

        EnsureCharacterController();
        AddHabitatCollision();
        CacheWaterBounds();
        ClampToWaterVolume();
    }

    private void OnEnable()
    {
        if (lockCursorOnStart)
        {
            LockCursor();
        }
    }

    private void Update()
    {
        UpdateCursorLock();
        UpdateLook();
        UpdateMovement();
    }

    private void UpdateCursorLock()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (unlockCursorWithEscape && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            LockCursor();
        }
    }

    private void UpdateLook()
    {
        Vector2 look = Vector2.zero;

        Mouse mouse = Mouse.current;
        if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
        {
            look += mouse.delta.ReadValue() * mouseSensitivity;
        }

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            look += gamepad.rightStick.ReadValue() * (gamepadLookSensitivity * Time.deltaTime);
        }

        yaw += look.x;
        pitch = Mathf.Clamp(pitch - look.y, -85f, 85f);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void UpdateMovement()
    {
        Vector2 moveInput = ReadMoveInput();
        float verticalInput = ReadVerticalInput();

        Vector3 forward = moveAlongLookDirection
            ? transform.forward
            : Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 right = moveAlongLookDirection
            ? transform.right
            : Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;

        Vector3 direction = (forward * moveInput.y) + (right * moveInput.x);
        if (direction.sqrMagnitude > 1f)
        {
            direction.Normalize();
        }

        float speed = IsSprinting() ? moveSpeed * sprintMultiplier : moveSpeed;
        Vector3 delta = direction * speed * Time.deltaTime;
        delta += Vector3.up * (verticalInput * verticalSpeed * Time.deltaTime);

        if (characterController != null && characterController.enabled)
        {
            characterController.Move(delta);
        }
        else
        {
            transform.position += delta;
        }

        ClampToWaterVolume();
    }

    private void EnsureCharacterController()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
        }

        characterController.radius = controllerRadius;
        characterController.height = controllerHeight;
        characterController.center = Vector3.zero;
        characterController.slopeLimit = 80f;
        characterController.stepOffset = 0.2f;
        characterController.minMoveDistance = 0f;
        characterController.detectCollisions = true;
    }

    private void AddHabitatCollision()
    {
        if (!addHabitatMeshColliders)
        {
            return;
        }

        GameObject habitat = GameObject.Find("habitat1");
        if (habitat == null)
        {
            return;
        }

        MeshFilter[] meshFilters = habitat.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < meshFilters.Length; i++)
        {
            MeshFilter meshFilter = meshFilters[i];
            if (meshFilter.sharedMesh == null || meshFilter.GetComponent<Collider>() != null)
            {
                continue;
            }

            MeshCollider meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.convex = false;
        }
    }

    private void CacheWaterBounds()
    {
        if (!constrainToWaterVolume)
        {
            hasWaterBounds = false;
            return;
        }

        Renderer floorRenderer = FindRenderer("SeaFloor");
        Renderer ceilingRenderer = FindRenderer("SeaCeeling");
        if (floorRenderer == null || ceilingRenderer == null)
        {
            hasWaterBounds = false;
            return;
        }

        Bounds floorBounds = floorRenderer.bounds;
        Bounds ceilingBounds = ceilingRenderer.bounds;

        waterBounds = floorBounds;
        waterBounds.Encapsulate(ceilingBounds);

        float halfHeight = characterController != null ? characterController.height * 0.5f : 0f;
        minSwimY = floorBounds.max.y + halfHeight + floorPadding;
        maxSwimY = ceilingBounds.min.y - halfHeight - surfacePadding;
        hasWaterBounds = maxSwimY > minSwimY;
    }

    private void ClampToWaterVolume()
    {
        if (!hasWaterBounds)
        {
            return;
        }

        Vector3 position = transform.position;
        float horizontalPadding = waterBoundaryPadding + (characterController != null ? characterController.radius : 0f);
        position.x = Mathf.Clamp(position.x, waterBounds.min.x + horizontalPadding, waterBounds.max.x - horizontalPadding);
        position.y = Mathf.Clamp(position.y, minSwimY, maxSwimY);
        position.z = Mathf.Clamp(position.z, waterBounds.min.z + horizontalPadding, waterBounds.max.z - horizontalPadding);

        if (position != transform.position)
        {
            bool wasEnabled = characterController != null && characterController.enabled;
            if (wasEnabled)
            {
                characterController.enabled = false;
            }

            transform.position = position;

            if (wasEnabled)
            {
                characterController.enabled = true;
            }
        }
    }

    private static Renderer FindRenderer(string objectName)
    {
        GameObject target = GameObject.Find(objectName);
        return target != null ? target.GetComponent<Renderer>() : null;
    }

    private static Vector2 ReadMoveInput()
    {
        Vector2 input = Vector2.zero;
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                input.x -= 1f;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                input.x += 1f;
            }

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                input.y -= 1f;
            }

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                input.y += 1f;
            }
        }

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            input += gamepad.leftStick.ReadValue();
        }

        return Vector2.ClampMagnitude(input, 1f);
    }

    private static float ReadVerticalInput()
    {
        float input = 0f;
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.spaceKey.isPressed || keyboard.eKey.isPressed)
            {
                input += 1f;
            }

            if (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed || keyboard.qKey.isPressed)
            {
                input -= 1f;
            }
        }

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            input += gamepad.rightTrigger.ReadValue();
            input -= gamepad.leftTrigger.ReadValue();
        }

        return Mathf.Clamp(input, -1f, 1f);
    }

    private static bool IsSprinting()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed))
        {
            return true;
        }

        Gamepad gamepad = Gamepad.current;
        return gamepad != null && gamepad.leftStickButton.isPressed;
    }

    private static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
