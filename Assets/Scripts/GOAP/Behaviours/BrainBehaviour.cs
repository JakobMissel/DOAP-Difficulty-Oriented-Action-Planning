using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace Assets.Scripts.GOAP.Behaviours
{
    public class BrainBehaviour : MonoBehaviour
    {
        private GoapActionProvider provider;

        private void Awake()
        {
            provider = GetComponent<GoapActionProvider>();
        }

        private void Start()
        {
            provider.RequestGoal<Goals.PatrolGoal>();
        }
    }
}