using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/***
 * FEATURE NOT READY - EXCLUDED FROM BETA
 * ***/

public class MovingPlatform : MonoBehaviour
{
    private Vector3 lastPos;
    private Transform player;

    private List<Transform> attachedObjects = new List<Transform>();

    private void Start()
    {
        lastPos = transform.position;
        player = GameObject.Find("PlayerController").transform;
    }

    public void AttachTransform(Transform transform)
    {
        if (attachedObjects.Contains(transform))
            return;
        
        attachedObjects.Add(transform);
    }

    public void DetachTransform(Transform transform)
    {
        attachedObjects.Remove(transform);
    }

    private void LateUpdate()
    {
        Vector3 positionChange = transform.position - lastPos;

        foreach (Transform t in attachedObjects)
        {
            t.position += positionChange;
        }

        lastPos = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            AttachTransform(other.transform);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();

        if (other.CompareTag("Player"))
        {
            DetachTransform(other.transform);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();

            if (player.Velocity.y > 0f && !player.Grounded)
                OnTriggerExit(other);
        }
    }
}
