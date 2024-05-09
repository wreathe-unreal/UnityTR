﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LadderVolume : MonoBehaviour
{
    public static LadderVolume CURRENT_LADDER = null;

    public bool offAtTop = false;

    private BoxCollider mainCollider;

    private void Start()
    {
        mainCollider = GetComponent<BoxCollider>();
    }

    public BoxCollider MainCollider
    {
        get { return mainCollider; }
    }
}
