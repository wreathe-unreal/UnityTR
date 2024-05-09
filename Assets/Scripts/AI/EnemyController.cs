using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    public int startHealth = 100;
    public float angleOfView = 84f;
    public float maxAimDistance = 12f;

    public Weapon weapon;

    private int health;
    private bool isAlive = true;
    private float dotValue = 0f;

    private StateMachine<EnemyController> stateMachine;
    private NavMeshAgent nav;
    private GameObject target;
    private Animator anim;

    private void Start()
    {
        health = startHealth;
        dotValue = Mathf.Cos(angleOfView * Mathf.Deg2Rad);

        anim = GetComponent<Animator>();
        nav = GetComponent<NavMeshAgent>();
        target = GameObject.FindGameObjectWithTag("Player");

        stateMachine = new StateMachine<EnemyController>(this);
        SetUpStates();
    }

    private void SetUpStates()
    {
        stateMachine.AddState(new AIIdle());
        stateMachine.AddState(new AIChase());
        stateMachine.AddState(new AIEngaged());
        stateMachine.AddState(new AIDead());
        stateMachine.GoToState<AIIdle>();
    }

    private void Update()
    {
        stateMachine.Update();

        if (health <= 0)
            stateMachine.GoToState<AIDead>();
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (!stateMachine.IsInState<AIIdle>())
            return;

        Vector3 playerDirection = (other.transform.position - transform.position).normalized;
        float dotProduct = Vector3.Dot(transform.forward, playerDirection);

        // Checks if player is within field of view
        if (dotProduct > dotValue)
        {
            stateMachine.GoToState<AIEngaged>();
        }
    }

    public void FireWeapon()
    {
        weapon.Fire(target.transform.position + Vector3.up * 1f);
    }

    public int Health
    {
        get { return health; }
        set {
            health = value;

            if (health <= 0)
            {
                health = 0;
            }
        }
    }

    public StateMachine<EnemyController> StateMachine
    {
        get { return stateMachine; }
    }

    public NavMeshAgent NavAgent
    {
        get { return nav; }
    }

    public Animator Anim
    {
        get { return anim; }
    }

    public GameObject Target
    {
        get { return target; }
        set { target = value; }
    }
}
