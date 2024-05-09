using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateMachine<T>
{
    private bool suspendUpdate = false;

    private readonly T owner;
    private StateBase<T> currentState;
    private StateBase<T> lastState;
    private Dictionary<string, StateBase<T>> allStates;

    public StateMachine(T owner)
    {
        this.owner = owner;
        allStates = new Dictionary<string, StateBase<T>>();
    }

    public void AddState(StateBase<T> state)
    {
        allStates.Add(GetStateName(state), state);
    }

    public void RemoveState(StateBase<T> state)
    {
        allStates.Remove(GetStateName(state));
    }

    public bool IsInState<TState>()
    {
        return currentState is TState;
    }

    public bool LastStateWas<TState>()
    {
        return lastState is TState;
    }

    public void GoToState<TState>()
    {
        string stateType = typeof(TState).ToString();

        lastState = currentState;

        if (currentState != null)
            currentState.OnExit(owner);

        if (allStates.TryGetValue(stateType, out currentState))
        {
            currentState.OnEnter(owner);
            return;
        }

        Debug.LogError("State is not available.");
    }

    public void GoToState<TState>(object context)
    {
        string stateType = typeof(TState).ToString();

        lastState = currentState;

        if (currentState != null)
            currentState.OnExit(owner);

        if (allStates.TryGetValue(stateType, out currentState))
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

    public void SuspendUpdate()
    {
        suspendUpdate = true;

        currentState.OnSuspend(owner);
    }

    public void UnsuspendUpdate()
    {
        suspendUpdate = false;

        currentState.OnUnsuspend(owner);
    }

    private string GetStateName(StateBase<T> state)
    {
        Type stateType = state.GetType();

        return stateType.ToString();
    }
}
