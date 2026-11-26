using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class FixCaveBackgroundSorting
{
    private const string BackgroundGuid = "4a2150b303e3135438243081930c0e1c";
    static FixCaveBackgroundSorting()
    {
        EditorApplication.delayCall += Apply;
        EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += Apply;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += Apply;
        };
    }

    [MenuItem("Tools/Fix Cave Background Sorting")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Apply;
            return;
        }
        string path = AssetDatabase.GUIDToAssetPath(BackgroundGuid);
        if (string.IsNullOrEmpty(path)) return;
        var report = new StringBuilder(DateTime.Now.ToString("s") + "\n");
        int count = 0;
        foreach (SpriteRenderer renderer in Object.FindObjectsOfType<SpriteRenderer>(true)
            .Where(r => r.sprite != null && AssetDatabase.GetAssetPath(r.sprite) == path))
        {
            if (renderer.gameObject.scene.path != "Assets/Scenes/level 2-Prototype.unity") continue;
            if (renderer.sortingLayerID != 0 || renderer.sortingOrder != -100)
            {
                Undo.RecordObject(renderer, "Place cave background behind gameplay sprites");
                renderer.sortingLayerID = 0;
                renderer.sortingOrder = -100;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                EditorUtility.SetDirty(renderer);
                EditorSceneManager.MarkSceneDirty(renderer.gameObject.scene);
            }
            count++;
            report.AppendLine(renderer.name + ": layer=" + renderer.sortingLayerName + "; order=" + renderer.sortingOrder);
        }
        report.AppendLine("Background instances configured: " + count);
        File.WriteAllText("Library/CaveBackgroundSortingReport.txt", report.ToString());
    }
}
