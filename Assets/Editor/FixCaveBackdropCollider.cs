using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class FixCaveBackdropCollider
{
    static FixCaveBackdropCollider()
    {
        EditorApplication.delayCall += InspectAndFix;
        EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += InspectAndFix;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += InspectAndFix;
        };
    }

    [MenuItem("Tools/Repair Cave Backdrop Collision")]
    public static void InspectAndFix()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += InspectAndFix;
            return;
        }
        GameObject backdrop = GameObject.Find("\u5899\u58c1");
        GroundDragPlayer player = Object.FindObjectOfType<GroundDragPlayer>();
        if (backdrop == null || player == null) return;
        BoxCollider2D collider = backdrop.GetComponent<BoxCollider2D>();
        SpriteRenderer image = backdrop.GetComponent<SpriteRenderer>();
        if (collider == null || image == null || image.bounds.size.x < 15f || image.bounds.size.y < 8f ||
            backdrop.transform.position.z <= player.transform.position.z) return;
        var report = new StringBuilder(DateTime.Now.ToString("s") + "\n");
        try
        {
            Physics2D.SyncTransforms();
            CapsuleCollider2D body = player.GetComponent<CapsuleCollider2D>();
            Vector2 center = body.transform.TransformPoint(body.offset);
            bool initiallyBlockedByBackdrop = PlayerCapsuleShape.CastAll(body, center, Vector2.up, 0.203f, -0.003f)
                .Any(hit => hit.collider == collider && hit.distance < 0.0001f);
            report.AppendLine("Backdrop initial-overlap upcast=" + initiallyBlockedByBackdrop);
            if (collider.enabled)
            {
                Undo.RecordObject(collider, "Disable decorative cave backdrop collision");
                collider.enabled = false;
                EditorUtility.SetDirty(collider);
                EditorSceneManager.MarkSceneDirty(backdrop.scene);
            }
            Physics2D.SyncTransforms();
            Require(!PlayerCapsuleShape.CastAll(body, center, Vector2.up, 0.203f, -0.003f)
                .Any(hit => hit.collider == collider), "Disabled backdrop still blocks movement.");
            report.AppendLine("Backdrop collider disabled; renderer retained.");
            if (!SessionState.GetBool("BackdropCollisionChecksV2", false))
            {
                string checks = VerifyMovement(body);
                File.WriteAllText("Library/WingUpwardCollisionChecks.txt", DateTime.Now.ToString("s") + "\n" + checks);
                SessionState.SetBool("BackdropCollisionChecksV2", true);
            }
            report.AppendLine("PASS");
        }
        catch (Exception exception)
        {
            report.AppendLine("FAIL: " + exception);
            Debug.LogException(exception);
        }
        File.WriteAllText("Library/CaveBackdropCollisionRepair.txt", report.ToString());
    }

    private static string VerifyMovement(CapsuleCollider2D source)
    {
        GameObject actor = new GameObject("Wing movement regression check");
        GameObject ceiling = new GameObject("Ceiling check");
        GameObject wall = new GameObject("Wall check");
        // Hide test objects from the Hierarchy without excluding them from editor physics.
        foreach (GameObject go in new[] { actor, ceiling, wall }) go.hideFlags = HideFlags.HideInHierarchy;
        try
        {
            Vector3 origin = new Vector3(1000, 1000, 0);
            actor.transform.position = origin;
            actor.transform.localScale = source.transform.lossyScale;
            CapsuleCollider2D capsule = actor.AddComponent<CapsuleCollider2D>();
            capsule.size = source.size; capsule.offset = source.offset; capsule.direction = source.direction;
            GroundDragPlayer controller = actor.AddComponent<GroundDragPlayer>();
            const BindingFlags fields = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(GroundDragPlayer).GetField("playerCollider", fields).SetValue(controller, capsule);
            typeof(GroundDragPlayer).GetField("wingActive", fields).SetValue(controller, true);
            MethodInfo move = typeof(GroundDragPlayer).GetMethod("MoveWingStep", fields);
            // Verify observable movement directly; an enclosing shape is not a reliable
            // initial-overlap sweep fixture and must not be a precondition for this test.
            Physics2D.SyncTransforms();
            bool blocked = (bool)move.Invoke(controller, new object[] { Vector2.up * 0.2f });
            Require(!blocked && actor.transform.position.y > origin.y + 0.19f,
                "The player must rise after disabling the backdrop.");

            actor.transform.position = origin;
            Physics2D.SyncTransforms();
            ceiling.transform.position = new Vector3(origin.x, capsule.bounds.max.y + 0.3f, 0);
            BoxCollider2D roof = ceiling.AddComponent<BoxCollider2D>();
            roof.size = new Vector2(5, 0.1f);
            Physics2D.SyncTransforms();
            blocked = (bool)move.Invoke(controller, new object[] { Vector2.up * 2f });
            Require(blocked && capsule.bounds.max.y <= roof.bounds.min.y + 0.01f,
                "The real ceiling must still block ascent.");
            roof.enabled = false;
            actor.transform.position = origin;
            Physics2D.SyncTransforms();
            wall.transform.position = new Vector3(capsule.bounds.max.x + 0.3f, capsule.bounds.center.y, 0);
            BoxCollider2D side = wall.AddComponent<BoxCollider2D>();
            side.size = new Vector2(0.1f, 5f);
            Physics2D.SyncTransforms();
            blocked = (bool)move.Invoke(controller, new object[] { Vector2.right * 2f });
            Require(blocked && capsule.bounds.max.x <= side.bounds.min.x + 0.01f,
                "A real side wall must still block horizontal movement.");
            return "PASS V2: unobstructed upward displacement works; real ceilings and side walls still block.";
        }
        finally
        {
            Object.DestroyImmediate(actor);
            Object.DestroyImmediate(ceiling); Object.DestroyImmediate(wall);
        }
    }

    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
