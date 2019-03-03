using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerSFX))]
public class PlayerController : MonoBehaviour
{

    #region Private Serializable Fields

    [SerializeField] private bool autoLedgeTarget = true;

    [Header("Movement Speeds")]
    [SerializeField] private float runSpeed = 3.49f;
    [SerializeField] private float walkSpeed = 1.345f;
    [SerializeField] private float stairSpeed = 2f;
    [SerializeField] private float swimSpeed = 3.5f;
    [SerializeField] private float treadSpeed = 1.2f;
    [SerializeField] private float slideSpeed = 5f;
    [SerializeField] private float speedChangeRate = 8f;

    [Header("Damage Rates")]
    [SerializeField] private float breathLossRate = 2f;
    [SerializeField] private float breathRecoveryRate = 2f;
    [SerializeField] private float underwaterDeathSpeed = 18f;

    [Header("Physics")]
    [SerializeField] private float gravity = 14f;
    [SerializeField] private float damageHeight = 7f;
    [SerializeField] private float deathHeight = 10f;
    [SerializeField] private float terminalVelocity = 30f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float runJumpVel = 4.5f;
    [SerializeField] private float standJumpVel = 3.5f;

    [Header("Offsets")]
    [SerializeField] private float grabForwardOffset = 0.11f;
    [SerializeField] private float grabUpOffset = 1.86f;
    [SerializeField] private float hangForwardOffset = 0.11f;
    [SerializeField] private float hangUpOffset = 2.05f;

    [Header("References")]
    [SerializeField] private CameraController camController;
    [SerializeField] private Transform waistBone;
    [SerializeField] private Transform headBone;

    #endregion

    #region Private Fields

    private bool isGrounded = true;
    private bool groundedLastFrame = true;  // True if player was grounded on previous frame
    private bool useGravity = true;  // Gravity has no effect if false
    private bool groundedOnSteps = false;  // Allows player to walk off steps without ungrounding
    private bool forceWaistRotation = false;
    private bool forceHeadLook = false;
    private bool isMovingAuto = false;  // Player is automatically moving somewhere (i.e. with no player input)
    private bool useRootMotion = false; // Allows root motion movement that is compatible with char controller
    private float verticalSpeed = 0f;  // From the effect of gravity only
    private float jumpYVel = 0f;  // Velocity calculated from jump height
    private float damageVelocity = 0f;  // Minimum velocity at which fall damage will occur
    private float deathVelocity = 0f;  // Falling velocity at which player will die
    private float targetAngle = 0f;
    private float targetSpeed = 0f;

    private StateMachine<PlayerController> stateMachine;  // Player's general state machine
    private StateMachine<PlayerController> upperStateMachine;  // State machine referring to what's above the waist
    private CharacterController charControl;
    private PlayerInput playerInput;
    private Transform cam;
    private Animator anim;
    private PlayerStats playerStats;
    private PlayerSFX playerSFX;
    private WeaponManager weaponManager;
    private Transform waistTarget;
    private Quaternion waistRotation;
    private Vector3 headLookAt;
    private Vector3 velocity;
    private Vector3 localVelocity;
    private Vector3 movementOffset; // Final movement amount (to keep gravity effect separate)
    private Vector3 velocityLastFrame; // Velocity from previous frame
    private GroundInfo groundInfo;

    #endregion

    #region Private Methods

    private void Awake()
    {
        // Calc jump speeds - v^2 = u^2 + 2as
        jumpYVel = Mathf.Sqrt(2f * jumpHeight * gravity);

        damageVelocity = Mathf.Sqrt(2f * gravity * damageHeight);
        deathVelocity = Mathf.Sqrt(2f * gravity * deathHeight);

        velocity = Vector3.zero;
        localVelocity = Vector3.zero;
    }

    private void Start()
    {
        charControl = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
        cam = camController.GetComponentInChildren<Camera>().transform;
        anim = GetComponent<Animator>();
        playerSFX = GetComponent<PlayerSFX>();
        playerStats = GetComponent<PlayerStats>();
        weaponManager = GetComponent<WeaponManager>();

        stateMachine = new StateMachine<PlayerController>(this);
        upperStateMachine = new StateMachine<PlayerController>(this);

        SetUpStateMachine();
    }

    private void SetUpStateMachine()
    {
        stateMachine.AddState(new Empty());
        stateMachine.AddState(new Locomotion());
        stateMachine.AddState(new Combat());
        stateMachine.AddState(new CombatJumping());
        stateMachine.AddState(new Climbing());
        stateMachine.AddState(new Freeclimb());
        stateMachine.AddState(new Drainpipe());
        stateMachine.AddState(new Ladder());
        stateMachine.AddState(new Crouch());
        stateMachine.AddState(new Dead());
        stateMachine.AddState(new InAir());
        stateMachine.AddState(new Jumping());
        stateMachine.AddState(new Swimming());
        stateMachine.AddState(new Grabbing());
        stateMachine.AddState(new AutoGrabbing());
        stateMachine.AddState(new MonkeySwing());
        stateMachine.AddState(new Sliding());

        upperStateMachine.AddState(new Empty());
        upperStateMachine.AddState(new UpperCombat());

        stateMachine.GoToState<Locomotion>();
        upperStateMachine.GoToState<Empty>();
    }

    private void Update()
    {
        if (RingMenu.isPaused)
        {
            anim.speed = 0f;
            return;
        }
        else
        {
            anim.speed = 1f;
        }

        CheckForGround();

        if (useGravity)
            ApplyGravity();

        stateMachine.Update();
        upperStateMachine.Update();

        UpdateAnimator();

        BuildMovementOffset();

        SlideOffSlopeLimit();

        groundInfo.Normal = Vector3.zero;

        if (charControl.enabled)
            charControl.Move(movementOffset * Time.deltaTime);
    }

    private void LateUpdate()
    {
        if (forceWaistRotation)
        {
            waistBone.rotation = waistRotation;

            // Correction for faulty bone
            // IF NEW MODEL CAUSES ISSUES, MESS WITH THIS
            waistBone.rotation = Quaternion.Euler(
                waistBone.eulerAngles.x - 90f, waistBone.eulerAngles.y,
                waistBone.eulerAngles.z);
        }

        velocityLastFrame = movementOffset;
        groundedLastFrame = isGrounded;
    }

    private void CheckForGround()
    {
        if (groundInfo.Angle > charControl.slopeLimit && groundInfo.Tag != "Slope")
            isGrounded = false;
        else
            isGrounded = charControl.isGrounded;

        // This downwards ray is more accurate than the capsule information
        // its not always available and player gets stuck if its not so we still use collider hits
        
        RaycastHit hit;

        float castDist = charControl.stepOffset + charControl.skinWidth + charControl.radius;
        Vector3 centerStart = transform.position + Vector3.up * (charControl.radius + charControl.skinWidth);

        if (groundedOnSteps && Physics.Raycast(centerStart, Vector3.down, out hit, castDist, ~(1 << 8), QueryTriggerInteraction.Ignore))
        {
            float distance = transform.position.y - hit.point.y;

            // allows player to run of steps properly
            if (distance <= charControl.stepOffset || hit.collider.tag.Equals("Slope"))
            {
                isGrounded = true;

                groundInfo.Distance = distance;
                groundInfo.Angle = UMath.GroundAngle(hit.normal);
                groundInfo.Tag = hit.collider.tag;
                groundInfo.Normal = hit.normal;
            }
        }

        anim.SetBool("isGrounded", isGrounded);
        anim.SetFloat("groundDistance", groundInfo.Distance);
        anim.SetFloat("groundAngle", groundInfo.Angle);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.normal.y < 0f)
            return;  // Normal is pointing down so mustn't be ground

        if (hit.normal.y > groundInfo.Normal.y)
        {
            groundInfo.Distance = transform.position.y - hit.point.y;
            groundInfo.Angle = UMath.GroundAngle(hit.normal);
            groundInfo.Tag = hit.collider.tag;
            groundInfo.Normal = hit.normal;
        }
    }

    private void ApplyGravity()
    {
        if (isGrounded)
        {
            // Keeps player grounded properly
            verticalSpeed = -gravity;
        }

        verticalSpeed -= gravity * Time.deltaTime;

        verticalSpeed = Mathf.Clamp(verticalSpeed, -terminalVelocity, terminalVelocity);
    }

    // Method that stops player landing on non-slideable slopes
    private void SlideOffSlopeLimit()
    {
        if (groundInfo.Angle > charControl.slopeLimit && groundInfo.Tag != "Slope")
        {
            movementOffset = Vector3.ProjectOnPlane(movementOffset, groundInfo.Normal);

            // Stops player getting stuck on an edge
            if (movementOffset == Vector3.zero)
            {
                Vector3 slopeRight = Vector3.Cross(Vector3.up, groundInfo.Normal);
                Vector3 slopeDirection = Vector3.Cross(slopeRight, groundInfo.Normal).normalized;

                movementOffset = slopeDirection;
            }
        }
    }

    private void BuildMovementOffset()
    {
        movementOffset = (useRootMotion ? Vector3.zero : velocity) + (Vector3.up * verticalSpeed);
    }

    private void OnAnimatorIK()
    {
        if (forceHeadLook)
        {
            anim.SetLookAtWeight(1f);
            anim.SetLookAtPosition(headLookAt);
        }
    }

    public void MoveWait(Vector3 point, Quaternion rotation, float tRate = 1f, float rRate = 1f)
    {
        StartCoroutine(MoveTo(point, rotation, tRate, rRate));
    }

    private IEnumerator MoveTo(Vector3 point, Quaternion rotation, float tRate = 1f, float rRate = 1f)
    {
        bool originalRMVal = UseRootMotion;

        UseRootMotion = false;
        StopMoving();

        float distance = Vector3.Distance(transform.position, point);
        float difference = Quaternion.Angle(transform.rotation, rotation);
        Vector3 direction = (point - transform.position).normalized;
        bool notInPosition = true;

        isMovingAuto = true;
        anim.SetBool("isWaiting", true);

        while (notInPosition)
        {
            notInPosition = false;

            if (Mathf.Abs(distance) > 0.05f)
            {
                notInPosition = true;
                transform.position = Vector3.MoveTowards(transform.position, point, tRate * Time.deltaTime);
                distance = Vector3.Distance(transform.position, point);
            }
            else
            {
                StopMoving();
            }

            if (Mathf.Abs(difference) > 5f)
            {
                notInPosition = true;
                transform.rotation = Quaternion.Lerp(transform.rotation, rotation, rRate * Time.deltaTime);
                difference = Quaternion.Angle(transform.rotation, rotation);
            }

            yield return null;
        }

        transform.position = point;
        transform.rotation = rotation;

        UseRootMotion = originalRMVal;

        isMovingAuto = false;
        anim.SetBool("isWaiting", false);
    }

    private void UpdateAnimator()
    {
        AnimatorStateInfo animState = anim.GetCurrentAnimatorStateInfo(0);
        float animTime = animState.normalizedTime <= 1.0f ? animState.normalizedTime
            : animState.normalizedTime % (int)animState.normalizedTime;

        anim.SetFloat("AnimTime", animTime);  // Used for determining certain transitions
    }

    private Vector3 VelocityToLocal(Vector3 vel)
    {
        return cam.InverseTransformVector(vel);
    }

    private Vector3 VelocityToGlobal(Vector3 vel)
    {
        return cam.TransformVector(vel);
    }

    #endregion

    #region Public Methods

    public Vector3 RawTargetVector(float speed = 1f, bool cameraRelative = false)
    {
        float horizontal = Input.GetAxisRaw(playerInput.horizontalAxis);
        float vertical = Input.GetAxisRaw(playerInput.verticalAxis);

        Vector3 directInput = new Vector3(horizontal, 0f, vertical);

        if (directInput.magnitude > 1f)
            directInput.Normalize();  // Stops running too fast

        directInput *= speed;

        if (cameraRelative)
            directInput = Quaternion.Euler(0f, cam.eulerAngles.y, 0f) * directInput;

        return directInput;
    }

    public void ImpulseVelocity(Vector3 vel, bool reset = true)
    {
        if (reset)
            StopMoving();

        velocity += Vector3.Scale(vel, new Vector3(1f, 0f, 1f));
        localVelocity = VelocityToLocal(velocity);
        verticalSpeed += vel.y;
    }

    public void StopMoving()
    {
        velocity = Vector3.zero;
        localVelocity = Vector3.zero;
        verticalSpeed = 0f;
    }

    public void ResetVerticalSpeed()
    {
        verticalSpeed = 0f;
    }

    public void MoveGrounded(float speed)
    {
        Vector3 targetVector = RawTargetVector(speed);

        targetAngle = Vector3.SignedAngle(Vector3.forward, targetVector.normalized, Vector3.up);
        targetSpeed = UMath.GetHorizontalMag(targetVector);

        anim.SetFloat("SignedTargetAngle", targetAngle);
        anim.SetFloat("TargetAngle", Mathf.Abs(targetAngle));
        anim.SetFloat("TargetSpeed", targetSpeed);

        // Allows Lara to smoothly take off
        if (localVelocity.sqrMagnitude == 0f && targetVector.magnitude > 0.1f)
        {
            Vector3 camForward = cam.forward;
            camForward.y = 0f;
            camForward.Normalize();

            // Finds correct forward direction for local velocity
            localVelocity = Quaternion.FromToRotation(camForward, transform.forward) * (Vector3.forward * 0.1f);
        }

        localVelocity = Vector3.Slerp(localVelocity, targetVector, Time.deltaTime * speedChangeRate);

        velocity = Quaternion.Euler(0f, cam.eulerAngles.y, 0f) * localVelocity;

        float actualSpeed = direction * UMath.GetHorizontalMag(velocity);
        anim.SetFloat("Speed", actualSpeed);
    }

    // Move on all axis not just horizontally
    public void MoveFree(float speed, float maxTurnAngle = 40f)
    {
        Vector3 targetVector = cam.transform.TransformDirection(RawTargetVector());

        if (velocity.magnitude < 0.1f && targetVector.magnitude > 0f)
            Velocity = transform.forward * 0.1f;  // Player will rotate smoothly from idle

        if (Vector3.Angle(velocity.normalized, targetVector) > maxTurnAngle)
        {
            Vector3 direction = Vector3.Cross(velocity.normalized, targetVector);
            targetVector = Quaternion.AngleAxis(maxTurnAngle, direction) * velocity.normalized;
        }

        targetVector *= speed;

        Velocity = Vector3.Slerp(velocity, targetVector, Time.deltaTime * speedChangeRate);

        anim.SetFloat("Speed", velocity.magnitude);
        anim.SetFloat("TargetSpeed", targetVector.magnitude);
    }

    public void MoveInDirection(float speed, Vector3 direction)
    {
        Vector3 finalVelocity = speed * direction;

        Velocity = finalVelocity;
    }

    public void RotateToCamera()
    {
        if (UMath.GetHorizontalMag(velocity) > 0.1f)
        {
            Quaternion target = Quaternion.LookRotation(cam.transform.forward, Vector3.up);
            target = Quaternion.Euler(0f, target.eulerAngles.y, 0f);
            transform.rotation = target;
        }
    }

    public void RotateToVelocityGround(float smoothing = 0f)
    {
        direction = 1;

        if (UMath.GetHorizontalMag(velocity) < 0.1f)
            return;

        Quaternion target = Quaternion.Euler(0.0f, Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg, 0.0f);

        if (smoothing == 0f)
            transform.rotation = target;
        else
            transform.rotation = Quaternion.Slerp(transform.rotation, target, smoothing * Time.deltaTime);
    }

    int direction = 1;
    bool adjustRotCombat = false;

    // This method is necessary as Lara must run backwards 50% of the time
    public void RotateToVelocityStrafe(float smoothing = 8f)
    {
        if (UMath.GetHorizontalMag(velocity) < 0.1f)
            return;

        float theAngle = Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg;

        if (targetAngle < -50f || targetAngle > 140f)
        {
            if (direction != -1)
                adjustRotCombat = true;
            direction = -1;
        }
        else if (targetAngle > -40f && targetAngle < 130f)
        {
            if (direction != 1)
                adjustRotCombat = true;
            direction = 1;
        }

        if (direction == -1)
            theAngle += 180f;

        Quaternion target = Quaternion.Euler(0.0f, theAngle, 0.0f);
        if (!adjustRotCombat)
        {
            transform.rotation = target;
        }
        else
        {
            if (Mathf.Abs(Quaternion.Angle(target, transform.rotation)) > 10f)
                transform.rotation = Quaternion.Slerp(transform.rotation, target, smoothing * Time.deltaTime);
            else
                adjustRotCombat = false;
        }
    }

    public void RotateToVelocity(float smoothing = 0f)
    {
        direction = 1;

        if (velocity.magnitude < 0.1f)
            return;

        Quaternion target = Quaternion.LookRotation(velocity);

        if (smoothing == 0f)
            transform.rotation = target;
        else
            transform.rotation = Quaternion.Slerp(transform.rotation, target, smoothing * Time.deltaTime);
    }

    public void RotateToTarget(Vector3 target)
    {
        Vector3 direction = Vector3.Scale(target - transform.position, new Vector3(1.0f, 0.0f, 1.0f));
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    public void MinimizeCollider()
    {
        charControl.radius = 0f;
    }

    public void MaximizeCollider()
    {
        charControl.radius = 0.2f;
    }

    public void DisableCharControl()
    {
        charControl.enabled = false;
    }

    public void EnableCharControl()
    {
        charControl.enabled = true;
    }

    #endregion

    #region Public Properties

    public StateMachine<PlayerController> StateMachine
    {
        get { return stateMachine; }
    }

    public StateMachine<PlayerController> UpperStateMachine
    {
        get { return upperStateMachine; }
    }

    public CharacterController CharControl
    {
        get { return charControl; }
    }

    public CameraController CamControl
    {
        get { return camController; }
    }

    public Transform Cam
    {
        get { return cam; }
    }

    public Transform WaistTarget
    {
        get { return waistTarget; }
        set { waistTarget = value; }
    }

    public Quaternion WaistRotation
    {
        get { return WaistRotation; }
        set { waistRotation = value; }
    }

    public Vector3 HeadLookAt
    {
        get { return headLookAt; }
        set { headLookAt = value; }
    }

    public Animator Anim
    {
        get { return anim; }
    }

    public PlayerInput Inputf
    {
        get { return playerInput; }
    }

    public PlayerSFX SFX
    {
        get { return playerSFX; }
    }

    public PlayerStats Stats
    {
        get { return playerStats; }
    }

    public WeaponManager Weapons
    {
        get { return weaponManager; }
    }

    public GroundInfo Ground
    {
        get { return groundInfo; }
    }

    public bool Grounded
    {
        get { return isGrounded; }
    }

    public bool WasGrounded
    {
        get { return groundedLastFrame; }
    }

    public bool IsMovingAuto
    {
        get { return isMovingAuto; }
    }

    public bool AutoLedgeTarget
    {
        get { return autoLedgeTarget; }
    }

    public bool UseGravity
    {
        get { return useGravity; }
        set
        {
            useGravity = value;

            if (useGravity == false)
                verticalSpeed = 0f;
        }
    }

    public bool UseRootMotion
    {
        get { return useRootMotion; }
        set
        {
            useRootMotion = anim.applyRootMotion = value;
        }
    }

    public bool GroundedOnSteps
    {
        get { return groundedOnSteps; }
        set { groundedOnSteps = value; }
    }

    public bool ForceWaistRotation
    {
        get { return forceWaistRotation; }
        set { forceWaistRotation = value; }
    }

    public bool ForceHeadLook
    {
        get { return forceHeadLook; }
        set { forceHeadLook = value; }
    }

    public float Gravity
    {
        get { return gravity; }
    }

    public float RunSpeed
    {
        get { return runSpeed; }
    }

    public float WalkSpeed
    {
        get { return walkSpeed; }
    }

    public float SlideSpeed
    {
        get { return slideSpeed; }
    }

    public float SwimSpeed
    {
        get { return swimSpeed; }
    }

    public float TreadSpeed
    {
        get { return treadSpeed; }
    }

    public float JumpHeight
    {
        get { return jumpHeight; }
    }

    public float JumpYVel
    {
        get { return jumpYVel; }
    }

    public float RunJumpVel
    {
        get { return runJumpVel; }
    }

    public float StandJumpVel
    {
        get { return standJumpVel; }
    }

    public float DamageVelocity
    {
        get { return damageVelocity; }
    }

    public float DeathVelocity
    {
        get { return deathVelocity; }
    }

    public float BreathLossRate
    {
        get { return breathLossRate; }
    }

    public float BreathRecoveryRate
    {
        get { return breathRecoveryRate; }
    }

    public float WaterDeathSpeed
    {
        get { return underwaterDeathSpeed; }
    }

    public float TargetAngle
    {
        get { return targetAngle; }
    }

    public float TargetSpeed
    {
        get { return targetSpeed;  }
    }

    public float HangUpOffset
    {
        get { return hangUpOffset; }
    }

    public float HangForwardOffset
    {
        get { return hangForwardOffset; }
    }

    public float GrabUpOffset
    {
        get { return grabUpOffset; }
    }

    public float GrabForwardOffset
    {
        get { return grabForwardOffset; }
    }

    public float VerticalSpeed
    {
        get { return verticalSpeed; }
    }

    public Vector3 Velocity
    {
        get { return velocity; }
        set
        {
            velocity = value;
            localVelocity = VelocityToLocal(value);
        }
    }

    public Vector3 LocalVelocity
    {
        get { return localVelocity; }
        set
        {
            localVelocity = value;
            velocity = VelocityToGlobal(value);
        }
    }

    public Vector3 VelocityLastFrame
    {
        get { return velocityLastFrame; }
    }

    #endregion
}
