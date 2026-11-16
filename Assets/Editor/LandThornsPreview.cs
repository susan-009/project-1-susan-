using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
internal static class LandThornsPreview
{
    static LandThornsPreview()
    {
        EditorApplication.delayCall += Install;
    }

    [MenuItem("Tools/Add Land1 Thorns")]
    private static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        GameObject land = null;
        foreach (Transform item in Object.FindObjectsOfType<Transform>())
            if (string.Equals(item.name.Trim(), "Land1", System.StringComparison.OrdinalIgnoreCase))
                land = item.gameObject;
        if (land == null || land.transform.Find("Land1 Thorn Bush") != null) return;
        BoxCollider2D floor = land.GetComponent<BoxCollider2D>();
        if (floor == null) floor = Undo.AddComponent<BoxCollider2D>(land);
        Physics2D.SyncTransforms();
        const string meshPath = "Assets/Land1ThornMesh.asset";
        const string materialPath = "Assets/Land1ThornMaterial.mat";
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (mesh == null)
        {
            mesh = new Mesh();
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            for (int i = 0; i < 12; i++)
            {
                float x = -0.5f + i / 12f;
                int index = vertices.Count;
                vertices.Add(new Vector3(x, 0f, 0f));
                vertices.Add(new Vector3(x + 1f / 24f, i % 2 == 0 ? 0.5f : 0.38f, 0f));
                vertices.Add(new Vector3(x + 1f / 12f, 0f, 0f));
                triangles.Add(index);
                triangles.Add(index + 1);
                triangles.Add(index + 2);
            }
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, meshPath);
        }
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("Sprites/Default"));
            material.color = new Color(0.2f, 0.55f, 0.18f, 1f);
            AssetDatabase.CreateAsset(material, materialPath);
        }
        GameObject bush = new GameObject("Land1 Thorn Bush");
        Undo.RegisterCreatedObjectUndo(bush, "Add Land1 Thorns");
        bush.transform.position = new Vector3(floor.bounds.center.x, floor.bounds.max.y, land.transform.position.z);
        bush.transform.localScale = new Vector3(floor.bounds.size.x, 1f, 1f);
        bush.transform.SetParent(land.transform, true);
        bush.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = bush.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingOrder = 7;
        BoxCollider2D trigger = bush.AddComponent<BoxCollider2D>();
        trigger.size = new Vector2(1f, 0.5f);
        trigger.offset = new Vector2(0f, 0.25f);
        trigger.isTrigger = true;
        bush.AddComponent<BurnableThorns>();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(land.scene);
        Selection.activeGameObject = bush;
        SceneView.RepaintAll();
    }
}
