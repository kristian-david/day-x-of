using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Health))]
public class EnemyController : MonoBehaviour
{
    public Animator animator;

    [Header("AI")]
    public float moveSpeed   = 5f;
    public float chaseRange  = 10f;
    public float attackRange = 2f;

    [Header("Attack")]
    public float   atkCooldown    = 0.5f;   // allow faster chaining
    public float   comboResetTime = 1f;
    public int     comboSteps     = 3;
    public float   strikeDelay    = 0.3f;
    public float   atkAngleDot    = 0.5f;   // ~60° cone
    public float   damage         = 15f;
    public float   atkReach       = 1.6f;
    public float   atkRadius      = 0.8f;

    CharacterController cc;
    Health health;
    Transform player;

    Vector3 vel;
    const float GRAVITY = -9.81f, GROUNDED_Y = -2f;

    enum State { Idle, Chasing, Attacking }
    State state = State.Idle;

    // combo state
    int     comboCount;
    float   lastAttackTime = -99f;
    Coroutine pendingStrike;
    readonly string[] atkTriggers = { "Attack1", "Attack2", "Attack3", "BigAttack" };

    void Awake()
    {
        cc     = GetComponent<CharacterController>();
        health = GetComponent<Health>();
        player = GameObject.FindGameObjectWithTag("Player").transform;

        health.OnDamaged += OnHit;
        health.OnDie     += OnDeath;
    }

    void Update()
    {
        // if player dead then go idle
        var pH = player.GetComponent<Health>();
        if (pH == null || !pH.IsAlive)
        {
            animator.SetFloat("Speed", 0f);
            state = State.Idle;
            return;
        }

        ApplyGravity();

        float dist = Vector3.Distance(transform.position, player.position);
        switch (state)
        {
            case State.Idle:
                animator.SetFloat("Speed", 0f);
                if (dist < chaseRange) state = State.Chasing;
                break;

            case State.Chasing:
                animator.SetFloat("Speed", 1f);
                ChasePlayer();
                if (dist < attackRange) state = State.Attacking;
                break;

            case State.Attacking:
                animator.SetFloat("Speed", 0f);
                if (dist > attackRange) 
                    state = State.Chasing;
                else 
                    DoComboAttack();
                break;
        }
    }

    void ApplyGravity()
    {
        if (cc.isGrounded && vel.y < 0f) vel.y = GROUNDED_Y;
        vel.y += GRAVITY * Time.deltaTime;
        cc.Move(vel * Time.deltaTime);
    }

    void ChasePlayer()
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        dir.Normalize();
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dir),
            Time.deltaTime * 8f
        );
        cc.Move(dir * moveSpeed * Time.deltaTime);
    }

    void DoComboAttack()
    {
        // reset combo if too slow
        if (Time.time - lastAttackTime > comboResetTime)
            comboCount = 0;

        // respect cooldown
        if (Time.time - lastAttackTime < atkCooldown)
            return;

        // Advance combo
        comboCount++;
        lastAttackTime = Time.time;

        // trigger animation
        if (comboCount <= comboSteps)
            animator.SetTrigger($"Attack{comboCount}");
        else
        {
            animator.SetTrigger("BigAttack");
            comboCount = 0;
        }

        // cancel any existing pending strike
        if (pendingStrike != null)
        {
            StopCoroutine(pendingStrike);
            pendingStrike = null;
        }
        // schedule the actual damage
        pendingStrike = StartCoroutine(PendingStrike(strikeDelay));
    }

    IEnumerator PendingStrike(float delay)
    {
        yield return new WaitForSeconds(delay);
        pendingStrike = null;

        // build sphere center halfway out
        Vector3 centre = transform.position 
                       + Vector3.up * 1.2f 
                       + transform.forward * (atkReach * 0.5f);

        foreach (var col in Physics.OverlapSphere(
                     centre, atkRadius, ~0,
                     QueryTriggerInteraction.Collide))
        {
            // ignore self
            if (col.transform.IsChildOf(transform)) continue;

            var victimH = col.GetComponentInParent<Health>();
            if (victimH == null || victimH == health || !victimH.IsAlive) 
                continue;

            // within front cone?
            Vector3 to = victimH.transform.position - transform.position;
            to.y = 0;
            if (to.sqrMagnitude > atkReach * atkReach) continue;
            to.Normalize();
            if (Vector3.Dot(transform.forward, to) < atkAngleDot) 
                continue;

            // deliver damage
            victimH.TakeDamage(damage);
            break;  // one hit per swing
        }
    }

    public void OnHit()
    {
        // wake up
        state = State.Chasing;

        // cancel pending strike
        if (pendingStrike != null)
        {
            StopCoroutine(pendingStrike);
            pendingStrike = null;
        }
        // cancel any attack triggers
        foreach (var t in atkTriggers)
            animator.ResetTrigger(t);

        animator.Play("Hit", 0, 0);

        // Reset combo
        comboCount = 0;
    }

    void OnDeath()
    {
        // cancel strike if pending
        if (pendingStrike != null)
        {
            StopCoroutine(pendingStrike);
            pendingStrike = null;
        }
        animator.enabled = false;
        enabled        = false;
        cc.enabled     = false;
    }
}
