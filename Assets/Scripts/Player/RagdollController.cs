using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RagdollController : MonoBehaviour
{
    [SerializeField]
    private Rigidbody[] rigidBodies;

    private PlayerController player;

    private void Start()
    {
        player = GetComponent<PlayerController>();

        DisableRagdoll();
    }

    private void OnEnable()
    {
        PlayerStats stats = GetComponent<PlayerStats>();

        if (stats)
            stats.OnDeath += EnableRagdoll;
        else
            Debug.LogError("No PlayerStats found by ragdoll manager for death event");
    }

    private void OnDisable()
    {
        PlayerStats stats = GetComponent<PlayerStats>();

        if (stats)
            stats.OnDeath -= EnableRagdoll;
        else
            Debug.LogError("No PlayerStats found by ragdoll manager for death event");
    }

    public void DisableRagdoll()
    {
        GetComponent<Animator>().enabled = true;

        foreach (Rigidbody rb in rigidBodies)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.gameObject.GetComponent<Collider>().enabled = false;
        }
    }

    public void EnableRagdoll()
    {
        GetComponent<Animator>().enabled = false;
        player.CamControl.Target = rigidBodies[0].transform;

        foreach (Rigidbody rb in rigidBodies)
        {
            player.DisableCharControl();  // So physics doesn't bamboozle

            rb.isKinematic = false;
            rb.useGravity = true;
            rb.gameObject.GetComponent<Collider>().enabled = true;

            if (!player.WasGrounded)
                rb.velocity = player.VelocityLastFrame;
            else
                rb.velocity = player.transform.forward * 0.1f;
        }
    }
}
