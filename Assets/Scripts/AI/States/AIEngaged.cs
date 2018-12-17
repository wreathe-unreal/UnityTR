using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIEngaged : StateBase<EnemyController>
{
    public override void OnEnter(EnemyController enemy)
    {
        enemy.Anim.SetBool("isEngaged", true);
        enemy.Anim.SetBool("isShooting", true);
    }

    public override void OnExit(EnemyController enemy)
    {
        enemy.Anim.SetBool("isEngaged", false);
        enemy.Anim.SetBool("isShooting", false);
    }

    public override void Update(EnemyController enemy)
    {
        enemy.Anim.SetFloat("Speed", enemy.NavAgent.velocity.magnitude);

        float distance = Vector3.Distance(enemy.Target.transform.position, enemy.transform.position);

        if (Mathf.Abs(distance) > enemy.maxAimDistance)
        {
            enemy.StateMachine.GoToState<AIChase>();
            return;
        }

        Vector3 direction = enemy.Target.transform.position - enemy.transform.position;
        enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation,
            Quaternion.LookRotation(direction.normalized, Vector3.up),
            Time.deltaTime * 20f);
        enemy.Anim.SetLookAtPosition(enemy.Target.transform.position + Vector3.up * 1.75f);
        enemy.Anim.SetLookAtWeight(1f);

        
    }
}
