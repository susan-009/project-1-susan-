using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class RepairScriptImports
{
    private const string RepairKey = "Susan.RepairScriptImports.v1";

    static RepairScriptImports()
    {
        if (!SessionState.GetBool(RepairKey, false))
        {
            SessionState.SetBool(RepairKey, true);
            EditorApplication.delayCall += RepairWhenReady;
        }
    }

    private static void RepairWhenReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += RepairWhenReady;
            return;
        }
        Repair();
    }

    [MenuItem("Tools/Repair Script Imports")]
    private static void Repair()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Exit Play mode before repairing script imports.");
            return;
        }

        HashSet<string> paths = new HashSet<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" }))
            paths.Add(AssetDatabase.GUIDToAssetPath(guid));
        // Refresh imports without removing components or their serialized values.
        foreach (string path in paths)
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        AssetDatabase.Refresh();
        int missing = 0;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            {
                int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject);
                if (count == 0) continue;
                missing += count;
                Debug.LogWarning("Missing script after reimport: " + scene.name + "/" + item.name +
                    " (" + count + ").", item.gameObject);
            }
        }
        Debug.Log("Script imports refreshed. Missing components in loaded scenes: " + missing + ".");
    }
}
