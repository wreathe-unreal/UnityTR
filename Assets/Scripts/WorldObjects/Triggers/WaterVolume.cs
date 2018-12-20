using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterVolume : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController playControl = other.gameObject.GetComponent<PlayerController>();
            playControl.StateMachine.GoToState<Swimming>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController playControl = other.gameObject.GetComponent<PlayerController>();
            playControl.StateMachine.GoToState<Locomotion>();
        }
    }
}
