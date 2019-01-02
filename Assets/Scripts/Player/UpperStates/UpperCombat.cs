using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpperCombat : StateBase<PlayerController>
{
    //public static Transform target;

    public override void OnEnter(PlayerController player)
    {
        if (player.Weapons.currentWeapon == null)
        {
            player.UpperStateMachine.GoToState<Empty>();
            return;
        }

        player.Weapons.target = null;

        player.Stats.ShowCanvas();
        player.Anim.SetBool("isCombat", true);
        player.Anim.SetBool("isTargetting", true);
        player.Anim.SetBool("isFiring", false);
        player.Weapons.HoldingCurrentWeapon(true);
        player.ForceWaistRotation = true;
        player.camController.LookAt = player.Weapons.target;
    }

    public override void OnExit(PlayerController player)
    {
        player.camController.LookAt = null;
        player.ForceWaistRotation = false;
        player.Anim.SetBool("isCombat", false);
        player.Anim.SetBool("isTargetting", false);
        player.Anim.SetBool("isFiring", false);
        player.Weapons.HoldingCurrentWeapon(false);
        player.Stats.HideCanvas();
    }

    public override void Update(PlayerController player)
    {
        if (player.StateMachine.IsInState<Dead>() ||
            (!Input.GetKey(player.playerInput.drawWeapon) && Input.GetAxisRaw("CombatTrigger") < 0.1f))
        {
            player.UpperStateMachine.GoToState<Empty>();
            return;
        }

        if (player.Weapons.target == null)
            CheckForTargets(player);

        Vector3 aimDirection = player.Weapons.target != null ? 
            (player.Weapons.target.position - player.transform.position).normalized
            : UMath.ZeroYInVector(player.Cam.forward).normalized;

        float aimAngle = Vector3.SignedAngle(aimDirection, player.transform.forward, Vector3.up);

        player.Anim.SetFloat("AimAngle", aimAngle);
        player.Anim.SetBool("isFiring", Input.GetKey(player.playerInput.fireWeapon));

        // Stops player's waist wobbling
        player.WaistRotation = player.transform.rotation;
    }

    private void CheckForTargets(PlayerController player)
    {
        Collider[] hitColliders = Physics.OverlapSphere(player.transform.position, 10f);

        foreach (Collider c in hitColliders)
        {
            if (c.gameObject.CompareTag("Enemy"))
            {
                player.camController.LookAt = player.Weapons.target = c.gameObject.transform;
                break;
            }
            else
            {
                player.Weapons.target = null;
            }
        }
    }
}
