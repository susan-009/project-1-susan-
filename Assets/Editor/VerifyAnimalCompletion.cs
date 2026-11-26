using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class VerifyAnimalCompletion
{
    static VerifyAnimalCompletion() { EditorApplication.delayCall += Verify; }

    [MenuItem("Tools/Verify Animal Contact Completion")]
    private static void Verify()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Verify;
            return;
        }
        if (SessionState.GetBool("AnimalContactCompletion.CheckedV1", false)) return;
        GameObject animal = GameObject.Find("animal");
        if (animal == null) return;
        try
        {
            Require(animal.GetComponent<AnimalEscape>() != null, "Scene animal needs AnimalEscape.");
            Check(false, false);
            Check(true, false);
            Check(false, true);
            SessionState.SetBool("AnimalContactCompletion.CheckedV1", true);
            File.WriteAllText("Library/AnimalContactCompletionChecks.txt", DateTime.Now.ToString("s") +
                "\nPASS: scene animal connected; no contact stays incomplete; touching completes once; " +
                "fast pass-through contact completes; teleport across animal does not complete; " +
                "animal stays still; movement and skill updates stop after completion; no exit or pillar requirements.\n");
        }
        catch (Exception exception)
        {
            File.WriteAllText("Library/AnimalContactCompletionChecks.txt", DateTime.Now.ToString("s") + "\nFAIL: " + exception);
            Debug.LogException(exception);
        }
    }

    private static void Check(bool swept, bool teleport)
    {
        GameObject a = new GameObject("Animal completion fixture");
        GameObject p = new GameObject("Player completion fixture");
        a.hideFlags = p.hideFlags = HideFlags.HideAndDontSave;
        a.transform.position = new Vector3(12000, 12000, 0);
        try
        {
            CircleCollider2D circle = a.AddComponent<CircleCollider2D>();
            circle.radius = 0.2f;
            AnimalEscape goal = a.AddComponent<AnimalEscape>();
            GroundDragPlayer controller = p.AddComponent<GroundDragPlayer>();
            CapsuleCollider2D body = p.GetComponent<CapsuleCollider2D>();
            body.size = new Vector2(0.5f, 1f);
            body.direction = CapsuleDirection2D.Vertical;
            p.transform.position = a.transform.position + Vector3.left * (teleport ? 4f : 1f);
            goal.Initialize(controller);
            Vector3 originalAnimal = a.transform.position;
            Require(!goal.TryCompleteOnContact() && !controller.IsLevelComplete, "No contact must not win.");
            p.transform.position = a.transform.position + (teleport ? Vector3.right * 4f : swept ? Vector3.right : Vector3.zero);
            Physics2D.SyncTransforms();
            if (teleport)
            {
                Require(!goal.TryCompleteOnContact() && !goal.IsCompleted, "Teleporting across the animal must not win.");
                return;
            }
            Require(goal.TryCompleteOnContact() && goal.IsCompleted && controller.IsLevelComplete,
                "Player contact must complete the level immediately.");
            Require(!goal.TryCompleteOnContact(), "Completion must happen only once.");
            Vector3 playerPosition = p.transform.position;
            controller.SetWingActive(true);
            controller.RelocateByStone(Vector3.zero);
            controller.ReturnToLanding(Vector3.zero);
            controller.StartFalling();
            controller.ShowPillarAnimation(new Bounds(), 1f, () => { throw new Exception("Mechanism ran after completion."); });
            Invoke(controller, "Update");
            Invoke(controller, "LateUpdate");
            Invoke(p.AddComponent<PlayerSpiritFollowers>(), "Update");
            PlayerSpiritFollowers followers = p.GetComponent<PlayerSpiritFollowers>();
            typeof(PlayerSpiritFollowers).GetField("player", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(followers, controller);
            Invoke(followers, "LateUpdate");
            StoneAbility stone = p.AddComponent<StoneAbility>();
            typeof(StoneAbility).GetField("player", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(stone, controller);
            typeof(StoneAbility).GetField("stone", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(stone, a.transform);
            Invoke(stone, "LateUpdate");
            Require(!controller.IsWingActive && !controller.CanRideWingPlatform && p.transform.position == playerPosition,
                "Player must stay stopped after completion.");
            Invoke(goal, "LateUpdate");
            Require(a.transform.position == originalAnimal, "Animal must never follow or move.");
        }
        finally { Object.DestroyImmediate(a); Object.DestroyImmediate(p); }
    }

    private static void Invoke(object instance, string method)
    {
        instance.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(instance, null);
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
