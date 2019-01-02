using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateMachine<T>
{
    private bool suspendUpdate = false;

    private T owner;
    private StateBase<T> currentState;
    private List<StateBase<T>> possibleStates;

    public StateMachine(T owner)
    {
        this.owner = owner;
        possibleStates = new List<StateBase<T>>();
    }

    public void AddState(StateBase<T> state)
    {
        possibleStates.Add(state);
    }

    public void RemoveState(StateBase<T> state)
    {
        possibleStates.Remove(state);
    }

    public bool IsInState<TState>()
    {
        return currentState is TState;
    }

    public void GoToState<TState>()
    {
        foreach (StateBase<T> state in possibleStates)
        {
            if (state is TState)
            {
                if (currentState != null)
                    currentState.OnExit(owner);
                currentState = state;
                currentState.OnEnter(owner);
                return;
            }
        }
        Debug.LogError("State is not available.");
    }

    public void GoToState<TState>(object context)
    {
        foreach (StateBase<T> state in possibleStates)
        {
            if (state is TState)
            {
                if (currentState != null)
                    currentState.OnExit(owner);
                currentState = state;
                currentState.ReceiveContext(context);
                currentState.OnEnter(owner);
                return;
            }
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
}
