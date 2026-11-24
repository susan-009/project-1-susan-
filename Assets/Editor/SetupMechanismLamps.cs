using System.IO;
using System.Linq;
using System.Text;
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
        if (off == null || on == null || importer == null || importer.userData != "MechanismLampVerticalV3")
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
            // Equal-sized, registered cuts keep the lamp stationary when its state changes.
            float sx = texture.width / 1672f;
            float sy = texture.height / 941f;
            var rectangles = new[]
            {
                new SpriteRect { name = "Mechanism_Off", rect = new Rect(489f * sx, 62f * sy, 272f * sx, 816f * sy),
                    pivot = new Vector2(0.5f, 0.5f), alignment = SpriteAlignment.Center, spriteID = new GUID("b31d84e705d34f1a87628f6f07a1ad10") },
                new SpriteRect { name = "Mechanism_On", rect = new Rect(912f * sx, 62f * sy, 272f * sx, 816f * sy),
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
            importer.userData = "MechanismLampVerticalV3";
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
            if (indicator != null && indicator.unlitSprite == off && indicator.litSprite == on && indicator.Display != null && indicator.ArtworkVersion >= MechanismStoneIndicator.CurrentArtworkVersion) continue;
            if (indicator == null) indicator = Undo.AddComponent<MechanismStoneIndicator>(mechanism.gameObject);
            Undo.RecordObject(indicator, "Update mechanism artwork");
            if (indicator.Display != null)
            {
                Undo.RecordObject(indicator.Display, "Update mechanism artwork");
                Undo.RecordObject(indicator.Display.transform, "Fit mechanism artwork");
            }
            indicator.unlitSprite = off;
            indicator.litSprite = on;
            indicator.InitializeVisual();
            if (indicator.Display != null)
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(indicator.Display);
                PrefabUtility.RecordPrefabInstancePropertyModifications(indicator.Display.transform);
            }
            EditorUtility.SetDirty(indicator);
            EditorSceneManager.MarkSceneDirty(mechanism.gameObject.scene);
            count++;
        }
        File.WriteAllText(@"C:\Users\susan\Documents\Codex\2026-11-10\d-susan-documents\work\mechanism-lamps-setup.txt",
            "Sprites imported: Mechanism_Off, Mechanism_On. Newly connected mechanisms: " + count);
        var report = new StringBuilder(System.DateTime.Now.ToString("s") + "\n");
        report.AppendLine("Atlas: " + ImagePath + "; import=" + importer.userData);
        report.AppendLine("Sprites: " + off.name + " " + off.rect + "; " + on.name + " " + on.rect);
        foreach (EarthMechanismController mechanism in Object.FindObjectsOfType<EarthMechanismController>())
        {
            MechanismStoneIndicator indicator = mechanism.GetComponent<MechanismStoneIndicator>();
            BoxCollider2D collider = mechanism.GetComponent<BoxCollider2D>();
            bool connected = indicator != null && indicator.Display != null &&
                indicator.unlitSprite == off && indicator.litSprite == on &&
                indicator.ArtworkVersion == MechanismStoneIndicator.CurrentArtworkVersion;
            bool fitted = connected && collider != null &&
                Vector3.Distance(indicator.Display.bounds.center, collider.bounds.center) < 0.01f &&
                Vector3.Distance((Vector2)indicator.Display.bounds.size, (Vector2)collider.bounds.size) < 0.01f;
            report.AppendLine(mechanism.name + ": connected=" + connected + ", fitted=" + fitted +
                ", collider=" + (collider != null && collider.enabled && !collider.isTrigger));
        }
        File.WriteAllText("Library/MechanismArtworkV3Report.txt", report.ToString());
    }
}
