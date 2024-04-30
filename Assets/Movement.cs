using Cinemachine;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Movement : MonoBehaviour
{
    [Header("Camera Views")]
    [SerializeField] private cameraView currentView = cameraView.FreeLook;
    private int number;
    private enum cameraView
    {
        FreeLook,
        FirstPerson,
        ThirdPerson,
        TOTAL_ENUM
    }

    [Header("Movements")]
    [SerializeField] private float turnSmoothTime = 0.15f;
    private float turnSmoothVelocity;
    [SerializeField] private float runSmoothTime = 0.5f;
    private float runSmoothVelocity;
    [SerializeField] private float walkSmoothTime = 0.15f;
    private float walkSmoothVelocity;
    [Space(10)]
    [SerializeField] private float currentSpeed;
    [SerializeField] private float walkSpeed = 250.0f;
    [SerializeField] private float runSpeed = 500.0f;
    [SerializeField] private float jumpPower = 10.0f;
    [SerializeField] private float jumpDelay = 0.0f;

    [Header("GroundCheck")]
    [SerializeField] private bool isGrounded = false;
    [Space(10)]
    [SerializeField] private float groundCheckHeight = -1.0f;
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Gravity Settings")]
    [SerializeField] private bool seeConsoleFallVelocityOnLand = false;
    [Space(10)]
    [SerializeField] private float gravityMultiplier = 2.0f;
    [SerializeField] private float gravityWhileGrounded = -3.0f;
    [SerializeField] private float fallVelocity;
    private float gravity = -9.81f;

    [Header("Others")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private CinemachineFreeLook freeLookCam;
    [SerializeField] private CinemachineVirtualCamera firstPersonCam;
    [SerializeField] private CinemachineFreeLook thirdPersonCam;
    [SerializeField] private Rigidbody rgb;
    private Vector3 moveDir;

    private void Start()
    {
        rgb = GetComponent<Rigidbody>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentView = cameraView.FreeLook;
        freeLookCam.enabled = true;
        firstPersonCam.enabled = false;
        thirdPersonCam.enabled = false;
    }

    private void Update()
    {
        MovementControls();
        GroundCheck();
        FallVelocity();
    }

    private void FixedUpdate()
    {
        ApplyForces();
    }

    private void MovementControls()
    {
        // WASD Inputs
        moveDir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

        // Hotkey for switching camera view
        if (Input.GetKeyDown(KeyCode.H))
        {
            currentView = (cameraView) (number++ % (int)cameraView.TOTAL_ENUM);
            
            switch (currentView)
            {
                case cameraView.FreeLook:
                    freeLookCam.enabled = true;

                    firstPersonCam.enabled = false;
                    thirdPersonCam.enabled = false;
                    break;
                case cameraView.FirstPerson:
                    firstPersonCam.enabled = true;

                    freeLookCam.enabled = false;
                    thirdPersonCam.enabled = false;
                    break;
                case cameraView.ThirdPerson:
                    thirdPersonCam.enabled = true;

                    freeLookCam.enabled = false;
                    firstPersonCam.enabled = false;
                    break;
            }
        }

        // Jump Input
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded) { StartCoroutine(Jump(jumpDelay)); }

        // Run Input
        float running = Mathf.SmoothDamp(currentSpeed, runSpeed, ref runSmoothVelocity, runSmoothTime);
        float walking = currentSpeed > walkSpeed ? Mathf.SmoothDamp(currentSpeed, walkSpeed, ref walkSmoothVelocity, walkSmoothTime) : walkSpeed;
        currentSpeed = Input.GetKey(KeyCode.LeftShift) ? running : walking;
    }

    private void ApplyForces()
    {
        Vector3 move = CameraRelativeMovements(moveDir) * currentSpeed * Time.deltaTime;
        move.y = fallVelocity;
        rgb.velocity = move;
    }

    private Vector3 CameraRelativeMovements(Vector3 moveDirection)
    {
        Vector3 forward = mainCamera.transform.forward;
        Vector3 right = mainCamera.transform.right;
        forward.y = 0;
        right.y = 0;

        Vector3 forwardRelativeVerticalInput = moveDirection.z * forward;
        Vector3 rightRelativeHorizontalInput = moveDirection.x * right;

        Vector3 cameraRelativeMovement = forwardRelativeVerticalInput + rightRelativeHorizontalInput;

        if (currentView == cameraView.FreeLook && moveDirection.magnitude >= 0.01f) { PlayerForwardToCamera(moveDirection, cameraView.FreeLook); }
        else if (currentView == cameraView.FirstPerson) { PlayerForwardToCamera(moveDirection, cameraView.FirstPerson); }
        else if (currentView == cameraView.ThirdPerson) { PlayerForwardToCamera(moveDirection, cameraView.ThirdPerson); }

        return cameraRelativeMovement.normalized;
    }

    private void PlayerForwardToCamera(Vector3 moveDirection, cameraView camState)
    {
        float horizontal = camState == cameraView.FirstPerson ? 0 : moveDirection.x;
        float vertical = camState == cameraView.ThirdPerson || camState == cameraView.FirstPerson ? Mathf.Abs(moveDirection.z) : moveDirection.z;

        float targetAngle = Mathf.Atan2(horizontal, vertical) * Mathf.Rad2Deg + mainCamera.transform.eulerAngles.y;
        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
        transform.rotation = Quaternion.Euler(0f, angle, 0f);
    }

    private void FallVelocity()
    {
        fallVelocity = isGrounded && fallVelocity < 0 ? gravityWhileGrounded : fallVelocity += gravity * gravityMultiplier * Time.deltaTime;
    }

    private IEnumerator Jump(float delay)
    {
        yield return new WaitForSeconds(delay);

        fallVelocity += jumpPower; print("gravity jump");
    }

    private void GroundCheck()
    {
        bool groundBool = isGrounded;

        isGrounded = Physics.CheckSphere(transform.position + (Vector3.up * groundCheckHeight), groundCheckRadius, groundLayer);

        if (groundBool != isGrounded) // maybe add fallVelocity <= 0 to happen only once
        {
            if (seeConsoleFallVelocityOnLand && fallVelocity <= 0) { print("Fall Velocity: " + fallVelocity); }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // GroundCheck visuals
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position + (Vector3.up * groundCheckHeight), groundCheckRadius);
    }
}
