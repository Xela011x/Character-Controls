using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ThirdPersonMovement : MonoBehaviour
{
    [Header("Movements")]
    [SerializeField] private float turnSmoothTime = 0.15f;
    [SerializeField] private float speed = 250.0f;
    [SerializeField] private float jumpPower = 10.0f;
    [SerializeField] private float jumpDelay = 0.0f;

    [Header("GroundCheck")]
    [SerializeField] private bool grounded = false;
    [Space(10)]
    [SerializeField] private float groundCheckHeight = -1.0f;
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Gravity Settings")]
    [SerializeField] private bool seeConsoleFallVelocityOnLand = false;
    [SerializeField] private float gravityMultiplier = 2.0f;
    [SerializeField] private float gravityWhileGrounded = -3.0f;
    [SerializeField] private float fallVelocity;
    private float gravity = -9.81f;

    [Header("Others")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Rigidbody rgb;
    private Vector3 moveDir;
    private float turnSmoothVelocity;

    private void Start()
    {
        rgb = GetComponent<Rigidbody>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
        moveDir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

        if (Input.GetKeyDown(KeyCode.Space) && grounded) { StartCoroutine(Jump(jumpDelay)); }
    }

    private void ApplyForces()
    {
        Vector3 move = ThirdPersonMovements(moveDir) * speed * Time.deltaTime;

        move.y = fallVelocity;

        rgb.velocity = move;
    }

    private Vector3 ThirdPersonMovements(Vector3 moveDirection)
    {
        Vector3 forward = mainCamera.transform.forward;
        Vector3 right = mainCamera.transform.right;
        forward.y = 0;
        right.y = 0;

        Vector3 forwardRelativeVerticalInput = moveDirection.z * forward;
        Vector3 rightRelativeHorizontalInput = moveDirection.x * right;

        Vector3 cameraRelativeMovement = forwardRelativeVerticalInput + rightRelativeHorizontalInput;

        if (moveDirection.magnitude >= 0.01f)
        {
            float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg + mainCamera.transform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }

        return cameraRelativeMovement.normalized;
    }

    public void FallVelocity()
    {
        fallVelocity = grounded && fallVelocity < 0 ? gravityWhileGrounded : fallVelocity += gravity * gravityMultiplier * Time.deltaTime;
    }

    private IEnumerator Jump(float delay)
    {
        yield return new WaitForSeconds(delay);

        fallVelocity += jumpPower; print("gravity jump");
    }

    private void GroundCheck()
    {
        bool groundBool = grounded;

        grounded = Physics.CheckSphere(transform.position + (Vector3.up * groundCheckHeight), groundCheckRadius, groundLayer);

        if (groundBool != grounded)
        {
            if (seeConsoleFallVelocityOnLand && fallVelocity <= 0) { print("Fall Velocity: " + fallVelocity); }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // GroundCheck visuals
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + (Vector3.up * groundCheckHeight), groundCheckRadius);

    }
}
