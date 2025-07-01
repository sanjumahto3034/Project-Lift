using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerControl : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    public Transform _playerEye;
    public float walkingSpeed = 7.5f;
    public float runningSpeed = 11.5f;
    public float jumpSpeed = 8.0f;
    public float gravity = 20.0f;
    public float lookSpeed = 2.0f;
    public float lookXLimit = 45.0f;

    private CharacterController characterController;
    // private PlayerInputActions inputActions;
    [SerializeField] private Vector2 moveInput;
    [SerializeField] private Vector2 lookInput;
    [SerializeField] private bool jumpPressed;

    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;

    [SerializeField] private GameObject _interactObjectShow;
    private IInteract _interactableObject = null;
    private bool _isInUIMode;
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }
    public void OnUIMode(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            _isInUIMode = !_isInUIMode;
            Debug.Log($"_isInUIMode: {_isInUIMode}");
            Cursor.lockState = _isInUIMode ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = _isInUIMode ? true : false;
        }
    }
    public void OnMove(InputAction.CallbackContext context)
    {
        if (_isInUIMode)
        {
            moveInput = Vector2.zero;
            return;
        }
        moveInput = context.ReadValue<Vector2>();
    }
    public void OnLook(InputAction.CallbackContext context)
    {
        if (_isInUIMode)
        {
            lookInput = Vector2.zero;
            return;
        }
        lookInput = context.ReadValue<Vector2>();
    }
    public void OnJump(InputAction.CallbackContext context)
    {
        if (_isInUIMode)
        {
            jumpPressed = false;
            return;
        }
        float value = context.ReadValue<float>();
        jumpPressed = value > 0;

    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        UpdateMovement();
        UpdateInteraction();
        HandleLiftPlatform();
    }
    private Vector3 lastLiftPosition;
    private Transform currentLift;

    private void HandleLiftPlatform()
    {
        if (characterController.isGrounded &&
            Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1.5f) &&
            hit.collider.CompareTag("Lift"))
        {
            Transform lift = hit.collider.transform;

            if (lift == currentLift)
            {
                Vector3 delta = lift.position - lastLiftPosition;
                characterController.Move(delta);
            }

            currentLift = lift;
            lastLiftPosition = lift.position;
            return;
        }

        currentLift = null;
    }

    void UpdateInteraction()
    {
        Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hitInfo, 5f))
        {
            var obj = hitInfo.transform.gameObject;
            if (obj.TryGetComponent<IInteract>(out var interact))
            {
                _interactObjectShow.SetActive(true);
                _interactableObject = interact;
                return;
            }
        }
        _interactObjectShow.SetActive(false);
        _interactableObject = null;
    }
    public void TryToInteract(InputAction.CallbackContext context)
    {
        if (context.started)
            _interactableObject?.Interact();
    }
    void UpdateMovement()
    {
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        bool isRunning = Keyboard.current.leftShiftKey.isPressed;

        float curSpeedX = -(isRunning ? runningSpeed : walkingSpeed) * moveInput.y;
        float curSpeedY = -(isRunning ? runningSpeed : walkingSpeed) * moveInput.x;
        float movementDirectionY = moveDirection.y;

        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        if (characterController.isGrounded && jumpPressed)
        {
            moveDirection.y = jumpSpeed;
            jumpPressed = false;
        }
        else
        {
            moveDirection.y = movementDirectionY;
        }

        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        characterController.Move(moveDirection * Time.deltaTime);

        rotationX += lookInput.y * lookSpeed * Time.deltaTime;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        _playerEye.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.rotation *= Quaternion.Euler(0, lookInput.x * lookSpeed * Time.deltaTime, 0);
    }
}
