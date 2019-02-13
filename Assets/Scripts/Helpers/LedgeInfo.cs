using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LedgeInfo 
{
    private Vector3 point;
    private Vector3 direction;
    private Collider collider;
    private LedgeType type;

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

    public LedgeType Type
    {
        get { return type; }
    }
}
