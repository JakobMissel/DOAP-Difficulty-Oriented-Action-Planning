using Assets.Scripts.GOAP.Goals;
using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace Assets.Scripts.GOAP.Behaviours
{
    public class EnergyBehaviour : MonoBehaviour
    {
        [SerializeField] private float maxEnergy = 100f;
        [SerializeField] private float drainRate = 2f;
        [SerializeField] private float rechargeRate = 8f;

        private float currentEnergy;
        private bool isRecharging;

        public float CurrentEnergy => currentEnergy;
        public float MaxEnergy => maxEnergy;

        private void Awake()
        {
            currentEnergy = maxEnergy;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (isRecharging)
            {
                currentEnergy += rechargeRate * dt;
                if (currentEnergy >= maxEnergy)
                {
                    currentEnergy = maxEnergy;
                    isRecharging = false;
                }
            }
            else
            {
                currentEnergy -= drainRate * dt;
                if (currentEnergy < 0f)
                    currentEnergy = 0f;
            }
        }

        public void SetRecharging(bool value)
        {
            isRecharging = value;
            Debug.Log($"[EnergyBehaviour] Recharging set to: {value}");
        }
    }
}
