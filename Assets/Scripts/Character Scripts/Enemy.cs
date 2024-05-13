using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : Character
{
    private NavMeshAgent agent;
    private bool firing;
    public GameObject target;
    public enum States
    {
        PATROL,
        CHASE,
        ATTACK
    }
    public States state = States.PATROL;
    protected override void Start()
    {
        base.Start();
        agent = GetComponent<NavMeshAgent>();
        agent.speed = MoveSpeed;
        
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }
    private void FixedUpdate()
    {
        if (IsAlive)
        {
            switch (state)
            {
                case States.PATROL:

                    break;
                case States.CHASE:
                    Move();
                    break;
                case States.ATTACK:
                    transform.rotation = Quaternion.LookRotation(target.transform.position - (transform.position + Vector3.down), Vector3.up);
                    break;
            }
        }
        animator.SetBool("Attacking", state == States.ATTACK);
    }
    public override void Move()
    {
        agent.SetDestination(target.transform.position);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == target)
        {
            state = States.ATTACK;
            firing = true;
            agent.enabled = false;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        state = States.CHASE;
        agent.enabled = true;  
    }

    public override void Die()
    {
        agent.enabled = false;
        rb.isKinematic = false;
        GameManager.instance.EnemyDeath();
        rb.AddRelativeTorque(Random.onUnitSphere * 50);
        Destroy(gameObject, 2);
        animator.enabled = false;
    }

}
