using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface StateContext
{
    void SetState(MoveState state);
}
public interface MoveState
{
    void Jump(StateContext context);
    void Run(StateContext context);
}

class JumpState : MoveState
{
    public void Jump(StateContext context)
    {
        Debug.Log("Jump In Jump");
    }

    public void Run(StateContext context)
    {
        Debug.Log("Run In Jump");
        context.SetState(new RunState());
    }
}
public class RunState : MoveState
{
    public void Jump(StateContext context)
    {
        Debug.Log("Jump In Run");

        context.SetState(new JumpState());
    }

    public void Run(StateContext context)
    {
        Debug.Log("Run In Run");

    }
}

public class StateMachine : MonoBehaviour, StateContext
{
    MoveState m_currentState = new RunState();

    public void SetState(MoveState state)
    {
        m_currentState = state;
    }

    void Start()
    {

    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            m_currentState.Jump(this);
        }
        
    }













}
