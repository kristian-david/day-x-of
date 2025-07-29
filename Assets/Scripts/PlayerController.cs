using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Health))]
public class PlayerController : MonoBehaviour
{
    public Animator animator;

    [Header("Movement")]
    public float moveSpeed            = 6f;
    public float rotationSmooth       = 0.1f;
    public float gravity              = -9.81f;
    public float jumpHeight           = 1.5f;

    [Header("Soft-Lock when attacking")]
    public float softLockRange        = 6f;
    [Range(0, 1)] public float softBlend = .7f;

    [Header("Attack")]
    public KeyCode attackKey          = KeyCode.Mouse0;
    public float   atkCooldown        = 0.8f;
    public float   comboResetTime     = 1f;
    public int     comboSteps         = 3;
    public float   atkRadius          = .9f;
    public float   atkReach           = 1.6f;
    public float   damage             = 25f;
    public float   strikeDelay        = 0.3f;

    // landing helpers
    bool  wasGrounded;
    float lastMoveInputMag;

    CharacterController cc;
    Health            health;

    Vector3 vel;
    const float groundedY = -2f;
    float yaw, yawVel;
    int   combo;
    float lastSwing;
    float stunTimer;
    Transform target;

    Coroutine pendingStrike;
    readonly string[] atkTriggers = { "Attack1", "Attack2", "Attack3", "BigAttack" };

    void Awake()
    {
        cc     = GetComponent<CharacterController>();
        health = GetComponent<Health>();
        health.OnDie     += OnDeath;
        health.OnDamaged += OnHit;
        yaw = transform.eulerAngles.y;
    }

    void Update()
    {
        if (!health.IsAlive) return;

        stunTimer = Mathf.Max(0, stunTimer - Time.deltaTime);
        FindTarget();
        Move();

        // landing detection: if we just became grounded and are already moving then force Move state
        bool landedThisFrame = cc.isGrounded && !wasGrounded;
        if (landedThisFrame && lastMoveInputMag >= 0.1f)
        {
            animator.ResetTrigger("Jump");
            animator.SetFloat("Speed", lastMoveInputMag);
            animator.CrossFadeInFixedTime("Move", 0f); // immediate switch to walking
        }
        wasGrounded = cc.isGrounded;


        Jump();
        Attack();
    }

    void FindTarget()
    {
        target = null;
        float best = softLockRange * softLockRange;
        foreach (var go in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            var h = go.GetComponent<Health>();
            if (h == null || !h.IsAlive) continue;
            float d = (go.transform.position - transform.position).sqrMagnitude;
            if (d < best)
            {
                best = d;
                target = go.transform;
            }
        }
    }

    void Move()
    {
        if (cc.isGrounded && vel.y < 0f) vel.y = groundedY;
        vel.y += gravity * Time.deltaTime;

        Vector3 inVec = new Vector3(
            Input.GetAxisRaw("Horizontal"), 0,
            Input.GetAxisRaw("Vertical")
        ).normalized;
        animator.SetFloat("Speed", inVec.magnitude);

        if (inVec.sqrMagnitude > .01f)
        {
            float camYaw  = Camera.main.transform.eulerAngles.y;
            float wishYaw = Mathf.Atan2(inVec.x, inVec.z) * Mathf.Rad2Deg + camYaw;

            if (target)
            {
                Vector3 to = target.position - transform.position; to.y = 0f;
                float eYaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
                if (Mathf.Abs(inVec.z) > Mathf.Abs(inVec.x))
                    wishYaw = inVec.z > 0 ? eYaw : eYaw + 180f;
                else
                    wishYaw = Mathf.LerpAngle(wishYaw, eYaw, softBlend);
            }

            yaw = Mathf.SmoothDampAngle(yaw, wishYaw, ref yawVel, rotationSmooth);
            transform.rotation = Quaternion.Euler(0, yaw, 0);
            Vector3 dir = Quaternion.Euler(0, wishYaw, 0) * Vector3.forward;
            cc.Move(dir * moveSpeed * Time.deltaTime);
        }
        else if (target)
        {
            Vector3 to = target.position - transform.position; to.y = 0f;
            float eYaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            yaw = Mathf.SmoothDampAngle(yaw, eYaw, ref yawVel, rotationSmooth);
            transform.rotation = Quaternion.Euler(0, yaw, 0);
        }

        cc.Move(vel * Time.deltaTime);
    }

    void Jump()
    {
        if (Input.GetButtonDown("Jump") && cc.isGrounded)
        {
            vel.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animator.SetTrigger("Jump"); // play jump animation
        }
    }

    void Attack()
    {
        if (stunTimer > 0f) return;
        if (!Input.GetKeyDown(attackKey)) return;
        if (Time.time - lastSwing < atkCooldown) return;

        if (Time.time - lastSwing > comboResetTime) combo = 0;
        combo++;
        lastSwing = Time.time;

        if (combo <= comboSteps) animator.SetTrigger($"Attack{combo}");
        else { animator.SetTrigger("BigAttack"); combo = 0; }

        // cancel any previous pending strike just in case
        if (pendingStrike != null)
        {
            StopCoroutine(pendingStrike);
            pendingStrike = null;
        }
        pendingStrike = StartCoroutine(DelayStrike(strikeDelay));
    }

    IEnumerator DelayStrike(float delay)
    {
        yield return new WaitForSeconds(delay);

        pendingStrike = null; // clear handle

        Vector3 centre = transform.position
                       + Vector3.up * 1.2f
                       + transform.forward * (atkReach * 0.5f);

        foreach (var col in Physics.OverlapSphere(
                     centre, atkRadius, ~0,
                     QueryTriggerInteraction.Collide))
        {
            if (col.transform.IsChildOf(transform)) continue;
            var root = col.GetComponentInParent<Transform>();
            var h    = root.GetComponent<Health>();
            if (h == null || h == health || !h.IsAlive) continue;

            Vector3 to = root.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > atkReach * atkReach) continue;
            to.Normalize();
            if (Vector3.Dot(transform.forward, to) < 0.5f) continue;

            h.TakeDamage(damage);
            root.GetComponent<EnemyController>()?.OnHit();
            break;
        }
    }

    void OnHit()
    {
        stunTimer = .5f;
        // cancel strike coroutine
        if (pendingStrike != null)
        {
            StopCoroutine(pendingStrike);
            pendingStrike = null;
        }
        // cancel any attack trigger
        foreach (var t in atkTriggers) animator.ResetTrigger(t);
        animator.Play("Hit", 0, 0);
        combo = 0;
    }

    void OnDeath()
    {
        // ensure no pending strike
        if (pendingStrike != null)
        {
            StopCoroutine(pendingStrike);
            pendingStrike = null;
        }
        animator.enabled = false;
        enabled          = false;
        cc.enabled       = false;
    }
}
