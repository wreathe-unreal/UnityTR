using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerSFX))]
public class PlayerController : MonoBehaviour
{
    public bool autoLedgeTarget = true;
    [Header("Movement Speeds")]
    public float runSpeed = 3.49f;
    public float walkSpeed = 1.44f;
    public float stairSpeed = 2f;
    public float swimSpeed = 2f;
    public float treadSpeed = 1.2f;
    public float slideSpeed = 5f;
    [Header("Physics")]
    public float gravity = 9.8f;
    public float damageHeight = 7f;
    public float deathHeight = 12f;
    [Header("Jump Settings")]
    public float jumpHeight = 1.2f;
    public float runJumpVel = 4.5f;
    public float standJumpVel = 3.5f;
    [Header("IK Settings")]
    public float footYOffset = 0.1f;
    [Header("Offsets")]
    public float grabForwardOffset = 0.11f;
    public float grabUpOffset = 1.56f;
    public float hangForwardOffset = 0.11f;
    public float hangUpOffset = 1.975f;

    [Header("References")]
    public CameraController camController;
    public Transform waistBone;
    public Transform headBone;
    [Header("Ragdoll")]
    public Rigidbody[] ragRigidBodies;

    private bool isGrounded = true;
    private bool considerStepOffset = false;
    private bool isFootIK = false;
    private bool holdRotation = false;
    private bool forceWaistRotation = false;
    private bool forceHeadLook = false;
    private float jumpYVel = 0f;
    private float damageVelocity = 0f;
    private float deathVelocity = 0f;
    private float combatAngle = 0f;
    [HideInInspector]
    public bool isMovingAuto = false;
    private float targetAngle = 0f;
    private float targetSpeed = 0f;

    private StateMachine<PlayerController> stateMachine;
    private StateMachine<PlayerController> upperStateMachine;
    [HideInInspector]
    public CharacterController charControl;
    [HideInInspector]
    public PlayerInput playerInput;
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
    private GroundInfo groundInfo;

    private void Awake()
    {
        DisableRagdoll();

        // Calc jump speeds - v^2 = u^2 + 2as
        jumpYVel = Mathf.Sqrt(2f * jumpHeight * gravity);

        damageVelocity = Mathf.Sqrt(2 * gravity * damageHeight);
        deathVelocity = Mathf.Sqrt(2 * gravity * deathHeight);
    }

    private void Start()
    {
        charControl = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
        cam = camController.GetComponentInChildren<Camera>().transform;
        anim = GetComponent<Animator>();
        playerSFX = GetComponent<PlayerSFX>();
        playerStats = GetComponent<PlayerStats>();
        playerStats.HideCanvas();
        velocity = Vector3.zero;
        localVelocity = Vector3.forward;
        stateMachine = new StateMachine<PlayerController>(this);
        upperStateMachine = new StateMachine<PlayerController>(this);
        weaponManager = GetComponent<WeaponManager>();
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
        stateMachine.AddState(new HorPole());
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

        stateMachine.Update();
        upperStateMachine.Update();

        UpdateAnimator();

        SlideOffSlopeLimit();

        if (charControl.enabled)
            charControl.Move((anim.applyRootMotion ? Vector3.Scale(velocity, Vector3.up) : velocity) * Time.deltaTime);
    }

    private void SlideOffSlopeLimit()
    {
        if (groundInfo.Angle > charControl.slopeLimit && velocity.y < 0f && groundInfo.Tag != "Slope")
        {
            // Test if we are on sharp edge or not
            RaycastHit testHit;
            Vector3 castTestDir = -new Vector3(groundInfo.Normal.x, 0f, groundInfo.Normal.z).normalized;

            if (Physics.Raycast(transform.position, castTestDir, out testHit, charControl.radius + 0.1f))
            {
                if (Vector3.Angle(Vector3.up, testHit.normal) >= 90f)
                    return;
            }

            Vector3 slopeRight = Vector3.Cross(Vector3.up, groundInfo.Normal).normalized;
            Vector3 slopeDirection = Vector3.Cross(slopeRight, groundInfo.Normal).normalized;

            Quaternion rotater = Quaternion.FromToRotation(velocity.normalized, slopeDirection);

            velocity = Vector3.Project(velocity, slopeDirection);
        }
    }

    private void CheckForGround()
    {
        isGrounded = false;

        groundInfo.Distance = 2f;
        groundInfo.Angle = 0f;
        groundInfo.Tag = "";
        groundInfo.Normal = Vector3.up;

        RaycastHit groundHit;

        Vector3 sphereStart = transform.position + Vector3.up * charControl.radius;

        if (Physics.SphereCast(sphereStart, charControl.radius, Vector3.down, out groundHit, charControl.skinWidth + 0.1f, ~(1 << 8), QueryTriggerInteraction.Ignore))
        {
            isGrounded = true;
            groundInfo.Distance = transform.position.y - groundHit.point.y;
            groundInfo.Angle = UMath.GroundAngle(groundHit.normal);
            groundInfo.Tag = groundHit.collider.tag;
            groundInfo.Normal = groundHit.normal;
        }
        else if (charControl.isGrounded)
        {
            isGrounded = true;
        }

        if (true)
        {
            float castDist = charControl.stepOffset + charControl.skinWidth + charControl.radius;

            Vector3 centerStart = transform.position 
                + Vector3.up * charControl.radius 
                + transform.forward * charControl.radius;

            centerStart = transform.position
            + Vector3.up * charControl.radius;

            Debug.DrawRay(centerStart, Vector3.down * castDist, Color.white);
            if (Physics.Raycast(centerStart, Vector3.down, out groundHit, castDist)
                && !groundHit.collider.CompareTag("Water"))
            {
                groundInfo.Distance = transform.position.y - groundHit.point.y;
                groundInfo.Angle = UMath.GroundAngle(groundHit.normal);
                groundInfo.Tag = groundHit.collider.tag;
                groundInfo.Normal = groundHit.normal;
            }

            if (considerStepOffset && groundInfo.Distance < charControl.stepOffset)
                isGrounded = true;

            if (groundInfo.Angle > charControl.slopeLimit && groundInfo.Tag != "Slope")
                isGrounded = false;
        }

        anim.SetBool("isGrounded", isGrounded);
        anim.SetFloat("groundDistance", groundInfo.Distance);
        anim.SetFloat("groundAngle", groundInfo.Angle);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        //stateMachine.SendMessage(hit);
    }

    private void OnAnimatorIK()
    {
        if (forceHeadLook)
        {
            anim.SetLookAtWeight(1f);
            anim.SetLookAtPosition(headLookAt);
        }
    }

    public void DisableRagdoll()
    {
        foreach (Rigidbody rb in ragRigidBodies)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.gameObject.GetComponent<Collider>().enabled = false;
        }
    }

    public void EnableRagdoll()
    {
        foreach (Rigidbody rb in ragRigidBodies)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.gameObject.GetComponent<Collider>().enabled = true;
        }
    }

    private void LateUpdate()
    {
        if (forceWaistRotation)
        {
            waistBone.rotation = waistRotation;

            // Correction for faulty bone
            // IF NEW MODEL CAUSES ISSUES MESS WITH THIS
            waistBone.rotation = Quaternion.Euler(
                waistBone.eulerAngles.x - 90f, waistBone.eulerAngles.y,
                waistBone.eulerAngles.z);
        }
    }

    public void AnimWait(float seconds)
    {
        StartCoroutine(StopDrop(seconds));
    }

    public void MoveWait(Vector3 point, Quaternion rotation, float tRate = 1f, float rRate = 1f)
    {
        StartCoroutine(MoveTo(point, rotation, tRate, rRate));
    }

    private IEnumerator StopDrop(float secs)
    {
        float startTime = Time.time;
        anim.SetBool("isWaiting", true);
        while (Time.time - startTime < secs)
        {
            yield return null;
        }
        anim.SetBool("isWaiting", false);
    }

    private IEnumerator MoveTo(Vector3 point, Quaternion rotation, float tRate = 1f, float rRate = 1f)
    {
        anim.applyRootMotion = false;

        velocity = Vector3.zero;

        float distance = Vector3.Distance(transform.position, point);
        float difference = Quaternion.Angle(transform.rotation, rotation);
        Vector3 direction = (point - transform.position).normalized;
        bool isNotOk = true;

        isMovingAuto = true;
        anim.SetBool("isWaiting", true);

        while (isNotOk)
        {
            isNotOk = false;

            if (Mathf.Abs(distance) > 0.05f)
            {
                isNotOk = true;
                transform.position = Vector3.Lerp(transform.position, point, tRate * Time.deltaTime);
                distance = Vector3.Distance(transform.position, point);
            }
            else
            {
                velocity = Vector3.zero;
            }

            if (Mathf.Abs(difference) > 5f)
            {
                isNotOk = true;
                transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rRate * Time.deltaTime);
                difference = Quaternion.Angle(transform.rotation, rotation);
            }

            yield return null;
        }

        transform.position = point;
        transform.rotation = rotation;
        velocity = Vector3.zero;

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

    public void MoveGrounded(float speed, bool pushDown = true, float smoothing = 8f)
    {
        Vector3 targetVector = RawTargetVector(speed);

        targetAngle = Vector3.SignedAngle(Vector3.forward, targetVector.normalized, Vector3.up);
        targetSpeed = UMath.GetHorizontalMag(targetVector);

        anim.SetFloat("SignedTargetAngle", targetAngle);
        anim.SetFloat("TargetAngle", Mathf.Abs(targetAngle));
        anim.SetFloat("TargetSpeed", targetSpeed);

        // Allows Lara to smoothly take off
        if (localVelocity == Vector3.zero && targetVector.magnitude > 0.1f)
        {
            Vector3 camForward = cam.forward;
            camForward.y = 0f;
            camForward.Normalize();

            localVelocity = Quaternion.FromToRotation(camForward, transform.forward) * (Vector3.forward * 0.1f);
        }

        if (Mathf.Abs(targetVector.magnitude - localVelocity.magnitude) < 0.1f && Vector3.Angle(localVelocity, targetVector) < 1f)
            localVelocity = targetVector;
        else
            localVelocity = Vector3.Slerp(localVelocity, targetVector, Time.deltaTime * smoothing);

        velocity = Quaternion.Euler(0f, cam.eulerAngles.y, 0f) * localVelocity;

        float actualSpeed = direction * UMath.GetHorizontalMag(velocity);
        anim.SetFloat("Speed", actualSpeed);

        if (pushDown)
            velocity.y = -gravity;  // so charControl is grounded consistently
    }

    // Move on all axis not just horizontally
    public void MoveFree(float speed, float smoothing = 16f, float maxTurnAngle = 20f)
    {
        Vector3 targetVector = Quaternion.Euler(0f, cam.eulerAngles.y, 0f) * RawTargetVector();

        if (velocity.magnitude < 0.1f && targetVector.magnitude > 0f)
            velocity = transform.forward * 0.1f;  // Player will rotate smoothly from idle

        if (Vector3.Angle(velocity.normalized, targetVector) > maxTurnAngle)
        {
            Vector3 direction = Vector3.Cross(velocity.normalized, targetVector);
            targetVector = Quaternion.AngleAxis(maxTurnAngle, direction) * velocity.normalized;
        }

        targetVector *= speed;

        velocity = Vector3.Slerp(velocity, targetVector, Time.deltaTime * smoothing);

        anim.SetFloat("Speed", velocity.magnitude);
        anim.SetFloat("TargetSpeed", targetVector.magnitude);
    }

    Vector3 extraMovement = Vector3.zero;

    public void MoveInDirection(float speed, Vector3 dir, float smoothing = 4f, float maxTurnAngle = 24f)
    {
        Vector3 targetVector = dir * speed;

        if (extraMovement.magnitude < 0.1f && targetVector.magnitude > 0f)
            extraMovement = transform.forward * 0.1f;  // Player will rotate smoothly from idle

        extraMovement = Vector3.Lerp(extraMovement, targetVector, Time.deltaTime * smoothing);
        velocity += extraMovement;

        anim.SetFloat("Speed", velocity.magnitude);
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
        if (holdRotation || UMath.GetHorizontalMag(velocity) < 0.1f)
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
        if (holdRotation || UMath.GetHorizontalMag(velocity) < 0.1f)
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
                transform.rotation = Quaternion.Lerp(transform.rotation, target, smoothing * Time.deltaTime);
            else
                adjustRotCombat = false;
        }
    }

    public void RotateToVelocity(float smoothing = 0f)
    {
        if (holdRotation || velocity.magnitude < 0.1f)
            return;

        if (smoothing == 0f)
            transform.rotation = Quaternion.LookRotation(velocity);
        else
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(velocity),
                smoothing * Time.deltaTime);
    }

    public void RotateToTarget(Vector3 target)
    {
        Vector3 direction = Vector3.Scale((target - transform.position), new Vector3(1.0f, 0.0f, 1.0f));
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    public void ApplyGravity(float amount)
    {
        velocity.y -= amount * Time.deltaTime;
    }

    public void MinimizeCollider(float size = 0f)
    {
        charControl.radius = size;
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

    #region Properties

    public StateMachine<PlayerController> StateMachine
    {
        get { return stateMachine; }
    }

    public StateMachine<PlayerController> UpperStateMachine
    {
        get { return upperStateMachine; }
    }

    public CharacterController Controller
    {
        get { return charControl; }
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

    public bool Grounded
    {
        get { return isGrounded; }
    }

    public bool ConsiderStepOffset
    {
        get { return considerStepOffset; }
        set { considerStepOffset = value; }
    }

    public bool IsFootIK
    {
        get { return isFootIK; }
        set { isFootIK = value; }
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

    public float JumpYVel
    {
        get { return jumpYVel; }
    }

    public float DamageVelocity
    {
        get { return damageVelocity; }
    }

    public float DeathVelocity
    {
        get { return deathVelocity; }
    }

    public float CombatAngle
    {
        get { return combatAngle; }
    }

    public GroundInfo Ground
    {
        get { return groundInfo; }
    }

    public float TargetAngle
    {
        get { return targetAngle; }
    }

    public float TargetSpeed
    {
        get { return targetSpeed;  }
    }

    public Vector3 Velocity
    {
        get { return velocity; }
        set { velocity = value; }
    }

    public Vector3 LocalVelocity
    {
        get { return localVelocity; }
        set { localVelocity = value; }
    }
    #endregion
}
