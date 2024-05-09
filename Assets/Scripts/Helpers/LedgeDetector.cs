using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LedgeDetector
{
    private static LedgeDetector instance;

    private float minDepth = 0.05f;  // Min depth forward inside ledge
    private float minHeight = 0.05f;  // Min amount of geometry below grab point
    private float maxForwardAngle = 32f;  // Max forward slope of a ledge
    private float maxRightAngle = 32f;  // Max sideways slope of a ledge

    // Singleton to conserve memory and easy management
    private LedgeDetector()
    {

    }

    public bool FindLedgeJump(Vector3 start, Vector3 dir, float maxDistance, float maxHeight, out LedgeInfo ledgeInfo, PlayerController player)
    {
        for (float offset = maxHeight - minHeight; offset >= 0f; offset -= minHeight)
        {
            Vector3 rayStart = start + Vector3.up * offset;

            LedgeInfo info;
            if (FindHangableLedge(rayStart, dir, maxDistance, minHeight, out info, player))
            {
                ledgeInfo = info;
                return true;
            }
        }

        ledgeInfo = new LedgeInfo();
        return false;
    }

    public bool FindHangableLedge(Vector3 start, Vector3 dir, float maxDistance, float deltaHeight, out LedgeInfo ledgeInfo, PlayerController player)
    {
        if (FindLedgeAtPoint(start, dir, maxDistance, deltaHeight, out ledgeInfo))
        {
            Vector3 heightCheckStart = ledgeInfo.Point - dir * player.HangForwardOffset;

            // If we hit something that isn't a trigger thats not water then something is blocking
            //
            RaycastHit hit;
            if (Physics.Raycast(heightCheckStart, Vector3.down, out hit, player.HangUpOffset, ~(1 << 8), QueryTriggerInteraction.Collide))
            {
                if (!hit.collider.isTrigger || hit.collider.CompareTag("Water"))
                    return false;
            }

            // Now check that there is room for Lara's hands on either side (stops her hanging inside a wall)
            //
            Vector3 handCheckStart = ledgeInfo.Point - ledgeInfo.Direction * 0.1f + Vector3.down * (minHeight / 2f);
            Vector3 ledgeRight = Vector3.Cross(ledgeInfo.UpNormal, -ledgeInfo.Direction);

            if (!FindLedgeAtPoint(handCheckStart - ledgeRight * player.CharControl.radius, ledgeInfo.Direction, 0.2f, minHeight))
                return false;

            if (!FindLedgeAtPoint(handCheckStart + ledgeRight * player.CharControl.radius, ledgeInfo.Direction, 0.2f, minHeight))
                return false;

            return true;
        }

        return false;  // No possible ledge even found
    }

    public bool FindLedgeAtPoint(Vector3 start, Vector3 dir, float maxDistance, float deltaHeight, out LedgeInfo ledgeInfo)
    {
        int notPlayerLayer = ~(1 << 8);

        // Horizontal check
        //
        RaycastHit hHit;
        if (!Physics.Raycast(start, dir, out hHit, maxDistance, notPlayerLayer, QueryTriggerInteraction.Ignore))
            goto NoLedge;

        bool isMoving = hHit.collider.CompareTag("MovingPlatform");

        if (hHit.collider.CompareTag("Freeclimb"))
        {
            ledgeInfo = new LedgeInfo(LedgeType.Free, new Vector3(hHit.point.x, start.y, hHit.point.z), -hHit.normal, hHit.collider);
            return true;
        }

        // Vertical check
        //
        RaycastHit vHit; 
        start = hHit.point + (dir * minDepth);
        start.y += deltaHeight;

        if (!Physics.Raycast(start, Vector3.down, out vHit, deltaHeight, notPlayerLayer, QueryTriggerInteraction.Ignore))
            goto NoLedge;

        Vector3 rightNormal = vHit.normal - Vector3.Scale(vHit.normal, UMath.MakeXYZPositive(-hHit.normal));

        // Check angle is shallow enough sideways (think shimmying up when going sideways)
        //
        float angleRight = Vector3.Angle(Vector3.up, rightNormal);

        if (angleRight > maxRightAngle)
            goto NoLedge;

        // Check angle is shallow enough straight in front (think grabbing bottom of slope)
        //
        Vector3 ledgeRight = Vector3.Cross(Vector3.up, hHit.normal).normalized;

        Vector3 forwardNormal = vHit.normal - Vector3.Scale(vHit.normal, UMath.MakeXYZPositive(ledgeRight));

        float angleForward = Vector3.Angle(Vector3.up, forwardNormal);

        if (angleForward > maxForwardAngle)
            goto NoLedge;

        // Resolve so Lara grabs correct point on a slope
        float offset = minDepth * Mathf.Tan(angleForward * Mathf.Deg2Rad);

        Vector3 ledgePoint = new Vector3(hHit.point.x, vHit.point.y - offset, hHit.point.z);

        // Check nothing blocking vertically
        //
        start = ledgePoint + hHit.normal * 0.1f - Vector3.up * 0.1f;
        if (Physics.Raycast(start, Vector3.up, 0.2f, notPlayerLayer, QueryTriggerInteraction.Ignore))
            goto NoLedge;

        start = ledgePoint + hHit.normal * 0.1f + Vector3.up * 0.1f;
        if (Physics.Raycast(start, Vector3.down, 0.2f, notPlayerLayer, QueryTriggerInteraction.Ignore))
            goto NoLedge;

        // Check minimum depth
        start = ledgePoint + Vector3.up * 0.1f + hHit.normal * 0.1f;
        if (Physics.Raycast(start, -hHit.normal, minDepth + 0.1f, notPlayerLayer, QueryTriggerInteraction.Ignore))
            goto NoLedge;

        ledgeInfo = new LedgeInfo(LedgeType.Normal, ledgePoint, -hHit.normal, vHit.normal, hHit.collider, angleForward, angleRight);
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
                ledgeInfo = new LedgeInfo(LedgeType.Monkey, hit.point, Vector3.up, hit.collider);
                return true;
            }
            else if (hit.collider.CompareTag("HorPole"))
            {
                ledgeInfo = new LedgeInfo(LedgeType.HorPole, hit.point, Vector3.up, hit.collider);
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
        
        if (Physics.Raycast(vStart, Vector3.down, out vHit, (maxHeight - 0.01f), ~(1 << 8)))
        {
            RaycastHit hHit;
            Vector3 hStart = new Vector3(start.x, vHit.point.y - 0.01f, start.z);
            
            if (Physics.Raycast(start, dir, out hHit, depth, ~(1 << 8)))
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
                        ledgeInfo = new LedgeInfo(LedgeType.Normal, ledgePoint, -hHit.normal, hHit.collider);
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