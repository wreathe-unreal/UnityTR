using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Contains some useful methods for things I did constantly
public static class UMath
{
    public static float GetHorizontalMag(Vector3 vector)
    {
        return Mathf.Sqrt(Mathf.Pow(vector.x, 2) + Mathf.Pow(vector.z, 2));
    }

    public static Vector3 ZeroYInVector(Vector3 vector)
    {
        return Vector3.Scale(vector, new Vector3(1f, 0f, 1f));
    }

    public static Vector3 MakeXYZPositive(Vector3 vector)
    {
        return new Vector3(Mathf.Abs(vector.x), Mathf.Abs(vector.y), Mathf.Abs(vector.z));
    }

    public static bool CanFitInSpace(Vector3 bottom, float height, float radius, int layerMask = ~(1 << 8))
    {
        // Check height wise
        if (Physics.Raycast(bottom, Vector3.up, height, layerMask, QueryTriggerInteraction.Ignore))
            return false;

        // Check each side using radius
        Vector3 halfWayUp = bottom + (Vector3.up * height / 2f);

        if (Physics.Raycast(halfWayUp, Vector3.forward, radius, layerMask, QueryTriggerInteraction.Ignore))
            return false;

        if (Physics.Raycast(halfWayUp, Vector3.back, radius, layerMask, QueryTriggerInteraction.Ignore))
            return false;

        if (Physics.Raycast(halfWayUp, Vector3.right, radius, layerMask, QueryTriggerInteraction.Ignore))
            return false;

        if (Physics.Raycast(halfWayUp, Vector3.left, radius, layerMask, QueryTriggerInteraction.Ignore))
            return false;

        return true;
    }

    public static Vector3 VelocityToReachPoint(Vector3 start, Vector3 end, float gravity, float time)
    {
        Vector3 relative = end - start;
        Vector3 dir = relative;
        dir.y = 0f;
        dir.Normalize();
        float xzResultant = Mathf.Sqrt(Mathf.Pow(relative.z, 2) + Mathf.Pow(relative.x, 2));
        float xz = xzResultant / time;  // u = s/t
        float y = (relative.y - (0.5f * (-gravity) * Mathf.Pow(time, 2))) / time;  // u = (s - 0.5at^2) / t
        return new Vector3(xz * dir.x, y, xz * dir.z);
    }

    public static Vector3 VelocityToReachPoint(Vector3 start, Vector3 end, float horVel, float gravity, out float time)
    {
        Vector3 relative = end - start;
        Vector3 dir = relative;
        dir.y = 0f;
        dir.Normalize();

        float horDist = GetHorizontalMag(relative);

        time = GetHorizontalMag(relative) / horVel;

        float u = (relative.y - (0.5f * (-gravity) * Mathf.Pow(time, 2))) / time;

        return (Vector3.up * u) + (horVel * dir);
    }

    public static float PredictDisplacement(float speed, float time, float accel = 0)
    {
        return (speed * time) + (0.5f * accel * Mathf.Pow(time, 2)); // s = ut + 1/2at^2
    }

    public static float TimeAtHorizontalPoint(float speed, float displace)
    {
        return displace / speed;  // t = s/u
    }

    public static float GroundAngle(Vector3 normal)
    {
        return Vector3.Angle(Vector3.up, normal);
    }

    public static float ClampAngle(float angle, float min, float max)
    {
        angle = Mathf.Repeat(angle, 360);
        min = Mathf.Repeat(min, 360);
        max = Mathf.Repeat(max, 360);
        bool inverse = false;
        float tmin = min;
        float tangle = angle;
        if (min > 180)
        {
            inverse = !inverse;
            tmin -= 180;
        }
        if (angle > 180)
        {
            inverse = !inverse;
            tangle -= 180;
        }
        var result = !inverse ? tangle > tmin : tangle < tmin;
        if (!result)
            angle = min;

        inverse = false;
        tangle = angle;
        var tmax = max;
        if (angle > 180)
        {
            inverse = !inverse;
            tangle -= 180;
        }
        if (max > 180)
        {
            inverse = !inverse;
            tmax -= 180;
        }

        result = !inverse ? tangle < tmax : tangle > tmax;
        if (!result)
            angle = max;
        return angle;
    }
}
