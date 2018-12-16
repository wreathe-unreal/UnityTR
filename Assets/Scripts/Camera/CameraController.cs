using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public bool mouseControl = true;
    public float rotationSpeed = 120.0f;
    public float yMax = 80.0f;
    public float yMin = -45.0f;
    public float rotationSmoothing = 30f;
    public float translationSmoothing = 30f;
    public bool LAUTurning = true;
    public bool isSplit = false;
    public string MouseX = "Mouse X";
    public string MouseY = "Mouse Y";

    [Header("Split Variables")]
    public Vector2 position;
    public Vector2 size;

    public Transform target;

    private float yRot = 0.0f;
    private float xRot = 0.0f;
    private float OldPlayerRot = 0f;
    private float playerRot = 0f;
    private float lastMouseMove = 0f;

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

            if (camState == CameraState.Grounded)
                yRot += x * rotationSpeed * Time.deltaTime;
            else
                yRot = Quaternion.LookRotation((lookAt.position - target.position).normalized, Vector3.up).eulerAngles.y;

            xRot -= y * rotationSpeed * Time.deltaTime; // Negative so mouse up = cam down
            xRot = Mathf.Clamp(xRot, yMin, yMax);
        }

        if (LAUTurning && camState == CameraState.Grounded
            && target.GetComponent<PlayerController>().Anim.GetCurrentAnimatorStateInfo(0).IsName("RunWalk"))
            DoExtraRotation();

        Quaternion targetRot = Quaternion.Euler(xRot, yRot, 0.0f);

        if (rotationSmoothing != 0f)
            pivot.rotation = Quaternion.Slerp(pivot.rotation, targetRot, rotationSmoothing * Time.deltaTime);
        else
            pivot.rotation = targetRot;

        pivot.localPosition = Vector3.Lerp(pivot.localPosition, targetPivotPosition, Time.deltaTime * 2f);
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
        if (Time.time - lastMouseMove < 0.5f)
            return;

        yRot += 1.6f * Input.GetAxis(target.GetComponent<PlayerInput>().horizontalAxis);
    }

    public void PivotOnHead()
    {
        targetPivotPosition = Vector3.zero + Vector3.up * 1.7f;
    }

    public void PivotOnTarget()
    {
        targetPivotPosition = Vector3.zero;
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
    Combat
}
