using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StepOffsetFix : MonoBehaviour
{
    public float offset = 0.64f;

    private CharacterController charControl;

	void Start()
    {
        charControl = GetComponent<CharacterController>();
	}

    private void Update()
    {
        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * offset + transform.forward * (charControl.radius + 0.01f);

        if (Physics.Raycast(origin, Vector3.down, out hit, offset, ~(1 << 8), QueryTriggerInteraction.Ignore))
        {
            charControl.stepOffset = offset;
        }
        else
        {
            charControl.stepOffset = 0.1f;
        }
    }
}
