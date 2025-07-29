using UnityEngine;

public class RagdollController : MonoBehaviour
{
    Rigidbody[]  bodies;
    Collider[]   cols;

    void Awake()
    {
        // find all child rigidbodies & colliders
        bodies = GetComponentsInChildren<Rigidbody>();
        cols   = GetComponentsInChildren<Collider>();
        SetKinematic(true);
    }

    void SetKinematic(bool kin)
    {
        foreach (var rb in bodies)
            rb.isKinematic = kin;
        foreach (var col in cols)
            if (col.gameObject != gameObject) // skip root collider if needed
                col.enabled = !kin;
    }

    /// Switch from animated to ragdoll.
    public void EnableRagdoll()
    {
        // disable Animator so it no longer drives the bones
        var anim = GetComponent<Animator>();
        if (anim) anim.enabled = false;

        SetKinematic(false);
    }
}
