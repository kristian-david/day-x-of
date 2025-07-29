using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CoinCollectible : MonoBehaviour
{
    public float rotationSpeed = 90f;

    public ParticleSystem collectEffectPrefab;

    public float destroyDelay = 0.5f;

    Collider col;

    void Awake()
    {
        col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void Update()
    {
        // rotate animation on Y axis
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        // only react to the player
        if (!other.CompareTag("Player")) return;

        Collect();
    }

    void Collect()
    {
        // Spawn effect
        if (collectEffectPrefab != null)
        {
            var ps = Instantiate(
                collectEffectPrefab,
                transform.position,
                Quaternion.identity
            );
            ps.Play();
            Destroy(ps.gameObject,
                    ps.main.duration + ps.main.startLifetime.constantMax);
        }

        // disable visuals & collider immediately
        Hide();

        Destroy(gameObject, destroyDelay);
    }

    void Hide()
    {
        // disable any renderer(s)
        foreach (var rend in GetComponentsInChildren<Renderer>())
            rend.enabled = false;

        // disable collider so no further triggers
        col.enabled = false;
    }
}
