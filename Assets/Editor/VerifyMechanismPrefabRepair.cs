using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Leave already-open scenes in place and never save them during verification.
[InitializeOnLoad]
internal static class VerifyMechanismPrefabRepair
{
    private const string RepairGuid = "7031fc0d53e697748ad2c0d823bc4674";
    private const string SessionKey = "MechanismPrefabRepair.VerifiedV1";

    static VerifyMechanismPrefabRepair()
    {
        EditorApplication.delayCall += VerifyOnce;
    }

    private static void VerifyOnce()
    {
        if (SessionState.GetBool(SessionKey, false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += VerifyOnce;
            return;
        }
        SessionState.SetBool(SessionKey, true);
        Verify();
    }

    [MenuItem("Tools/Verify Mechanism Prefab Repair")]
    private static void Verify()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var report = new StringBuilder(DateTime.Now.ToString("s") + "\n");
        int issues = 0;
        try
        {
            string path = AssetDatabase.GUIDToAssetPath(RepairGuid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException("Restored prefab did not import.");
            CheckId(prefab, 1263557903461873627L);
            CheckId(prefab.transform, 887741285588429588L);
            SpriteRenderer renderer = prefab.GetComponent<SpriteRenderer>();
            CheckId(renderer, 3664577603386334661L);
            if (renderer.sprite == null || renderer.sharedMaterial == null)
                throw new InvalidOperationException("Prefab sprite or material did not resolve.");
            report.AppendLine("PASS prefab GUID, object IDs, sprite and material: " + path);

            foreach (string scenePath in new[] {
                "Assets/Scenes/level 2-Prototype.unity",
                "Assets/Scenes/level 2-Prototype 1.unity" })
            {
                if (!File.Exists(scenePath)) continue;
                Scene activeScene = SceneManager.GetActiveScene();
                Scene inspected = SceneManager.GetSceneByPath(scenePath);
                bool alreadyLoaded = inspected.IsValid() && inspected.isLoaded;
                if (!alreadyLoaded) inspected = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                int missing = 0;
                int mechanisms = 0;
                int unresolvedDisplays = 0;
                try
                {
                    foreach (GameObject root in inspected.GetRootGameObjects())
                    {
                        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                            if (PrefabUtility.IsPrefabAssetMissing(child.gameObject)) missing++;
                        foreach (MechanismStoneIndicator indicator in root.GetComponentsInChildren<MechanismStoneIndicator>(true))
                        {
                            mechanisms++;
                            if (indicator.Display == null || indicator.Display.sprite == null) unresolvedDisplays++;
                        }
                    }
                }
                finally
                {
                    if (!alreadyLoaded) EditorSceneManager.CloseScene(inspected, true);
                    if (activeScene.IsValid() && activeScene.isLoaded) SceneManager.SetActiveScene(activeScene);
                }
                if (missing != 0 || unresolvedDisplays != 0 || mechanisms != 3) issues++;
                report.AppendLine(scenePath + ": missing prefabs=" + missing +
                    ", mechanisms=" + mechanisms + ", unresolved displays=" + unresolvedDisplays);
            }
        }
        catch (Exception exception)
        {
            issues++;
            report.AppendLine(exception.ToString());
        }
        report.AppendLine(issues == 0 ? "PASS" : "FAIL");
        File.WriteAllText("Library/MechanismPrefabRepairReport.txt", report.ToString());
        if (issues == 0) Debug.Log("Mechanism prefab repair verified in both scene files. Report: Library/MechanismPrefabRepairReport.txt");
        else Debug.LogWarning("Mechanism prefab repair needs inspection. Report: Library/MechanismPrefabRepairReport.txt");
    }

    private static void CheckId(UnityEngine.Object asset, long expected)
    {
        if (asset == null || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long id) ||
            guid != RepairGuid || id != expected)
            throw new InvalidOperationException("Restored prefab object ID mismatch: " + expected);
    }
}
