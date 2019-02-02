using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateMachine<T>
{
    private bool suspendUpdate = false;

    private readonly T owner;
    private StateBase<T> currentState;
    private Dictionary<string, StateBase<T>> possibleStates;

    public StateMachine(T owner)
    {
        this.owner = owner;
        possibleStates = new Dictionary<string, StateBase<T>>();
    }

    public void AddState(StateBase<T> state)
    {
        possibleStates.Add(GetStateName(state), state);
    }

    public void RemoveState(StateBase<T> state)
    {
        possibleStates.Remove(GetStateName(state));
    }

    public bool IsInState<TState>()
    {
        return currentState is TState;
    }

    public void GoToState<TState>()
    {
        string stateType = typeof(TState).ToString();

        if (currentState != null)
            currentState.OnExit(owner);

        if (possibleStates.TryGetValue(stateType, out currentState))
        {
            currentState.OnEnter(owner);
            return;
        }

        Debug.LogError("State is not available.");
    }

    public void GoToState<TState>(object context)
    {
        string stateType = typeof(TState).ToString();

        if (currentState != null)
            currentState.OnExit(owner);

        if (possibleStates.TryGetValue(stateType, out currentState))
        {
            currentState.ReceiveContext(context);
            currentState.OnEnter(owner);
            return;
        }

        Debug.LogError("State is not available.");
    }

    public void Update()
    {
        if (suspendUpdate)
            return;

        currentState.Update(owner);
    }

    public void SuspendUpdate(bool suspend = true)
    {
        suspendUpdate = suspend;

        if (suspend)
            currentState.OnSuspend(owner);
        else
            currentState.OnUnsuspend(owner);
    } 

    private string GetStateName(StateBase<T> state)
    {
        Type stateType = state.GetType();

        return stateType.ToString();
    }
}
