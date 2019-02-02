using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class StateBase<T>
{
    public virtual void OnEnter(T entity) { }
    public virtual void OnExit(T entity) { }
    public virtual void ReceiveContext(object context) { }
    public virtual void OnSuspend(T entity) { }
    public virtual void OnUnsuspend(T entity) { }

    public abstract void Update(T entity);
}
