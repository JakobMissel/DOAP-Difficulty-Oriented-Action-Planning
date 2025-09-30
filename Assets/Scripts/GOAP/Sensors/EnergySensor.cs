using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core; // IActionReceiver, IComponentReference
using CrashKonijn.Goap.Runtime; // LocalWorldSensorBase
using UnityEngine;

namespace Assets.Scripts.GOAP.Sensors
{
    [GoapId("EnergySensor-a4da1183-e27a-4159-83bd-fb49f2ea3f2a")]
    public class EnergySensor : LocalWorldSensorBase
    {
        [SerializeField] private float maxEnergy = 100f;
        [SerializeField] private float drainRate = 2f;     // per second
        [SerializeField] private float rechargeRate = 10f; // per second

        private float currentEnergy;
        private bool isRecharging;

        public override void Created()
        {
            currentEnergy = maxEnergy;
        }

        public override void Update()
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
        public override SenseValue Sense(IActionReceiver agent, IComponentReference refs)
        {
            return new SenseValue((int)currentEnergy);
        }
        public void SetRecharging(bool value) => isRecharging = value;
    }
}