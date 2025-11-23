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
        
        private float currentEnergy;
        private bool isRecharging;
        private GuardSight sight;
        private GuardAnimation guardAnimation;

        public float CurrentEnergy => currentEnergy;
        public float MaxEnergy => maxEnergy;
        public bool IsRecharging => isRecharging;
        public bool IsOutOfEnergy => currentEnergy <= 0f;

        private void Awake()
        {
            currentEnergy = maxEnergy;
            guardAnimation = GetComponent<GuardAnimation>();
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
                currentEnergy -= DifficultyTracker.DifficultyTranslation(EnemyActions.EnergyUsage) * drainRate * dt;
                if (currentEnergy < 0f)
                    currentEnergy = 0f;
            }
        }

        public void SetRecharging(bool value)
        {
            isRecharging = value;
            Debug.Log($"[EnergyBehaviour] {gameObject.name} recharging set to: {value}");
            
            // Trigger appropriate animation based on recharging state
            if (guardAnimation != null)
            {
                if (value)
                {
                    guardAnimation.Recharge();
                    Debug.Log($"[EnergyBehaviour] {gameObject.name} triggering Recharge animation");
                }
                else
                {
                    guardAnimation.Idle();
                    Debug.Log($"[EnergyBehaviour] {gameObject.name} triggering Idle animation");
                }
            }
        }
    }
}
