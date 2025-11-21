using UnityEngine;

public class Spin : MonoBehaviour
{
    [SerializeField] Vector3 spinSpeed = new(10,30,10);

    void Update()
    {
        transform.Rotate(spinSpeed * Time.deltaTime);
    }
}
