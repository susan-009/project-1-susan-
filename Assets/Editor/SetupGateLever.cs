using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

[InitializeOnLoad]
internal static class SetupGateLever
{
    private const string ImagePath = "Assets/Resources/GateLever/GateLever.png";
    private const string ReportPath = @"C:\Users\susan\Documents\Codex\2026-11-10\d-susan-documents\work\gate-lever\setup-report.txt";

    static SetupGateLever()
    {
        EditorApplication.delayCall += Install;
        EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += Install;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += Install;
        };
    }

    [MenuItem("Tools/Set Up Gate Lever")]
    private static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Install;
            return;
        }
        if (!File.Exists(ImagePath)) return;
        TextureImporter importer = AssetImporter.GetAtPath(ImagePath) as TextureImporter;
        if (importer == null)
        {
            AssetDatabase.ImportAsset(ImagePath, ImportAssetOptions.ForceSynchronousImport);
            importer = AssetImporter.GetAtPath(ImagePath) as TextureImporter;
        }
        if (importer == null) return;
        if (importer.userData != "GateLeverV1")
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096;
            importer.isReadable = true;
            importer.SaveAndReimport();
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(ImagePath);
            Color32[] pixels = texture.GetPixels32();
            int split = texture.width / 2;
            var rects = new[]
            {
                new SpriteRect { name = "Lever_Idle", rect = VisibleBounds(pixels, texture.width, texture.height, 0, split),
                    pivot = new Vector2(0.5f, 0f), alignment = SpriteAlignment.BottomCenter,
                    spriteID = new GUID("e6748bb2b48245fbb14e5ff10aae0fb5") },
                new SpriteRect { name = "Lever_Activated", rect = VisibleBounds(pixels, texture.width, texture.height, split, texture.width),
                    pivot = new Vector2(0.5f, 0f), alignment = SpriteAlignment.BottomCenter,
                    spriteID = new GUID("829b49338f2747f2b9e7b10c632ff606") }
            };
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(
                rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            importer.userData = "GateLeverV1";
            importer.isReadable = false;
            importer.SaveAndReimport();
        }
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(ImagePath).OfType<Sprite>().ToArray();
        Sprite idle = sprites.FirstOrDefault(s => s.name == "Lever_Idle");
        Sprite active = sprites.FirstOrDefault(s => s.name == "Lever_Activated");
        if (idle == null || active == null) return;
        int found = 0, connected = 0;
        foreach (Transform item in Object.FindObjectsOfType<Transform>())
        {
            if (item.name.Trim() != "机关门开关") continue;
            found++;
            GateLeverVisual lever = item.GetComponent<GateLeverVisual>();
            if (lever != null && lever.IsConfigured) continue;
            Undo.RegisterFullObjectHierarchyUndo(item.gameObject, "Set up gate lever");
            if (lever == null) lever = Undo.AddComponent<GateLeverVisual>(item.gameObject);
            lever.idleSprite = idle;
            lever.activatedSprite = active;
            lever.InitializeVisual();
            EditorUtility.SetDirty(lever);
            EditorSceneManager.MarkSceneDirty(item.gameObject.scene);
            connected++;
        }
        File.WriteAllText(ReportPath, "Lever sprites imported. Switches found=" + found + "; newly connected=" + connected + ". Scene is marked dirty for saving.");
    }

    private static Rect VisibleBounds(Color32[] pixels, int width, int height, int startX, int endX)
    {
        int minX = endX, minY = height, maxX = startX - 1, maxY = -1;
        for (int y = 0; y < height; y++)
            for (int x = startX; x < endX; x++)
            {
                if (pixels[y * width + x].a < 128) continue;
                minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y);
            }
        if (maxY < minY) throw new System.InvalidOperationException("Gate lever cell is empty.");
        // Native sprite slicing trims padding without altering the PNG or its transparency.
        return new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }
}
