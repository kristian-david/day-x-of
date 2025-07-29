using UnityEngine;

public class BumpBlock : MonoBehaviour
{

    public Transform coin; 
    public float coinRiseHeight = 1f;
    public float coinRiseTime   = 0.5f;


    public float bumpHeight   = 0.3f;
    public float bumpDuration = 0.2f;
    public bool  oneTime      = true;

    float originalY;
    bool  animating;
    bool  used;

    Collider solidCol;   // collision
    Collider triggerCol; // trigger to check player head

    float coinOriginalY;

    void Awake()
    {
        originalY = transform.position.y;

        // Find existing colliders
        var cols = GetComponents<Collider>();
        foreach (var c in cols)
        {
            if (c.isTrigger) triggerCol = c;
            else             solidCol   = c;
        }

        if (solidCol == null)
            solidCol = gameObject.AddComponent<BoxCollider>();

        // Auto-create a trigger under the block if not present
        if (triggerCol == null)
        {
            BoxCollider solidBox = solidCol as BoxCollider;
            BoxCollider trig = gameObject.AddComponent<BoxCollider>();
            trig.isTrigger = true;

            if (solidBox != null)
            {
                trig.size   = solidBox.size;
                trig.center = solidBox.center + Vector3.down * (solidBox.size.y * 0.5f + 0.05f);
            }
            triggerCol = trig;
        }

        // Setup coin
        if (coin != null)
        {
            coinOriginalY = coin.position.y;
            coin.gameObject.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // ensure hit came from below the solid block
        if (other.bounds.max.y <= solidCol.bounds.min.y + 0.05f)
            Bump();
    }

    void Bump()
    {
        if (animating) return;
        animating = true;

        // Bounce animation
        LeanTween.moveY(gameObject, originalY + bumpHeight, bumpDuration * 0.5f)
            .setEaseOutQuad()
            .setOnComplete(() =>
            {
                LeanTween.moveY(gameObject, originalY, bumpDuration * 0.5f)
                    .setEaseInQuad()
                    .setOnComplete(() => animating = false);
            });

        // Coin reuse logic
        if (coin != null)
        {
            if (oneTime && used) return; // already used
            used = true;

            // reset & show
            coin.gameObject.SetActive(true);
            coin.position = new Vector3(coin.position.x, coinOriginalY, coin.position.z);
            LeanTween.cancel(coin.gameObject);

            // move up
            LeanTween.moveY(coin.gameObject, coinOriginalY + coinRiseHeight, coinRiseTime)
                .setEaseOutQuad()
                .setOnComplete(() =>
                {
                    if (oneTime)
                    {
                        coin.gameObject.SetActive(false);
                    }
                    else
                    {
                        // move back down then hide & allow reuse
                        LeanTween.moveY(coin.gameObject, coinOriginalY, 0.3f)
                            .setDelay(0.1f)
                            .setEaseInQuad()
                            .setOnComplete(() =>
                            {
                                coin.gameObject.SetActive(false);
                                used = false; // allow next bump
                            });
                    }
                });
        }
    }
}
