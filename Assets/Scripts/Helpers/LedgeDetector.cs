using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LedgeDetector
{
    private static LedgeDetector instance;

    private float minDepth = 0.05f;
    private float minHeight = 0.05f;
    private float hangRoom = 2.1f;
    private float maxForwardAngle = 45f;
    private float maxRightAngle = 45f;

    // Singleton to conserve memory and easy management
    private LedgeDetector()
    {

    }

    // MAKE NEW FUNC
    public bool FindLedgeJump(Vector3 start, Vector3 dir, float maxDistance, float maxHeight, float belowHeight = 2f)
    {
        // Start at the maximum ledge height
        // raycast below that by min ledge height, repeat
        for (float offset = maxHeight - minHeight; 
            offset >= -belowHeight - minHeight; 
            offset -= minHeight)
        {
            Vector3 currentStart = start + offset * Vector3.up;

            if (FindLedgeAtPoint(currentStart, dir, maxDistance, minHeight))
            {
                return true;
            }
        }

        return false;
    }

    public bool FindLedgeJump(Vector3 start, Vector3 dir, float maxDistance, float maxHeight, out LedgeInfo ledgeInfo)
    {
        for (float offset = maxHeight - minHeight; offset >= 0f; offset -= minHeight)
        {
            Vector3 rayStart = start + Vector3.up * offset;

            LedgeInfo info;
            if (FindLedgeAtPoint(rayStart, dir, maxDistance, minHeight, out info))
            {
                if (!info.HangRoom)
                    continue;

                Vector3 handCheckStart = info.Point - info.Direction * 0.1f + Vector3.down * (minHeight / 2f);
                Vector3 ledgeRight = Vector3.Cross(info.Direction, Vector3.up);

                // Check either side to make sure there is hand room
                if (!FindLedgeAtPoint(handCheckStart - ledgeRight * 0.2f, info.Direction, 0.2f, minHeight))
                    continue;

                if (!FindLedgeAtPoint(handCheckStart + ledgeRight * 0.2f, info.Direction, 0.2f, minHeight))
                    continue;

                ledgeInfo = info;
                return true;
            }
        }

        ledgeInfo = new LedgeInfo();
        return false;
    }

    public bool CanClimbUp(Vector3 start, Vector3 dir)
    {
        if (!Physics.Raycast(start + Vector3.up * 2.4f, dir, 0.4f)
            && !Physics.Raycast(start + Vector3.up * 3.9f, dir, 0.4f))
            return true;

        return false;
    }

    public bool FindLedgeAtPoint(Vector3 start, Vector3 dir, float maxDistance, float deltaHeight, out LedgeInfo ledgeInfo)
    {
        int notPlayerLayer = ~(1 << 8);

        // Horizontal check
        RaycastHit hHit;

        Debug.DrawRay(start, dir * maxDistance, Color.white, 5f);
        if (!Physics.Raycast(start, dir, out hHit, maxDistance, notPlayerLayer, QueryTriggerInteraction.Ignore))
            goto NoLedge;

        bool isMoving = hHit.collider.CompareTag("MovingPlatform");

        if (hHit.collider.CompareTag("Freeclimb"))
        {
            ledgeInfo = new LedgeInfo(LedgeType.Free, new Vector3(hHit.point.x, start.y, hHit.point.z), -hHit.normal, hHit.collider, true);
            return true;
        }

        // Vertical check
        RaycastHit vHit; 
        start = hHit.point + (dir * minDepth);
        start.y += deltaHeight;

        if (!Physics.Raycast(start, Vector3.down, out vHit, deltaHeight, notPlayerLayer, QueryTriggerInteraction.Ignore))
            goto NoLedge;

        Vector3 ledgePoint = new Vector3(hHit.point.x, vHit.point.y, hHit.point.z);

        // Check minimum depth
        start = ledgePoint + Vector3.up * 0.1f + hHit.normal * 0.1f;
        if (Physics.Raycast(start, -hHit.normal, minDepth + 0.1f))
            goto NoLedge;

        // Check if player has room to hang
        bool hangRoom = !Physics.Raycast(ledgePoint - (dir * 0.1f), Vector3.down, 2f, notPlayerLayer, QueryTriggerInteraction.Ignore);

        ledgeInfo = new LedgeInfo(LedgeType.Normal, ledgePoint, -hHit.normal, hHit.collider, hangRoom);
        return true;

        NoLedge:
        ledgeInfo = new LedgeInfo();
        return false;
    }

    public bool FindLedgeAtPoint(Vector3 start, Vector3 dir, float maxDistance, float deltaHeight)
    {
        LedgeInfo redundantInfo;

        return FindLedgeAtPoint(start, dir, maxDistance, deltaHeight, out redundantInfo);
    }

    public bool FindAboveHead(Vector3 start, Vector3 dir, float maxHeight, out LedgeInfo ledgeInfo)
    {
        RaycastHit hit;

        if (Physics.Raycast(start, dir, out hit, maxHeight, ~(1 << 8), QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.CompareTag("MonkeySwing"))
            {
                ledgeInfo = new LedgeInfo(LedgeType.Monkey, hit.point, Vector3.up, hit.collider, true);
                return true;
            }
            else if (hit.collider.CompareTag("HorPole"))
            {
                ledgeInfo = new LedgeInfo(LedgeType.HorPole, hit.point, Vector3.up, hit.collider, true);
                return true;
            }
        }

        ledgeInfo = new LedgeInfo();
        return false;
    }

    public bool FindPlatformInfront(Vector3 start, Vector3 dir, float maxHeight, out LedgeInfo ledgeInfo, float depth = 0.25f)
    {
        RaycastHit vHit;
        Vector3 vStart = start + (Vector3.up * 2f) + (dir * depth);
        Debug.DrawRay(vStart, Vector3.down * (maxHeight - 0.01f), Color.red, 1f);
        if (Physics.Raycast(vStart, Vector3.down, out vHit, (maxHeight - 0.01f)))
        {
            RaycastHit hHit;
            Vector3 hStart = new Vector3(start.x, vHit.point.y - 0.01f, start.z);
            Debug.DrawRay(hStart, dir * depth, Color.red, 1f);
            if (Physics.Raycast(start, dir, out hHit, depth))
            {
                Vector3 ledgeRight = Vector3.Cross(Vector3.up, hHit.normal);

                float sideAngle = Vector3.SignedAngle(Vector3.up, vHit.normal, -hHit.normal);
                float forwardAngle = Vector3.SignedAngle(Vector3.up, vHit.normal, ledgeRight);

                // Check that the ledge doesnt slope too much in either direction
                if (Mathf.Abs(sideAngle) < maxRightAngle && Mathf.Abs(forwardAngle) < maxForwardAngle)
                {
                    Vector3 ledgePoint = new Vector3(hHit.point.x, vHit.point.y, hHit.point.z);

                    hStart = ledgePoint - dir * 0.1f + Vector3.up * 1.75f;
                    if (!Physics.Raycast(hStart, dir, 1f))
                    {
                        ledgeInfo = new LedgeInfo(LedgeType.Normal, ledgePoint, -hHit.normal, hHit.collider, false);
                        return true;
                    }
                }
                
            }
        }

        ledgeInfo = new LedgeInfo();
        return false;
    }

    public float MinDepth
    {
        get { return minDepth; }
        set { minDepth = value; }
    }

    public float MinHeight
    {
        get { return minHeight; }
        set { minHeight = value; }
    }

    public float HangRoom
    {
        get { return hangRoom; }
        set { hangRoom = value; }
    }

    public float MaxForwardAngle
    {
        get { return maxForwardAngle; }
        set { maxForwardAngle = value; }
    }

    public float MaxRightAngle
    {
        get { return maxRightAngle; }
        set { maxRightAngle = value; }
    }

    public static LedgeDetector Instance
    {
        get
        {
            if (instance == null)
                instance = new LedgeDetector();
            return instance;
        }
    }
}

public enum GrabType
{
    Hand,
    Hip,
    Clear
}

public enum LedgeType
{
    Free,
    Normal,
    Monkey,
    HorPole
}