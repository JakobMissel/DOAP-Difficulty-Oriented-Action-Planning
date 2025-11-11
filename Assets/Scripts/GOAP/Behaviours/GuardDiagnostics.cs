using UnityEngine;
using CrashKonijn.Goap.Runtime;
using CrashKonijn.Agent.Runtime;

namespace Assets.Scripts.GOAP.Behaviours
{
    /// <summary>
    /// Attach this to a guard to diagnose multi-agent issues.
    /// Shows which action is running, goal priorities, and detects conflicts.
    /// </summary>
    public class GuardDiagnostics : MonoBehaviour
    {
        [Header("Display Settings")]
        public bool showDebugInfo = true;
        public Color debugColor = Color.cyan;
        
        private GoapActionProvider provider;
        private AgentBehaviour agent;
        private BrainBehaviour brain;
        
        private void Awake()
        {
            provider = GetComponent<GoapActionProvider>();
            agent = GetComponent<AgentBehaviour>();
            brain = GetComponent<BrainBehaviour>();
        }

        private void OnGUI()
        {
            if (!showDebugInfo || provider == null || agent == null)
                return;

            // Check if camera exists
            if (Camera.main == null)
                return;

            // Position debug info above the guard
            Vector3 worldPos = transform.position + Vector3.up * 3f;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            
            if (screenPos.z < 0)
                return; // Behind camera

            GUIStyle style = new GUIStyle();
            style.normal.textColor = debugColor;
            style.fontSize = 12;
            style.fontStyle = FontStyle.Bold;
            
            // Build info string
            string info = $"{gameObject.name}\n";
            
            // Show current action (use newer API)
            var currentAction = agent.ActionState?.Action;
            if (currentAction != null)
            {
                // Get action name from the type name
                string actionName = currentAction.GetType().Name;
                info += $"Action: {actionName}\n";
            }
            else
            {
                info += "Action: None\n";
            }
            
            // Show current goal (use newer API)
            var currentGoal = provider.CurrentPlan?.Goal;
            if (currentGoal != null)
            {
                string goalName = currentGoal.GetType().Name;
                info += $"Goal: {goalName}\n";
            }
            else
            {
                info += "Goal: None\n";
            }
            
            // Show brain state
            if (brain != null)
            {
                if (brain.IsPlayerCaught)
                    info += "STATE: Player Caught\n";
                else if (brain.HasLastKnownPosition)
                    info += "STATE: Has Last Known\n";
                else if (brain.HasHeardPlayerNoise)
                    info += "STATE: Heard Player Noise\n";
                else if (brain.HasHeardDistractionNoise)
                    info += "STATE: Heard Distraction\n";
                else
                    info += "STATE: Normal\n";
            }
            
            // Draw the text
            Rect rect = new Rect(screenPos.x - 100, Screen.height - screenPos.y - 50, 200, 100);
            GUI.Label(rect, info, style);
        }

        private void OnDrawGizmos()
        {
            if (!showDebugInfo)
                return;

            // Draw a colored sphere above the guard
            Gizmos.color = debugColor;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2.5f, 0.3f);
            
            // Draw current patrol zone connection if available
            var patrol = GetComponent<PatrolRouteBehaviour>();
            if (patrol != null && patrol.waypointParent != null)
            {
                Gizmos.color = debugColor;
                Gizmos.DrawLine(transform.position, patrol.waypointParent.position);
            }
        }
    }
}
