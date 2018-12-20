using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LedgeInfo 
{
    private bool hangRoom;

    private Vector3 point;
    private Vector3 direction;
    private LedgeType type;

    public LedgeInfo(LedgeType type, Vector3 point, Vector3 direction, bool hangRoom)
    {
        this.point = point;
        this.direction = direction;
        this.hangRoom = hangRoom;
        this.type = type;
    }

    public LedgeInfo()
    {
        this.point = Vector3.zero;
        this.direction = Vector3.zero;
        this.hangRoom = false;
        this.type = LedgeType.Normal;
    }

    public bool HangRoom
    {
        get { return hangRoom; }
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
