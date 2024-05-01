using Cinemachine;
using System.Collections;
using UnityEngine;

/*
 * - on land make it so he cant move
 */

[RequireComponent(typeof(Rigidbody))]
public class Movement : AnimatorBrain
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
    public static Movement instance;

    //private readonly Animations[] idleAnimatiions =
    //{
    //    // Add all extra idle animations here
    //    Animations.Idle,
    //};
    //private int currentIdle = 0;

    private const int UPPERBODY = 0;
    private const int LOWERBODY = 1;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        Initialize(GetComponent<Animator>().layerCount, Animations.NONE, GetComponent<Animator>(), DefaultAnimation);
        
        rgb = GetComponent<Rigidbody>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentView = cameraView.FreeLook;
        freeLookCam.enabled = true;
        firstPersonCam.enabled = false;
        thirdPersonCam.enabled = false;

        //IEnumerator ChangeIdle()
        //{
        //    while (true)
        //    {
        //        yield return new WaitForSeconds(2);
        //        currentIdle++;
        //        if (currentIdle >= idleAnimatiions.Length) { currentIdle = 0; }
        //    }
        //}
    }

    private void Update()
    {
        MovementControls();
        GroundCheck();
        FallVelocity();

        CheckTopAnimation();
        CheckBottomAnimation();
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
        if (Input.GetKeyDown(KeyCode.Space) && Grounded) { StartCoroutine(Jump(jumpDelay)); }

        // Run Input
        float running = Mathf.SmoothDamp(currentSpeed, runSpeed, ref runSmoothVelocity, runSmoothTime);
        float walking = currentSpeed > walkSpeed ? Mathf.SmoothDamp(currentSpeed, walkSpeed, ref walkSmoothVelocity, walkSmoothTime) : walkSpeed;
        currentSpeed = Input.GetKey(KeyCode.LeftShift) ? running : walking;

        // For Testing Purposes

        if (Input.GetKeyDown(KeyCode.L)) { transform.position = Vector3.up * 100; }
    }

    #region Animations
    private void CheckTopAnimation()
    {
        CheckMovementAnimations(UPPERBODY);
    }

    private void CheckBottomAnimation()
    {
        CheckMovementAnimations(LOWERBODY);
    }

    private void CheckMovementAnimations(int layer)
    {
        if (currentView == cameraView.FreeLook)
        {
            if (fallVelocity < 0 && !Grounded) { Play(Animations.Falling, layer, false, false, 0.1f); }
            else if (moveDir.z != 0) { Play(Animations.Walking, layer, false, false); }
            else if (moveDir.x < 0) { Play(Animations.WalkingRight, layer, false, false); }
            else if (moveDir.x > 0) { Play(Animations.WalkingLeft, layer, false, false); }
            else { Play(Animations.Idle, layer, false, false); }
        }
        else if (currentView == cameraView.FirstPerson || currentView == cameraView.ThirdPerson)
        {
            if (fallVelocity < 0 && !Grounded) { Play(Animations.Falling, layer, false, false, 0.1f); }
            else if (moveDir.z > 0) { Play(Animations.Walking, layer, false, false); }
            else if (moveDir.z < 0) { Play(Animations.WalkingBackWard, layer, false, false); }
            else if (moveDir.x > 0) { Play(Animations.WalkingLeft, layer, false, false); }
            else if (moveDir.x < 0) { Play(Animations.WalkingRight, layer, false, false); }
            else { Play(Animations.Idle, layer, false, false); }
        }
        
        //else { Play(idleAnimatiions[currentIdle], layer, false, false); }
    }

    private void DefaultAnimation(int layer)
    {
        if (layer == UPPERBODY) { CheckTopAnimation(); }
        else {  CheckBottomAnimation(); }
    }
    #endregion

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
        fallVelocity = Grounded && fallVelocity < 0 ? gravityWhileGrounded : fallVelocity += gravity * gravityMultiplier * Time.deltaTime;
    }

    private IEnumerator Jump(float delay)
    {
        Play(Animations.Jumping, UPPERBODY, true, false, 0.05f);
        Play(Animations.Jumping, LOWERBODY, true, false, 0.05f);

        yield return new WaitForSeconds(delay);

        fallVelocity += jumpPower;
    }

    private void GroundCheck()
    {
        bool groundBool = Grounded;

        Grounded = Physics.CheckSphere(transform.position + (Vector3.up * groundCheckHeight), groundCheckRadius, groundLayer);

        if (groundBool != Grounded) // maybe add fallVelocity <= 0 to happen only once
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
