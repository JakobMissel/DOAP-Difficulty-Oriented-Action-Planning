using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Editor
{
    /// <summary>
    /// Utility to fix Graph Viewer crashes caused by corrupted TextMesh Pro font assets
    /// </summary>
    public static class FixGraphViewerCrash
    {
        [MenuItem("Tools/GOAP/Fix Graph Viewer Crash")]
        public static void FixCrash()
        {
            // Clear the font asset cache
            EditorPrefs.DeleteKey("TMPro.Font.Asset.Cache");
            
            // Clear UI Toolkit text cache
            EditorPrefs.DeleteKey("UIElements.FontAsset.Cache");
            
            // Force reimport of all TextMesh Pro assets
            string[] fontAssets = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets/TextMesh Pro" });
            
            foreach (string guid in fontAssets)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                Debug.Log($"Reimported: {path}");
            }
            
            AssetDatabase.Refresh();
            
            Debug.Log("<color=green>Font cache cleared and TMP assets reimported. Try opening the Graph Viewer again.</color>");
        }
    }
}

