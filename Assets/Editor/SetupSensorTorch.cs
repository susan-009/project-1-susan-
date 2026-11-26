using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class SetupSensorTorch
{
    private const string Folder = "Assets/Resources/SensorTorch/";
    static SetupSensorTorch()
    {
        EditorApplication.delayCall += Install;
        EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += Install;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += Install;
        };
    }

    public static void ConnectAndSaveScene()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/level 2-Prototype.unity");
        Install();
        SensorTorch torch = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<SensorTorch>(true))
            .FirstOrDefault(t => t.name.Trim().Replace(" ", "") == "\u706b\u70ac1" ||
                t.name.Trim().Replace(" ", "") == "\u706b\u70ae1" || t.name == "Torch1");
        Require(torch != null && torch.IsConfigured, "Scene torch was not connected.");
        string result = CheckStates(torch.unlitSprite, torch.detectedSprite, torch.burningSprite);
        Require(EditorSceneManager.SaveScene(scene), "Could not save the torch scene.");
        File.WriteAllText("Library/SensorTorchBatchVerification.txt", DateTime.Now.ToString("s") + "\n" +
            torch.name + ": configured=True\n" + result + "\nScene saved successfully.\n");
    }

    [MenuItem("Tools/Set Up Sensor Torch")]
    private static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Install;
            return;
        }
        var report = new StringBuilder(DateTime.Now.ToString("s") + "\n");
        try
        {
            Sprite detected = Import("Detected");
            Sprite burning = Import("Burning");
            if (detected == null || burning == null) return;
            int found = 0;
            foreach (Transform item in Object.FindObjectsOfType<Transform>())
            {
                string name = item.name.Trim().Replace(" ", "");
                if (!IsSensorTorchName(name)) continue;
                found++;
                SensorTorch torch = item.GetComponent<SensorTorch>();
                if (torch == null || !torch.IsConfigured || torch.detectedSprite != detected || torch.burningSprite != burning)
                {
                    Undo.RegisterFullObjectHierarchyUndo(item.gameObject, "Connect sensor torch states");
                    if (torch == null) torch = Undo.AddComponent<SensorTorch>(item.gameObject);
                    torch.detectedSprite = detected;
                    torch.burningSprite = burning;
                    torch.InitializeVisual();
                    EditorUtility.SetDirty(torch);
                    EditorSceneManager.MarkSceneDirty(item.gameObject.scene);
                }
                report.AppendLine(item.name + ": configured=" + torch.IsConfigured +
                    "; unlit=" + (torch.unlitSprite != null) + "; detected=" + (torch.detectedSprite != null) +
                    "; burning=" + (torch.burningSprite != null));
                string checkKey = "SensorTorch.ChecksV2:" + item.GetInstanceID();
                if (torch.IsConfigured && !SessionState.GetBool(checkKey, false))
                {
                    report.AppendLine(CheckStates(torch.unlitSprite, detected, burning));
                    SessionState.SetBool(checkKey, true);
                }
            }
            report.AppendLine("Matching scene torches: " + found);
            string independenceKey = "SensorTorch.IndependentChecksV2:" + found;
            if (found >= 2 && !SessionState.GetBool(independenceKey, false))
            {
                SensorTorch[] torches = Object.FindObjectsOfType<SensorTorch>()
                    .Where(t => IsSensorTorchName(t.name.Trim().Replace(" ", ""))).ToArray();
                string result = CheckIndependentTorches(torches);
                File.WriteAllText("Library/SensorTorchIndependenceChecks.txt", DateTime.Now.ToString("s") + "\n" + result);
                SessionState.SetBool(independenceKey, true);
            }
        }
        catch (Exception exception)
        {
            report.AppendLine("FAIL: " + exception);
            Debug.LogException(exception);
        }
        File.WriteAllText("Library/SensorTorchSetupReport.txt", report.ToString());
    }

    private static bool IsSensorTorchName(string name)
    {
        return Enumerable.Range(1, 3).Any(number => name == "\u706b\u70ac" + number ||
            name == "\u706b\u70ae" + number || name == "Torch" + number);
    }

    private static string CheckIndependentTorches(params SensorTorch[] sources)
    {
        GameObject[] objects = new GameObject[sources.Length];
        try
        {
            SensorTorch[] copies = new SensorTorch[sources.Length];
            for (int i = 0; i < sources.Length; i++)
            {
                objects[i] = new GameObject("Torch independence check " + i);
                objects[i].hideFlags = HideFlags.HideAndDontSave;
                objects[i].transform.position = new Vector3(10000 + i * 100, 10000, 0);
                objects[i].AddComponent<SpriteRenderer>().sprite = sources[i].unlitSprite;
                SensorTorch copy = objects[i].AddComponent<SensorTorch>();
                copy.unlitSprite = sources[i].unlitSprite;
                copy.detectedSprite = sources[i].detectedSprite;
                copy.burningSprite = sources[i].burningSprite;
                copy.InitializeVisual();
                copy.RefreshVisual(true);
                copies[i] = copy;
            }
            Require(copies.All(t => !t.IsBurning && t.CurrentSprite == t.detectedSprite),
                "Selecting fire must light all sensors without igniting any torch.");
            for (int i = 0; i < copies.Length; i++)
            {
                foreach (SensorTorch copy in copies) copy.RefreshVisual(true);
                Require(copies[i].TryIgnite(), "Each torch must ignite independently.");
                for (int j = 0; j < copies.Length; j++)
                    Require(copies[j].IsBurning == (j <= i) && copies[j].CurrentSprite ==
                        (j <= i ? copies[j].burningSprite : copies[j].detectedSprite),
                        "Ignition must preserve every other torch's state.");
                foreach (SensorTorch copy in copies) copy.RefreshVisual(false);
                for (int j = 0; j < copies.Length; j++)
                    Require(copies[j].CurrentSprite == (j <= i ? copies[j].burningSprite : copies[j].unlitSprite),
                        "Deselecting fire must retain flames and turn off unlit sensors.");
            }
            return "PASS: " + copies.Length + " sensors detect fire; every torch ignites independently; burning persists after deselection.";
        }
        finally { foreach (GameObject item in objects) if (item != null) Object.DestroyImmediate(item); }
    }

    private static Sprite Import(string name)
    {
        string path = Folder + name + ".png";
        if (!File.Exists(path)) return null;
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            importer = AssetImporter.GetAtPath(path) as TextureImporter;
        }
        if (importer == null) return null;
        if (importer.userData != "SensorTorchV1")
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.spritePivot = new Vector2(0.5f, 0.5f);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spritePivot = new Vector2(0.5f, 0.5f);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.userData = "SensorTorchV1";
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static string CheckStates(Sprite off, Sprite detected, Sprite burning)
    {
        GameObject temp = new GameObject("Sensor Torch State Check");
        temp.hideFlags = HideFlags.HideAndDontSave;
        temp.transform.position = new Vector3(10000, 10000, 0);
        try
        {
            temp.AddComponent<SpriteRenderer>().sprite = off;
            SensorTorch torch = temp.AddComponent<SensorTorch>();
            torch.unlitSprite = off;
            torch.detectedSprite = detected;
            torch.burningSprite = burning;
            torch.InitializeVisual();
            torch.RefreshVisual(false);
            Require(torch.CurrentSprite == off, "Default state must be off.");
            torch.RefreshVisual(true);
            Require(torch.CurrentSprite == detected && !torch.IsBurning, "Fire selection must light only the sensor.");
            torch.RefreshVisual(false);
            Require(torch.CurrentSprite == off, "Deselecting fire must turn the sensor off.");
            BoxCollider2D target = torch.GetComponentInChildren<BoxCollider2D>();
            Physics2D.SyncTransforms();
            Bounds bounds = target.bounds;
            var hits = Physics2D.CircleCastAll(new Vector2(bounds.min.x - 0.2f, bounds.center.y),
                0.01f, Vector2.right, bounds.size.x + 0.4f);
            RaycastHit2D hit = hits.FirstOrDefault(h => h.collider == target);
            Require(hit.collider != null, "A swept fireball must hit the torch target.");
            Require(hit.collider.GetComponentInParent<SensorTorch>().TryIgnite(), "First hit must ignite.");
            Require(torch.CurrentSprite == burning, "Ignition must show the burning sprite.");
            torch.RefreshVisual(false);
            Require(torch.CurrentSprite == burning && torch.IsBurning, "Burning must persist after deselection.");
            Require(!torch.TryIgnite(), "Repeat hits must not reactivate a burning torch.");
            return "PASS: default, fire selection/deselection, swept hit ignition, persistent burning, repeat-hit guard.";
        }
        finally { Object.DestroyImmediate(temp); }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
