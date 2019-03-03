using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpperCombat : StateBase<PlayerController>
{
    private HUDManager hud;

    public override void OnEnter(PlayerController player)
    {
        if (player.Weapons.currentWeapon == null)
        {
            player.UpperStateMachine.GoToState<Empty>();
            return;
        }

        player.Anim.SetBool("isCombat", true);
        player.Anim.SetBool("isTargetting", true);
        player.Anim.SetBool("isFiring", false);

        player.Weapons.target = null;
        player.Weapons.HoldingCurrentWeapon(true);

        player.Stats.TryShowCanvas();
        
        player.CamControl.LookAt = player.Weapons.target;

        player.ForceWaistRotation = true;
    }

    public override void OnExit(PlayerController player)
    {
        player.Anim.SetBool("isCombat", false);
        player.Anim.SetBool("isTargetting", false);
        player.Anim.SetBool("isFiring", false);

        player.Weapons.HoldingCurrentWeapon(false);

        player.Stats.TryHideCanvas();

        player.CamControl.LookAt = null;

        player.ForceWaistRotation = false;
    }

    public override void Update(PlayerController player)
    {
        if (player.StateMachine.IsInState<Dead>() ||
            (!Input.GetKey(player.Inputf.drawWeapon) && Input.GetAxisRaw("CombatTrigger") < 0.1f))
        {
            player.UpperStateMachine.GoToState<Empty>();
            return;
        }

        CheckForTargets(player);

        Vector3 aimDirection = player.Weapons.target != null ? 
            (player.Weapons.target.position - player.transform.position).normalized
            : UMath.ZeroYInVector(player.Cam.forward).normalized;

        float aimAngle = Vector3.SignedAngle(aimDirection, player.transform.forward, Vector3.up);

        player.Anim.SetFloat("AimAngle", aimAngle);
        player.Anim.SetBool("isFiring", Input.GetKey(player.Inputf.fireWeapon));

        // Stops player's waist wobbling
        player.WaistRotation = player.transform.rotation;
    }

    private void CheckForTargets(PlayerController player)
    {
        if (player.Weapons.target != null)
        {
            EnemyController enemy = player.Weapons.target.GetComponent<EnemyController>();

            // Enemy is now dead, get rid of it
            if (enemy && enemy.Health <= 0f)
                player.Weapons.target = player.CamControl.LookAt = null;
        }
        else
        {
            Collider[] hitColliders = Physics.OverlapSphere(player.transform.position, 10f);

            foreach (Collider c in hitColliders)
            {
                if (c.gameObject.CompareTag("Enemy"))
                {
                    EnemyController enemy = c.GetComponent<EnemyController>();

                    // Enemy is now dead, ignore
                    if (enemy && enemy.Health <= 0f)
                        continue;

                    player.CamControl.LookAt = player.Weapons.target = c.gameObject.transform;
                    break;
                }
                else
                {
                    player.Weapons.target = null;
                }
            }
        }
    }
}
