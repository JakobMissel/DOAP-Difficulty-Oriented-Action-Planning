using Assets.Scripts.GOAP.Goals;
using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using Assets.Scripts.DDA;

namespace Assets.Scripts.GOAP.Behaviours
{
    public class EnergyBehaviour : MonoBehaviour
    {
        [SerializeField] private float maxEnergy = 100f;
        [SerializeField] private float drainRate = 2f;
        [SerializeField] private float rechargeRate = 8f;

        [SerializeField] private float currentEnergy;
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
                // Multiplies the drainrate by the Difficulty Translation of it (full energy usage at difficulty 0, no energy usage at difficulty 1)
                currentEnergy -= DifficultyTracker.DifficultyTranslationEnemy(EnemyActions.EnergyUsage) * drainRate * dt;
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
