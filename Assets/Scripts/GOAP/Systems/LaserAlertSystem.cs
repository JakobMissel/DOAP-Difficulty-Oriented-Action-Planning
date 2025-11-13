// filepath: Assets/Scripts/GOAP/Systems/LaserAlertSystem.cs
using UnityEngine;

namespace Assets.Scripts.GOAP.Systems
{
    /// <summary>
    /// Global laser alert state (position + lifecycle). Guards read this via sensors to plan a response.
    /// </summary>
    public static class LaserAlertSystem
    {
        /// <summary>True while an alert is active (laser has been triggered recently).</summary>
        public static bool Active { get; private set; }
        /// <summary>World position of the laser that triggered the alert.</summary>
        public static Vector3 Position { get; private set; }
        /// <summary>Optional anchor transform of the triggering laser (preferred source for live position).</summary>
        public static Transform Anchor { get; private set; }
        
        /// <summary>The specific guard assigned to respond to this alert (only this guard will respond).</summary>
        public static Transform AssignedGuard { get; private set; }

        /// <summary>Seconds to keep the alert after the laser deactivates.</summary>
        public static float HoldSecondsAfterDeactivate { get; set; } = 8f;

        private static float _clearAt = -1f;

        /// <summary>True if the world key is active (laser beam is on).</summary>
        public static bool WorldKeyActive { get; private set; }

        /// <summary>Raise or refresh the alert at the specified position.</summary>
        public static void RaiseAlert(Vector3 pos)
        {
            Active = true;
            Position = pos;
            Anchor = null; // explicit position provided
            AssignedGuard = null; // no specific guard assigned
            WorldKeyActive = true; // immediately set world key on
            _clearAt = -1f; // cancel any pending clear
        }

        /// <summary>Raise or refresh the alert anchored at a specific transform (laser), and assign a specific guard to respond.</summary>
        public static void RaiseAlert(Transform anchor, Transform assignedGuard = null)
        {
            if (anchor != null)
            {
                Position = anchor.position;
                Anchor = anchor;
            }
            Active = true;
            AssignedGuard = assignedGuard;
            WorldKeyActive = true; // immediately set world key on
            _clearAt = -1f; // cancel any pending clear
        }

        /// <summary>Check if a specific guard is assigned to respond to the current alert.</summary>
        public static bool IsGuardAssigned(Transform guard)
        {
            // If no specific guard is assigned, all guards can respond (backward compatibility)
            if (AssignedGuard == null)
                return true;
            
            return AssignedGuard == guard;
        }

        /// <summary>Call when the laser deactivates (player left the beam). Schedules a clear.</summary>
        public static void OnLaserDeactivated()
        {
            if (!Active)
                return;
            _clearAt = Time.time + Mathf.Max(0f, HoldSecondsAfterDeactivate);
        }

        /// <summary>Optionally clear immediately (e.g., when a responder arrives).
        /// </summary>
        public static void Clear()
        {
            Active = false;
            Anchor = null;
            AssignedGuard = null;
            WorldKeyActive = false;
            _clearAt = -1f;
        }

        /// <summary>Attempt to clear the world key only if the provided anchor matches the current one.
        /// Returns true if cleared.</summary>
        public static bool TryClearWorldKeyForAnchor(Transform anchorSnapshot)
        {
            if (!WorldKeyActive)
                return false;

            // If anchor changed (another laser triggered), don’t clear yet
            if (Anchor != null && anchorSnapshot != Anchor)
                return false;

            WorldKeyActive = false;
            return true;
        }

        // Clear only the world key (keeps anchor/position intact)
        public static void ClearWorldKey()
        {
            WorldKeyActive = false;
        }

        /// <summary>Return the best current alert position (anchor if set, otherwise stored Position).</summary>
        public static Vector3 GetCurrentPosition()
        {
            if (Anchor != null)
                return Anchor.position;
            return Position;
        }

        /// <summary>Update timer; sensors call this once per sense to enforce delayed clear.</summary>
        public static void UpdateTimer()
        {
            if (Active && _clearAt > 0f && Time.time >= _clearAt)
            {
                Active = false;
                Anchor = null;
                _clearAt = -1f;
            }
        }
    }
}