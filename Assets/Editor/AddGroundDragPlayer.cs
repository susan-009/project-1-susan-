using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class AddGroundDragPlayer
{
    [MenuItem("Tools/Add Ground Drag Player")]
    private static void AddPlayer()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject ground = GameObject.Find("new ground");
        Camera camera = Camera.main;
        if (scene.name != "level 2" || ground == null || ground.scene != scene ||
            camera == null || camera.gameObject.scene != scene || !camera.orthographic ||
            GameObject.Find("Player") != null || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Add Ground Drag Player",
                "Open level 2 in Edit mode with new ground and an orthographic Main Camera. The scene must not already contain a Player.", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Add Ground Drag Player");
        int group = Undo.GetCurrentGroup();
        BoxCollider2D floor = ground.GetComponent<BoxCollider2D>();
        if (floor == null)
            floor = Undo.AddComponent<BoxCollider2D>(ground);

        GameObject player = new GameObject("Player");
        Undo.RegisterCreatedObjectUndo(player, "Create Player");
        GroundDragPlayer controller = Undo.AddComponent<GroundDragPlayer>(player);
        SpriteRenderer renderer = player.GetComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        renderer.color = Color.black;
        renderer.sortingOrder = 5;
        float diameter = 0.65f;
        Vector2 spriteSize = renderer.sprite.bounds.size;
        player.transform.localScale = new Vector3(diameter / spriteSize.x, diameter / spriteSize.y, 1f);
        player.GetComponent<CircleCollider2D>().radius = spriteSize.x * 0.5f;
        player.transform.position = new Vector3(floor.bounds.center.x, floor.bounds.max.y + diameter * 0.5f, ground.transform.position.z);

        SerializedObject settings = new SerializedObject(controller);
        settings.FindProperty("ground").objectReferenceValue = floor;
        settings.FindProperty("followCamera").objectReferenceValue = camera;
        settings.ApplyModifiedProperties();
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = player;
    }
}
