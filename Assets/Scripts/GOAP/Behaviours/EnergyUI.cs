using UnityEngine;
using Assets.Scripts.GOAP.Behaviours;

/// <summary>
/// Displays the guard's energy level above their head while they are recharging.
/// Automatically shows/hides based on recharging state.
/// Only displays when guard is visible (not occluded by walls).
/// </summary>
public class EnergyUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnergyBehaviour energyBehaviour;
    
    [Header("UI Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 3f, 0);
    [SerializeField] private Color chargingColor = Color.cyan;
    [SerializeField] private Color lowEnergyColor = Color.red;
    [SerializeField] private int fontSize = 14;
    [SerializeField] private bool showWhenNotRecharging;
    
    [Header("Occlusion Settings")]
    [Tooltip("Layers that can block the UI visibility. IMPORTANT: Should only include solid walls/floors. Exclude: Guards, Lamps, Decorations, UI, Ignore Raycast")]
    [SerializeField] private LayerMask occlusionLayers = -1;
    [Tooltip("Offset from guard's position for raycast target (chest height)")]
    [SerializeField] private float raycastHeightOffset = 1.0f;
    [Tooltip("Small offset to prevent hitting guard's own collider")]
    [SerializeField] private float raycastDistanceBuffer = 0.1f;
    [Tooltip("Enable to see debug rays in Scene view (green = visible, red = occluded)")]
    [SerializeField] private bool debugOcclusion = false;
    
    private void Awake()
    {
        if (energyBehaviour == null)
        {
            energyBehaviour = GetComponent<EnergyBehaviour>();
        }
    }

    private void OnGUI()
    {
        if (energyBehaviour == null)
            return;
        
        // Only show when recharging (or always if showWhenNotRecharging is true)
        if (!energyBehaviour.IsRecharging && !showWhenNotRecharging)
            return;
        
        // Check if camera exists
        if (Camera.main == null)
            return;

        // Calculate position above guard's head
        Vector3 worldPos = transform.position + offset;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        
        // Don't draw if behind camera
        if (screenPos.z < 0)
            return;

        // Occlusion check: Raycast from camera to guard
        if (!IsVisibleToCamera())
            return;

        // Calculate energy percentage
        float energyPercent = (energyBehaviour.CurrentEnergy / energyBehaviour.MaxEnergy) * 100f;
        
        // Choose color based on state
        Color textColor = energyBehaviour.IsRecharging ? chargingColor : lowEnergyColor;
        
        // Create GUI style
        GUIStyle style = new GUIStyle();
        style.normal.textColor = textColor;
        style.fontSize = fontSize;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        
        // Add outline/shadow for better visibility
        GUIStyle shadowStyle = new GUIStyle(style);
        shadowStyle.normal.textColor = Color.black;
        
        // Build display text
        string displayText = energyBehaviour.IsRecharging 
            ? $"RECHARGING\n{energyPercent:F0}%" 
            : $"ENERGY: {energyPercent:F0}%";
        
        // Calculate rect for text
        Vector2 textSize = style.CalcSize(new GUIContent(displayText));
        Rect rect = new Rect(
            screenPos.x - textSize.x / 2, 
            Screen.height - screenPos.y - textSize.y / 2, 
            textSize.x, 
            textSize.y
        );
        
        // Draw shadow first (offset by 1 pixel)
        Rect shadowRect = new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height);
        GUI.Label(shadowRect, displayText, shadowStyle);
        
        // Draw main text
        GUI.Label(rect, displayText, style);
        
        // Optional: Draw energy bar
        DrawEnergyBar(screenPos, energyPercent, textColor);
    }
    
    /// <summary>
    /// Check if the guard is visible to the camera (not occluded by walls/objects)
    /// </summary>
    private bool IsVisibleToCamera()
    {
        if (Camera.main == null)
            return false;

        // Target position on the guard (chest height)
        Vector3 guardPos = transform.position + Vector3.up * raycastHeightOffset;
        Vector3 cameraPos = Camera.main.transform.position;
        Vector3 direction = guardPos - cameraPos;
        float distance = direction.magnitude;

        // Reduce distance slightly to avoid hitting guard's own collider
        float checkDistance = Mathf.Max(0.1f, distance - raycastDistanceBuffer);

        // Raycast from camera to guard, checking for obstructions
        RaycastHit[] hits = Physics.RaycastAll(cameraPos, direction.normalized, checkDistance, occlusionLayers);
        
        // Filter out hits on the guard itself or its children
        foreach (RaycastHit hit in hits)
        {
            // If we hit something that's not the guard or its children, it's occluded
            if (hit.transform != transform && !hit.transform.IsChildOf(transform) && !transform.IsChildOf(hit.transform))
            {
                if (debugOcclusion)
                {
                    Debug.Log($"[EnergyUI] {transform.name} occluded by {hit.transform.name} on layer {LayerMask.LayerToName(hit.transform.gameObject.layer)}");
                }
                return false;
            }
        }
        
        // No obstruction detected, guard is visible
        return true;
    }
    
    private void DrawEnergyBar(Vector3 screenPos, float energyPercent, Color barColor)
    {
        float barWidth = 100f;
        float barHeight = 8f;
        float barYOffset = 25f; // Below the text
        
        // Background bar (dark)
        Rect bgRect = new Rect(
            screenPos.x - barWidth / 2,
            Screen.height - screenPos.y + barYOffset,
            barWidth,
            barHeight
        );
        
        // Draw background
        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.DrawTexture(bgRect, Texture2D.whiteTexture);
        
        // Draw filled portion
        Rect fillRect = new Rect(
            bgRect.x + 1,
            bgRect.y + 1,
            (barWidth - 2) * (energyPercent / 100f),
            barHeight - 2
        );
        
        GUI.color = barColor;
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
        
        // Reset GUI color
        GUI.color = Color.white;
    }

    private void OnDrawGizmos()
    {
        if (!debugOcclusion)
            return;
            
        // Draw a small sphere to show where the UI will appear
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + offset, 0.2f);
        
        // Draw line from camera to guard (for debugging occlusion)
        if (Camera.main != null)
        {
            Vector3 guardPos = transform.position + Vector3.up * raycastHeightOffset;
            bool visible = IsVisibleToCamera();
            
            // Green = visible, Red = occluded
            Gizmos.color = visible ? Color.green : Color.red;
            Gizmos.DrawLine(Camera.main.transform.position, guardPos);
            
            // Draw sphere at raycast target point
            Gizmos.DrawWireSphere(guardPos, 0.15f);
        }
    }
}
