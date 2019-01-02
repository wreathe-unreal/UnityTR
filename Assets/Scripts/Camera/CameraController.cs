using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public bool mouseControl = true;
    public float rotationSpeed = 120.0f;
    public float pitchMax = 80.0f;
    public float pitchMin = -45.0f;
    public float rotationSmoothing = 30f;
    public float translationSmoothing = 30f;
    public float turnRate = 1.6f;
    public float verticalTurnInfluence = 0.6f;
    public bool LAUTurning = true;
    public bool isSplit = false;
    public string MouseX = "Mouse X";
    public string MouseY = "Mouse Y";

    [Header("Split Variables")]
    public Vector2 position;
    public Vector2 size;

    public Transform target;

    private float yaw = 0.0f;
    private float pitch = 0.0f;
    private float lastMouseMove = 0f;
    private float currentTurnRate = 0f;

    private Transform pivot;
    private Transform lookAt;
    private Vector3 pivotOrigin;
    private Vector3 targetPivotPosition;
    private Vector3 forceDirection;
    private Camera cam;
    private CameraState camState;

    private void Start()
    {
        forceDirection = Vector3.zero;
        camState = CameraState.Grounded;
        cam = GetComponentInChildren<Camera>();

        if (isSplit)
            cam.rect = new Rect(position, size);

        pivot = cam.transform.parent;
        pivotOrigin = pivot.localPosition;
        targetPivotPosition = pivot.localPosition;
    }

    private void LateUpdate()
    {
        if (RingMenu.isPaused)
            return;

        if (camState == CameraState.Freeze)
            return;

        HandleMovement();
        HandleRotation();
    }

    private void HandleRotation()
    {
        if (mouseControl)
        {
            float x = Input.GetAxis(MouseX);
            float y = Input.GetAxis(MouseY);

            if (x != 0f || y != 0f)
                lastMouseMove = Time.time;

            if (camState == CameraState.Grounded || camState == CameraState.Climb)
                yaw += x * rotationSpeed * Time.deltaTime;
            else if (camState == CameraState.Combat)
                yaw = Quaternion.LookRotation((lookAt.position - target.position).normalized, Vector3.up).eulerAngles.y;

            if (camState == CameraState.Climb)
                LimitYaw(80f, ref yaw);

            pitch -= y * rotationSpeed * Time.deltaTime; // Negative so mouse up = cam down
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
        }

        if (LAUTurning && camState == CameraState.Grounded)
            DoExtraRotation();

        Quaternion targetRot = Quaternion.Euler(pitch, yaw, 0.0f);

        if (rotationSmoothing != 0f)
            pivot.rotation = Quaternion.Slerp(pivot.rotation, targetRot, rotationSmoothing * Time.deltaTime);
        else
            pivot.rotation = targetRot;

        pivot.localPosition = Vector3.Lerp(pivot.localPosition, targetPivotPosition, Time.deltaTime * 2f);
    }

    private void LimitYaw(float range, ref float yaw)
    {
        float yawMax = target.eulerAngles.y + range;
        float yawMin = target.eulerAngles.y - range;

        yaw = UMath.ClampAngle(yaw, yawMin, yawMax);

        StartCoroutine(UnsmoothRotationSet());
    }

    private void HandleMovement()
    {
        if (translationSmoothing != 0f)
            transform.position = Vector3.Lerp(transform.position, target.position, Time.deltaTime * translationSmoothing);
        else
            transform.position = target.position;
    }

    private void DoExtraRotation()
    {
        string horizontal = target.GetComponent<PlayerInput>().horizontalAxis;
        string vertical = target.GetComponent<PlayerInput>().verticalAxis;

        float axisValue = Input.GetAxis(horizontal);
        float vertAxis = Input.GetAxis(vertical);

        if (Time.time - lastMouseMove < 0.75f)
            currentTurnRate = 0f;
        else
            currentTurnRate = turnRate - (vertAxis * verticalTurnInfluence);

        yaw += currentTurnRate * axisValue;
    }

    public IEnumerator UnsmoothRotationSet()
    {
        float oldSmoothValue = rotationSmoothing;
        rotationSmoothing = 0f;

        yield return null;

        rotationSmoothing = oldSmoothValue;
    }

    public void PivotOnHead()
    {
        targetPivotPosition = Vector3.up * 1.7f;
    }

    public void PivotOnTarget()
    {
        targetPivotPosition = Vector3.zero;
    }

    public void PivotOnHip()
    {
        targetPivotPosition = Vector3.up;
    }

    public void PivotOnPivot()
    {
        targetPivotPosition = pivotOrigin;
    }

    public CameraState State
    {
        get { return camState; }
        set { camState = value; }
    }

    public Vector3 ForceDirection
    {
        get { return forceDirection; }
        set { forceDirection = value; }
    }

    public Transform LookAt
    {
        get { return lookAt; }
        set { lookAt = value; }
    }
}

// enum incase of future extensions
public enum CameraState
{
    Freeze,
    Grounded,
    Combat,
    Climb
}
