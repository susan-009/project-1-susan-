using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class VerifyPillarArtwork
{
    static VerifyPillarArtwork()
    {
        EditorApplication.delayCall += Verify;
        EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += Verify;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += Verify;
        };
    }

    [MenuItem("Tools/Verify Pillar Artwork")]
    public static void Verify()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Verify;
            return;
        }
        var pillars = Object.FindObjectsOfType<StonePillarController>();
        if (pillars.Length == 0) return;
        var report = new StringBuilder(DateTime.Now.ToString("s") + "\n");
        try
        {
            foreach (StonePillarController pillar in pillars)
            {
                SpriteRenderer root = pillar.GetComponent<SpriteRenderer>();
                SpriteRenderer art = pillar.GetComponentsInChildren<SpriteRenderer>(true)
                    .FirstOrDefault(r => r != root && r.sprite != null);
                Require(art != null, pillar.name + " has no imported artwork.");
                report.AppendLine(pillar.name + ": artwork=" + AssetDatabase.GetAssetPath(art.sprite));
                Check(pillar, art.sprite, false);
                Check(pillar, art.sprite, true);
            }
            report.AppendLine("PASS: all pillars use child artwork; initial hidden/visible states and animation endpoints preserve art-only rendering and colliders.");
        }
        catch (Exception exception)
        {
            report.AppendLine("FAIL: " + exception);
            Debug.LogException(exception);
        }
        File.WriteAllText("Library/PillarArtworkVerification.txt", report.ToString());
    }

    private static void Check(StonePillarController source, Sprite sprite, bool initiallyHidden)
    {
        GameObject go = new GameObject("Pillar artwork check");
        go.hideFlags = HideFlags.HideAndDontSave;
        go.transform.position = new Vector3(1000, 1000, 0);
        try
        {
            SpriteRenderer prototype = go.AddComponent<SpriteRenderer>();
            prototype.sprite = source.GetComponent<SpriteRenderer>().sprite;
            prototype.enabled = false;
            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            GameObject child = new GameObject("Imported artwork");
            child.transform.SetParent(go.transform, false);
            SpriteRenderer art = child.AddComponent<SpriteRenderer>();
            art.sprite = sprite;
            StonePillarController pillar = go.AddComponent<StonePillarController>();
            pillar.startVisible = !initiallyHidden;
            pillar.moveDuration = 0f;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(StonePillarController).GetMethod("Awake", flags).Invoke(pillar, null);
            Require(!prototype.enabled && art.enabled == !initiallyHidden && collider.enabled == !initiallyHidden,
                "Initialization must show only the imported asset when raised.");
            pillar.SetRaisedImmediate(false);
            Require(!prototype.enabled && !art.enabled && !collider.enabled, "Lowered pillars must hide their art and collider.");
            pillar.SetRaisedImmediate(true);
            Require(!prototype.enabled && art.enabled && collider.enabled, "Raised pillars must restore art without the block.");
            MethodInfo animate = typeof(StonePillarController).GetMethod("MoveAnimation", flags);
            foreach (bool visible in new[] { false, true })
            {
                IEnumerator animation = (IEnumerator)animate.Invoke(pillar, new object[] { go.transform.position, visible });
                while (animation.MoveNext()) { }
                Require(!prototype.enabled && art.enabled == visible && collider.enabled == visible,
                    "Animation completion must keep the original block disabled.");
            }
        }
        finally { Object.DestroyImmediate(go); }
    }

    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
