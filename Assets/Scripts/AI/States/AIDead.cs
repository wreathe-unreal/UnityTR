using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIDead : StateBase<EnemyController>
{
    public override void OnEnter(EnemyController enemy)
    {
        enemy.Anim.SetBool("isDead", true);
    }

    public override void OnExit(EnemyController enemy)
    {
        
    }

    public override void Update(EnemyController enemy)
    {
        
    }
}
