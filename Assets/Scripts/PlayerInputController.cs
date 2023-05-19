using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputController : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private float moveSpeed = 4.0f;
    public float MoveSpeed { get { return moveSpeed; } set { moveSpeed = value; } }
    [SerializeField] private float sprintSpeed = 6.0f;
    public float SprintSpeed { get { return sprintSpeed; } set { sprintSpeed = value; } }
    [SerializeField] private float rotationSpeed = 1.0f;
    public float RotationSpeed => rotationSpeed;
    [SerializeField] private float speedChangeRate = 10.0f;
    public float SpeedChangeRate => speedChangeRate;

    [Space(10)]
    [SerializeField] private float jumpHeight = 1.2f;
    public float JumpHeight => jumpHeight;
    [SerializeField] private float gravity = -15.0f;
    public float Gravity => gravity;

    [Space(10)]
    [SerializeField] private float jumpTimeout = 0.1f;
    public float JumpTimeout => jumpTimeout;
    [SerializeField] private float FallTimeout = 0.15f;
    public float fallTimeout => fallTimeout;

    [Header("Player Grounded")]
    [SerializeField] private bool grounded = true;
    public bool Grounded { get { return grounded; } set { grounded = value; } }
    [SerializeField] private float groundedOffset = -0.14f;
    public float GroundedOffset => groundedOffset;
    [SerializeField] private float GroundedRadius = 0.5f;
    public float groundedRadius => GroundedRadius;
    [SerializeField] private LayerMask GroundLayers;
    public LayerMask groundLayers => GroundLayers;

    [Header("Cinemachine")]
    [SerializeField] private GameObject cinemachineTarget;
    [SerializeField] private float topClamp = 90.0f;
    public float TopClamp => topClamp;
    [SerializeField] private float bottomClamp = -90.0f;
    public float BottomClamp => bottomClamp;


    //Player
    private float _speed;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;

    // timeout deltatime
    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

    //Cinemachine
    float _cinemachineTargetPitch;

    //References
    CharacterController _playerCC;
    PlayerActionsInput _playerActions;
    PlayerInput _playerInput;

    //CheckInput
    private bool IsCurrentDeviceMouse
    {
        get { return _playerInput.currentControlScheme == "Keyboard&Mouse"; }
    }

    //Constants
    private const float _threshold = 0.01f;


    void Start()
    {
        _playerCC = GetComponent<CharacterController>();
        _playerActions = GetComponent<PlayerActionsInput>();
        _playerInput = GetComponent<PlayerInput>();
    }

    void Update()
    {
        JumpAndGravity();
        GroundedCheck();
        Move();
    }

    void LateUpdate()
    {
        CameraRotation();
    }

    void CameraRotation()
    {
        if (_playerActions.Look.sqrMagnitude >= _threshold)
        {
            //If is in mouse dont multiply by deltaTime
            float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

            _cinemachineTargetPitch += _playerActions.Look.y * RotationSpeed * deltaTimeMultiplier;
            _rotationVelocity = _playerActions.Look.x * RotationSpeed * deltaTimeMultiplier;

            //Clamp blocking look more than angle passed in bottom and top clamps
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            //Rotate camera target by pitch 
            cinemachineTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

            //Rotates the Player
            transform.Rotate(Vector3.up * _rotationVelocity);
        }
    }

    void Move()
    {
        //set target speed based on move speed, sprint speed and if sprint is pressed
        float targetSpeed = _playerActions.Sprint ? SprintSpeed : MoveSpeed;

        //a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

        //note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        //if there is no input, set the target speed to 0
        if (_playerActions.Move == Vector2.zero) targetSpeed = 0.0f;

        //a reference to the players current horizontal velocity
        float currentHorizontalSpeed = new Vector3(_playerCC.velocity.x, 0.0f, _playerCC.velocity.z).magnitude;

        float speedOffset = 0.1f;
        //      float inputMagnitude = _playerActions.analogMovement ? _playerActions.Move.magnitude : 1f; //Gamepad
        float inputMagnitude = 1f;

        //accelerate or decelerate to target speed
        if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            //creates curved result rather than a linear one giving a more organic speed change
            //note T in Lerp is clamped, so we don't need to clamp our speed
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

            //round speed to 3 decimal places
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else
        {
            _speed = targetSpeed;
        }

        //normalise input direction
        Vector3 inputDirection = new Vector3(_playerActions.Move.x, 0.0f, _playerActions.Move.y).normalized;

        //note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        //if there is a move input rotate player when the player is moving
        if (_playerActions.Move != Vector2.zero)
        {
            //move
            inputDirection = transform.right * _playerActions.Move.x + transform.forward * _playerActions.Move.y;
        }

        //move the player
        _playerCC.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
    }

    private void JumpAndGravity()
    {
        if (Grounded)
        {
            // reset the fall timeout timer
            _fallTimeoutDelta = FallTimeout;

            // stop our velocity dropping infinitely when grounded
            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
            }

            // Jump
            if (_playerActions.Jump && _jumpTimeoutDelta <= 0.0f)
            {
                // the square root of H * -2 * G = how much velocity needed to reach desired height
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
            }

            // jump timeout
            if (_jumpTimeoutDelta >= 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else
        {
            // reset the jump timeout timer
            _jumpTimeoutDelta = JumpTimeout;

            // fall timeout
            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }

            // if we are not grounded, do not jump
            _playerActions.Jump = false;
        }

        // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
        if (_verticalVelocity < _terminalVelocity)
        {
            _verticalVelocity += Gravity * Time.deltaTime;
        }
    }

    private void GroundedCheck()
    {
        // set sphere position, with offset
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void OnDrawGizmosSelected()
    {
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        if (Grounded) Gizmos.color = transparentGreen;
        else Gizmos.color = transparentRed;

        // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
        Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
    }
}
