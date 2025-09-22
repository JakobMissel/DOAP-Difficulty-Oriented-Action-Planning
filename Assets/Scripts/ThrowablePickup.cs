using UnityEngine;

public class ThrowablePickup : Pickup
{
    [Header("Throwable")]
    [SerializeField] GameObject thrownObjectPrefab;
    [SerializeField] int ammoCount = 5;

    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            other.GetComponent<PlayerThrow>().AddThrowable(thrownObjectPrefab, ammoCount);
            Destroy(gameObject);
        }
    }
}
