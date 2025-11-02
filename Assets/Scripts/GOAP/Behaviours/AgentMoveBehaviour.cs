using CrashKonijn.Agent.Core;
using CrashKonijn.Agent.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace CrashKonijn.Docs.GettingStarted.Behaviours
{
    public class AgentMoveBehaviour : MonoBehaviour
    {
        private AgentBehaviour agent;
        private NavMeshAgent navMeshAgent;
        private ITarget currentTarget;
        private bool shouldMove;

        private void Awake()
        {
            this.agent = this.GetComponent<AgentBehaviour>();
            this.navMeshAgent = this.GetComponent<NavMeshAgent>();
            
            if (this.navMeshAgent != null)
            {
                this.navMeshAgent.updateRotation = true;
                this.navMeshAgent.updatePosition = true;
            }
        }

        private void OnEnable()
        {
            this.agent.Events.OnTargetInRange += this.OnTargetInRange;
            this.agent.Events.OnTargetChanged += this.OnTargetChanged;
            this.agent.Events.OnTargetNotInRange += this.TargetNotInRange;
            this.agent.Events.OnTargetLost += this.TargetLost;
        }

        private void OnDisable()
        {
            this.agent.Events.OnTargetInRange -= this.OnTargetInRange;
            this.agent.Events.OnTargetChanged -= this.OnTargetChanged;
            this.agent.Events.OnTargetNotInRange -= this.TargetNotInRange;
            this.agent.Events.OnTargetLost -= this.TargetLost;
        }
        
        private void TargetLost()
        {
            this.currentTarget = null;
            this.shouldMove = false;
            
            if (this.navMeshAgent != null && this.navMeshAgent.enabled)
            {
                this.navMeshAgent.isStopped = true;
                this.navMeshAgent.ResetPath();
            }
        }

        private void OnTargetInRange(ITarget target)
        {
            this.shouldMove = false;
            
            if (this.navMeshAgent != null && this.navMeshAgent.enabled)
            {
                this.navMeshAgent.isStopped = true;
            }
        }

        private void OnTargetChanged(ITarget target, bool inRange)
        {
            this.currentTarget = target;
            this.shouldMove = !inRange;
        }

        private void TargetNotInRange(ITarget target)
        {
            this.shouldMove = true;
        }

        public void Update()
        {
            if (this.agent.IsPaused)
            {
                if (this.navMeshAgent != null && this.navMeshAgent.enabled)
                {
                    this.navMeshAgent.isStopped = true;
                }
                return;
            }

            if (!this.shouldMove)
            {
                if (this.navMeshAgent != null && this.navMeshAgent.enabled)
                {
                    this.navMeshAgent.isStopped = true;
                }
                return;
            }
            
            if (this.currentTarget == null)
            {
                if (this.navMeshAgent != null && this.navMeshAgent.enabled)
                {
                    this.navMeshAgent.isStopped = true;
                }
                return;
            }
            
            // Use NavMeshAgent if available, otherwise fall back to simple movement
            if (this.navMeshAgent != null && this.navMeshAgent.enabled && this.navMeshAgent.isOnNavMesh)
            {
                this.navMeshAgent.isStopped = false;
                this.navMeshAgent.SetDestination(this.currentTarget.Position);
            }
            else
            {
                // Fallback: direct movement (original behavior)
                this.transform.position = Vector3.MoveTowards(
                    this.transform.position, 
                    new Vector3(this.currentTarget.Position.x, this.transform.position.y, this.currentTarget.Position.z), 
                    Time.deltaTime * (this.navMeshAgent != null ? this.navMeshAgent.speed : 3.5f)
                );
            }
        }

        private void OnDrawGizmos()
        {
            if (this.currentTarget == null)
                return;
            
            Gizmos.DrawLine(this.transform.position, this.currentTarget.Position);
        }
    }
}