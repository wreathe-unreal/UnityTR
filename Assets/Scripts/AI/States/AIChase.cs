using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIChase : StateBase<EnemyController>
{
    public override void OnEnter(EnemyController enemy)
    {
        enemy.NavAgent.enabled = true;
    }

    public override void Update(EnemyController enemy)
    {
        float distance = Vector3.Distance(enemy.Target.transform.position, enemy.transform.position);

        enemy.Anim.SetFloat("Speed", enemy.NavAgent.velocity.magnitude);
        enemy.NavAgent.SetDestination(enemy.Target.transform.position);

        if (enemy.Health <= 0)
        {
            enemy.Anim.SetBool("isDead", true);

            enemy.NavAgent.enabled = false;

            return;
        }
        else if (Mathf.Abs(distance) <= enemy.NavAgent.stoppingDistance)
        {
            enemy.StateMachine.GoToState<AIEngaged>();
        }
    }

}
