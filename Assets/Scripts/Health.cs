using UnityEngine;
using System;

[RequireComponent(typeof(RagdollController))]
public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    [SerializeField]
    float currentHealth;

    public event Action OnDie;          // death
    public event Action OnDamaged;      // non-lethal hit

    void Awake() => currentHealth = maxHealth;

    public void TakeDamage(float dmg)
    {
        if (currentHealth <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - dmg);

        if (currentHealth == 0)
            Die();
        else
            OnDamaged?.Invoke();
    }

    void Die()
    {
        OnDie?.Invoke();
        GetComponent<RagdollController>().EnableRagdoll();

        // disable everything else on the root
        foreach (var b in GetComponents<MonoBehaviour>())
            if (b != this && b != GetComponent<RagdollController>())
                b.enabled = false;
    }

    public bool IsAlive => currentHealth > 0;

}
