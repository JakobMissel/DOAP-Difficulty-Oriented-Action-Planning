// filepath: Assets/Scripts/DDA/DdaTestingModeBehaviour.cs
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; // Keyboard
#endif

namespace Assets.Scripts.DDA
{
    [DisallowMultipleComponent]
    public class DdaTestingModeBehaviour : MonoBehaviour
    {
        [Header("Testing Mode")]
        [Tooltip("Enable testing difficulty override when this behaviour becomes active")] 
        public bool enableOnStart = true;

        [Tooltip("Show an on-screen overlay with the current testing difficulty and hints")] 
        public bool showOverlay = true;

        [Range(0, 100)]
        public int startDifficultyPercent = 50;

        [Header("Overlay Style")] 
        public Color overlayColor = new Color(1,1,1,0.9f);
        public int fontSize = 14;
        public Vector2 overlayOffset = new Vector2(12, 12);

        private int currentPercent;

        private void OnEnable()
        {
            if (enableOnStart)
            {
                DifficultyTracker.EnableTestingMode(true);
                DifficultyTracker.SetTestingDifficultyPercent(Mathf.Clamp(startDifficultyPercent, 0, 100));
            }

            currentPercent = DifficultyTracker.GetDifficultyI();
        }

        private void Update()
        {
            int? setTo = null;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.pageUpKey.wasPressedThisFrame)
                    setTo = DifficultyTracker.StepTestingDifficulty(+1);
                else if (kb.pageDownKey.wasPressedThisFrame)
                    setTo = DifficultyTracker.StepTestingDifficulty(-1);
            }
#else
            if (Input.GetKeyDown(KeyCode.PageUp))
                setTo = DifficultyTracker.StepTestingDifficulty(+1);
            else if (Input.GetKeyDown(KeyCode.PageDown))
                setTo = DifficultyTracker.StepTestingDifficulty(-1);
#endif

            if (setTo.HasValue)
            {
                currentPercent = setTo.Value;
#if UNITY_EDITOR
                Debug.Log($"[DDA] Testing difficulty set to {currentPercent}% \n Use PgUp/PgDn to adjust");
#endif
            }
        }

        private void OnGUI()
        {
            if (!showOverlay)
                return;

            var oldColor = GUI.color;

            GUI.color = overlayColor;

            string mode = DifficultyTracker.IsTestingMode() ? "Testing" : "Live";
            int percent = DifficultyTracker.GetDifficultyI();
            string text = $"DDA: {mode} difficulty = {percent}% \n(Use PgUp/PgDn to adjust 0/25/50/75/100)";

            // Build a style that matches our desired appearance
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
                richText = false,
            };

            // Constrain to screen width so we never clip horizontally; height is computed
            float padding = 8f;
            float maxWidth = Mathf.Max(50f, Screen.width - overlayOffset.x - padding);
            float height = style.CalcHeight(new GUIContent(text), maxWidth);

            var rect = new Rect(overlayOffset.x, overlayOffset.y, maxWidth, height);
            GUI.Label(rect, text, style);

            GUI.color = oldColor;
        }
    }
}
