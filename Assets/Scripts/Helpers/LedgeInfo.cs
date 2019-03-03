using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LedgeInfo 
{
    private float forwardAngle;
    private float rightAngle;

    private Vector3 point;
    private Vector3 direction;
    private Vector3 upNormal;
    private Collider collider;
    private LedgeType type;

    public LedgeInfo(LedgeType type, Vector3 point, Vector3 direction, Vector3 upNormal, Collider collider, float forwardAngle, float rightAngle)
    {
        this.point = point;
        this.direction = direction;
        this.upNormal = upNormal;
        this.collider = collider;
        this.type = type;
        this.forwardAngle = forwardAngle;
        this.rightAngle = rightAngle;
    }

    public LedgeInfo(LedgeType type, Vector3 point, Vector3 direction, Collider collider)
    {
        this.point = point;
        this.direction = direction;
        this.collider = collider;
        this.type = type;
    }

    public LedgeInfo()
    {
        this.point = Vector3.zero;
        this.direction = Vector3.zero;
        this.collider = null;
        this.type = LedgeType.Normal;
    }

    public Collider Collider
    {
        get { return collider; }
    }

    public Vector3 Point
    {
        get { return point; }
    }

    public Vector3 Direction
    {
        get { return direction; }
    }

    public Vector3 UpNormal
    {
        get { return upNormal; }
    }

    public LedgeType Type
    {
        get { return type; }
    }
}
