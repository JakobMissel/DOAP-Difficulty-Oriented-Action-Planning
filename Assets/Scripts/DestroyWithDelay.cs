using UnityEngine;

public class DestroyWithDelay : MonoBehaviour
{
    [SerializeField] float lifeTime = 5f;
    float lifeTimer = 0f;

    void Update()
    {
        Destroy();
    }

    void Destroy()
    {
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= lifeTime)
        {
            if(!gameObject) return;
            Destroy(gameObject);
        }
    }
}
