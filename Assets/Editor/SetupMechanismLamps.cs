using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

[InitializeOnLoad]
internal static class SetupMechanismLamps
{
    private const string ImagePath = "Assets/Resources/Mechanism/MechanismLamp.png";
    static SetupMechanismLamps()
    {
        EditorApplication.delayCall += Install;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += Install;
        };
        EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += Install;
    }

    [MenuItem("Tools/Set Up Mechanism Lamps")]
    private static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Install;
            return;
        }
        if (!File.Exists(ImagePath)) return;
        Sprite off = AssetDatabase.LoadAllAssetsAtPath(ImagePath).OfType<Sprite>().FirstOrDefault(s => s.name == "Mechanism_Off");
        Sprite on = AssetDatabase.LoadAllAssetsAtPath(ImagePath).OfType<Sprite>().FirstOrDefault(s => s.name == "Mechanism_On");
        var importer = AssetImporter.GetAtPath(ImagePath) as TextureImporter;
        if (off == null || on == null || importer == null || importer.userData != "MechanismLampVerticalV2")
        {
            if (importer == null)
            {
                AssetDatabase.ImportAsset(ImagePath, ImportAssetOptions.ForceSynchronousImport);
                importer = AssetImporter.GetAtPath(ImagePath) as TextureImporter;
            }
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.isReadable = true;
            importer.SaveAndReimport();
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(ImagePath);
            // Explicit aligned cuts exclude the decorative haze outside each narrow device.
            float sx = texture.width / 1536f;
            float sy = texture.height / 1024f;
            var rectangles = new[]
            {
                new SpriteRect { name = "Mechanism_Off", rect = new Rect(498f * sx, 100f * sy, 150f * sx, 834f * sy),
                    pivot = new Vector2(0.5f, 0.5f), alignment = SpriteAlignment.Center, spriteID = new GUID("b31d84e705d34f1a87628f6f07a1ad10") },
                new SpriteRect { name = "Mechanism_On", rect = new Rect(883f * sx, 100f * sy, 150f * sx, 834f * sy),
                    pivot = new Vector2(0.5f, 0.5f), alignment = SpriteAlignment.Center, spriteID = new GUID("94f47c601c594e59a5cf395061e8b831") }
            };
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            provider.SetSpriteRects(rectangles);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(
                rectangles.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            importer.userData = "MechanismLampVerticalV2";
            importer.isReadable = false;
            importer.SaveAndReimport();
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(ImagePath).OfType<Sprite>().ToArray();
            off = sprites.First(s => s.name == "Mechanism_Off");
            on = sprites.First(s => s.name == "Mechanism_On");
        }
        int count = 0;
        foreach (EarthMechanismController mechanism in Object.FindObjectsOfType<EarthMechanismController>())
        {
            MechanismStoneIndicator indicator = mechanism.GetComponent<MechanismStoneIndicator>();
            if (indicator != null && indicator.unlitSprite != null && indicator.litSprite != null && indicator.Display != null && indicator.ArtworkVersion >= 2) continue;
            if (indicator == null) indicator = Undo.AddComponent<MechanismStoneIndicator>(mechanism.gameObject);
            indicator.unlitSprite = off;
            indicator.litSprite = on;
            indicator.InitializeVisual();
            EditorUtility.SetDirty(indicator);
            EditorSceneManager.MarkSceneDirty(mechanism.gameObject.scene);
            count++;
        }
        File.WriteAllText(@"C:\Users\susan\Documents\Codex\2026-11-10\d-susan-documents\work\mechanism-lamps-setup.txt",
            "Sprites imported: Mechanism_Off, Mechanism_On. Newly connected mechanisms: " + count);
    }
}
