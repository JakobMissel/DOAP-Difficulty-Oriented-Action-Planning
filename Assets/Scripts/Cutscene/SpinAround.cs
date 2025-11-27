using UnityEngine;

namespace Assets.Scripts.Cutscene
{
    public class SpinAround : MonoBehaviour
    {
        [SerializeField] private float rotationalSpeed = 5f;

        // Update is called once per frame
        void Update()
        {
            transform.Rotate(new Vector3(0f, rotationalSpeed * Time.deltaTime, 0f), Space.Self);
        }
    }
}
