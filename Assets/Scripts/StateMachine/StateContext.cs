using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateContext<T>
{
    private T contextInfo;

    public StateContext(T contextInfo)
    {
        this.contextInfo = contextInfo;
    }

    public T ContextInfo
    {
        get { return contextInfo; }
    }
}
