using UnityEngine;

public class BearLauncher : MonoBehaviour
{
    [SerializeField] private Rigidbody bear;
    [SerializeField] private Vector3 velocity = Vector3.right;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        bear.linearVelocity = velocity;
        bear.useGravity = true;
    }
}
