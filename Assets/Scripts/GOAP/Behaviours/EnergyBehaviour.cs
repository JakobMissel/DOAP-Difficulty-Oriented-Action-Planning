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
        
        [Header("Spotlight Control")]
        [SerializeField] private GameObject spotlight;
        [Tooltip("Set the spotlight game object: The child of the gurds Eyes")]
        
        private float currentEnergy;
        private bool isRecharging;
        private SimpleGuardSightNiko sight;

        public float CurrentEnergy => currentEnergy;
        public float MaxEnergy => maxEnergy;
        public bool IsRecharging => isRecharging;
        public bool IsOutOfEnergy => currentEnergy <= 0f;

        private void Awake()
        {
            currentEnergy = maxEnergy;
            sight = GetComponent<SimpleGuardSightNiko>();
            
            // Try to find spotlight if not assigned
            if (spotlight == null && sight != null && sight.Eyes != null)
            {
                // Look for spotlight as child of Eyes
                Transform spotlightTransform = sight.Eyes.Find("Spotlight");
                if (spotlightTransform != null)
                {
                    spotlight = spotlightTransform.gameObject;
                    Debug.Log($"[EnergyBehaviour] Found spotlight automatically: {spotlight.name}");
                }
                else
                {
                    Debug.LogWarning($"[EnergyBehaviour] Could not find spotlight as child of Eyes!");
                }
            }
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
                    
                    // Re-enable spotlight when fully charged
                    if (spotlight != null)
                    {
                        spotlight.SetActive(true);
                        Debug.Log($"[EnergyBehaviour] {gameObject.name} fully recharged, spotlight enabled");
                    }
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
            
            // Disable spotlight when recharging or out of energy
            if (spotlight != null)
            {
                if (value || currentEnergy <= 0f)
                {
                    spotlight.SetActive(false);
                    Debug.Log($"[EnergyBehaviour] {gameObject.name} spotlight disabled (recharging or no energy)");
                }
                else if (currentEnergy >= maxEnergy)
                {
                    spotlight.SetActive(true);
                    Debug.Log($"[EnergyBehaviour] {gameObject.name} spotlight enabled (full energy)");
                }
            }
        }
    }
}
