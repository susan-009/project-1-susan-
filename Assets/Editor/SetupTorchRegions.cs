using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class SetupTorchRegions
{
    static SetupTorchRegions()
    {
        EditorApplication.delayCall += Install;
        EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += Install;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += Install;
        };
    }

    [MenuItem("Tools/Torches/Set Up First Lighting Region")]
    public static void Install()
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
            SensorTorch torch = Object.FindObjectsOfType<SensorTorch>().FirstOrDefault(t => IsFirst(t.name));
            if (torch == null) return;
            Physics2D.SyncTransforms();
            if (torch.lightRegion == null)
            {
                Collider2D left = FindCollider("wall 1");
                Collider2D right = FindCollider("Stone Wall A");
                Collider2D floor = FindCollider("new ground");
                Collider2D ceiling = Object.FindObjectsOfType<BoxCollider2D>()
                    .Where(c => c.name == "wall3" && c.bounds.size.x > 10f)
                    .OrderByDescending(c => c.bounds.center.y).FirstOrDefault();
                Require(left != null && right != null && floor != null && ceiling != null,
                    "Could not find the first area's scene boundaries.");
                Vector2 min = new Vector2(left.bounds.min.x - 0.02f, floor.bounds.min.y - 0.05f);
                Vector2 max = new Vector2(right.bounds.max.x + 0.02f, ceiling.bounds.max.y + 0.05f);
                CreateRegion(torch, (min + max) * 0.5f, max - min);
            }
            TorchLightRegion region = torch.lightRegion;
            report.AppendLine(torch.name + " -> " + region.name);
            report.AppendLine("Center=" + region.transform.position + "; Size=" + region.regionSize);
            report.AppendLine("Light=" + region.RegionLight.lightType + "; starts enabled=" + region.RegionLight.enabled);
            Require(region.RegionLight.lightType == Light2D.LightType.Freeform, "Region must use a shaped light.");
            var layers = new SerializedObject(region.RegionLight).FindProperty("m_ApplyToSortingLayers");
            Require(layers != null && layers.arraySize > 0, "Region needs target sprite sorting layers.");
            TorchLightRegion prison = EnsurePrisonRegion(torch.gameObject.scene);
            report.AppendLine(prison.name + ": startsLit=" + prison.startsLit +
                "; Center=" + prison.transform.position + "; Size=" + prison.regionSize);
            GameObject rabbit = GameObject.Find("animal");
            Require(rabbit == null || prison.ContainsWorldPoint(rabbit.transform.position),
                "Initial lighting must include the rabbit's prison area.");
            SensorTorch second = Object.FindObjectsOfType<SensorTorch>().FirstOrDefault(t =>
            {
                string name = t.name.Trim().Replace(" ", "");
                return name == "\u706b\u70ac2" || name == "\u706b\u70ae2" || name == "Torch2";
            });
            if (second != null)
            {
                EnsureSecondRegion(second, region);
                TorchLightRegion upper = second.lightRegion;
                Require(upper != region && upper != prison, "Each torch needs its own light region.");
                Require(upper.ContainsWorldPoint(second.transform.position), "The upper region must include torch 2.");
                report.AppendLine(second.name + " -> " + upper.name + "; startsLit=" + upper.startsLit +
                    "; Center=" + upper.transform.position + "; Size=" + upper.regionSize);
            }
            SensorTorch third = Object.FindObjectsOfType<SensorTorch>().FirstOrDefault(t =>
            {
                string name = t.name.Trim().Replace(" ", "");
                return name == "\u706b\u70ac3" || name == "\u706b\u70ae3" || name == "Torch3";
            });
            if (third != null)
            {
                EnsureThirdRegion(third);
                TorchLightRegion lowerRight = third.lightRegion;
                Require(lowerRight != region && lowerRight != prison &&
                    (second == null || lowerRight != second.lightRegion), "Torch 3 needs its own light region.");
                Require(!lowerRight.startsLit && lowerRight.ContainsWorldPoint(third.transform.position),
                    "The right region must start dark and include torch 3.");
                Require(lowerRight.RegionLight.lightType == Light2D.LightType.Freeform,
                    "The right region needs a shaped light.");
                report.AppendLine(third.name + " -> " + lowerRight.name + "; startsLit=" + lowerRight.startsLit +
                    "; Center=" + lowerRight.transform.position + "; Size=" + lowerRight.regionSize);
                if (!SessionState.GetBool("TorchRegions.ThirdChecksV1", false))
                {
                    string result = CheckIndependentRegions(third);
                    File.WriteAllText("Library/Torch3RegionStateChecks.txt", DateTime.Now.ToString("s") + "\n" + result);
                    SessionState.SetBool("TorchRegions.ThirdChecksV1", true);
                }
            }
            if (!SessionState.GetBool("TorchRegions.ChecksV2", false))
            {
                string result = CheckIndependentRegions(torch);
                File.WriteAllText("Library/TorchRegionStateChecks.txt", DateTime.Now.ToString("s") + "\n" + result);
                SessionState.SetBool("TorchRegions.ChecksV2", true);
            }
            report.AppendLine("PASS connection and light configuration.");
        }
        catch (Exception exception)
        {
            report.AppendLine("FAIL: " + exception);
            Debug.LogException(exception);
        }
        File.WriteAllText("Library/TorchRegionSetupReport.txt", report.ToString());
    }

    [MenuItem("Tools/Torches/Create Lighting Region For Selected Torch")]
    private static void CreateForSelected()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || Selection.activeGameObject == null) return;
        SensorTorch torch = Selection.activeGameObject.GetComponentInParent<SensorTorch>();
        if (torch == null) return;
        CreateRegion(torch, torch.transform.position + Vector3.up * 2f, new Vector2(6f, 6f));
        Selection.activeGameObject = torch.lightRegion.gameObject;
    }

    private static void CreateRegion(SensorTorch torch, Vector2 center, Vector2 size)
    {
        var go = new GameObject(torch.name + " Light Region");
        Undo.RegisterCreatedObjectUndo(go, "Create torch lighting region");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, torch.gameObject.scene);
        go.transform.position = new Vector3(center.x, center.y, 0f);
        TorchLightRegion region = Undo.AddComponent<TorchLightRegion>(go);
        region.regionSize = size;
        region.ConfigureLight();
        ConfigureSortingLayers(region.RegionLight);
        Undo.RecordObject(torch, "Connect torch lighting region");
        torch.lightRegion = region;
        EditorUtility.SetDirty(torch);
        EditorUtility.SetDirty(region);
        EditorSceneManager.MarkSceneDirty(torch.gameObject.scene);
    }

    private static void EnsureSecondRegion(SensorTorch second, TorchLightRegion firstRegion)
    {
        if (second.lightRegion != null && second.lightRegion != firstRegion) return;
        Collider2D left = FindCollider("Stone Wall A");
        Collider2D right = FindCollider("wall 2");
        Collider2D floor = FindCollider("Land4");
        Collider2D ceiling = Object.FindObjectsOfType<BoxCollider2D>()
            .Where(c => c.name == "wall3" && c.bounds.size.x > 10f)
            .OrderByDescending(c => c.bounds.center.y).FirstOrDefault();
        Require(left != null && right != null && floor != null && ceiling != null,
            "Upper laser-stage boundaries were not found.");
        Vector2 min = new Vector2(left.bounds.min.x - 0.05f, floor.bounds.min.y - 0.03f);
        Vector2 max = new Vector2(right.bounds.max.x + 0.02f, ceiling.bounds.max.y + 0.05f);
        CreateRegion(second, (min + max) * 0.5f, max - min);
    }

    private static void EnsureThirdRegion(SensorTorch third)
    {
        if (third.lightRegion != null) return;
        Collider2D left = Object.FindObjectsOfType<BoxCollider2D>()
            .Where(c => c.name == "wall3" && c.bounds.size.x < 1f && c.bounds.size.y > 1f)
            .OrderBy(c => Mathf.Abs(c.bounds.center.x)).FirstOrDefault();
        Collider2D right = FindCollider("wall 2");
        Collider2D floor = FindCollider("new ground");
        Collider2D top = FindCollider("Land4");
        Require(left != null && right != null && floor != null && top != null,
            "Right-side lighting boundaries were not found.");
        Vector2 min = new Vector2(left.bounds.min.x - 0.05f, floor.bounds.min.y - 0.05f);
        Vector2 max = new Vector2(right.bounds.max.x + 0.02f, top.bounds.min.y + 0.03f);
        CreateRegion(third, (min + max) * 0.5f, max - min);
    }

    private static void ConfigureSortingLayers(Light2D light)
    {
        var settings = new SerializedObject(light);
        var layers = settings.FindProperty("m_ApplyToSortingLayers");
        int[] ids = SortingLayer.layers.Select(l => l.id).ToArray();
        layers.arraySize = ids.Length;
        for (int i = 0; i < ids.Length; i++) layers.GetArrayElementAtIndex(i).intValue = ids[i];
        settings.ApplyModifiedProperties();
    }

    private static TorchLightRegion EnsurePrisonRegion(UnityEngine.SceneManagement.Scene scene)
    {
        const string name = "Prison Initial Light Region";
        GameObject existing = GameObject.Find(name);
        if (existing != null && existing.TryGetComponent(out TorchLightRegion configured)) return configured;
        Collider2D left = FindCollider("MountainSlope");
        Collider2D right = FindCollider("CaveDoor");
        Collider2D bottom = FindCollider("MountainBottom");
        Collider2D top = FindCollider("MountainTop");
        Require(left != null && right != null && bottom != null && top != null,
            "Prison architecture boundaries were not found.");
        Vector2 min = new Vector2(left.bounds.min.x - 0.05f, bottom.bounds.min.y - 0.05f);
        Vector2 max = new Vector2(right.bounds.max.x + 0.05f, top.bounds.max.y + 0.05f);
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Add initially illuminated prison area");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.position = (min + max) * 0.5f;
        TorchLightRegion region = Undo.AddComponent<TorchLightRegion>(go);
        region.regionSize = max - min;
        region.startsLit = true;
        region.ConfigureLight();
        ConfigureSortingLayers(region.RegionLight);
        EditorUtility.SetDirty(region);
        EditorSceneManager.MarkSceneDirty(scene);
        return region;
    }

    private static bool IsFirst(string name)
    {
        string clean = name.Trim().Replace(" ", "");
        return clean == "\u706b\u70ac1" || clean == "\u706b\u70ae1" || clean == "Torch1";
    }

    private static Collider2D FindCollider(string name)
    {
        GameObject go = GameObject.Find(name);
        return go != null ? go.GetComponent<Collider2D>() : null;
    }

    private static string CheckIndependentRegions(SensorTorch source)
    {
        GameObject a = new GameObject("Region A Check");
        GameObject b = new GameObject("Region B Check");
        GameObject t = new GameObject("Torch Region Check");
        GameObject initial = new GameObject("Initially Lit Area Check");
        a.hideFlags = b.hideFlags = t.hideFlags = initial.hideFlags = HideFlags.HideAndDontSave;
        a.transform.position = new Vector3(10000, 10000, 0);
        b.transform.position = new Vector3(10100, 10000, 0);
        t.transform.position = a.transform.position;
        try
        {
            TorchLightRegion first = a.AddComponent<TorchLightRegion>();
            TorchLightRegion second = b.AddComponent<TorchLightRegion>();
            first.ConfigureLight(); second.ConfigureLight();
            TorchLightRegion litAtStart = initial.AddComponent<TorchLightRegion>();
            litAtStart.startsLit = true;
            typeof(TorchLightRegion).GetMethod("Awake", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic).Invoke(litAtStart, null);
            Require(litAtStart.IsLit && litAtStart.RegionLight.enabled,
                "The prison's startsLit flag must enable light at startup.");
            Require(!first.IsLit && !second.IsLit && !first.RegionLight.enabled && !second.RegionLight.enabled,
                "Unlit areas must start dark.");
            t.AddComponent<SpriteRenderer>().sprite = source.unlitSprite;
            SensorTorch torch = t.AddComponent<SensorTorch>();
            torch.unlitSprite = source.unlitSprite;
            torch.detectedSprite = source.detectedSprite;
            torch.burningSprite = source.burningSprite;
            torch.lightRegion = first;
            torch.InitializeVisual();
            torch.RefreshVisual(true);
            Require(!first.IsLit && !first.RegionLight.enabled, "Sensor detection must not illuminate the area.");
            Physics2D.SyncTransforms();
            BoxCollider2D target = torch.GetComponentInChildren<BoxCollider2D>();
            Bounds bounds = target.bounds;
            RaycastHit2D hit = Physics2D.CircleCastAll(new Vector2(bounds.min.x - 0.2f, bounds.center.y),
                0.01f, Vector2.right, bounds.size.x + 0.4f).FirstOrDefault(h => h.collider == target);
            Require(hit.collider != null && hit.collider.GetComponentInParent<SensorTorch>().TryIgnite(),
                "A fireball hit must ignite the torch.");
            Require(first.IsLit && first.RegionLight.enabled && first.RegionLight.intensity == first.litIntensity,
                "Ignition must illuminate the linked area.");
            Require(!second.IsLit && !second.RegionLight.enabled, "Other areas must remain dark.");
            Require(litAtStart.IsLit && litAtStart.RegionLight.enabled,
                "Torch activation must leave the initially bright area illuminated.");
            torch.RefreshVisual(false);
            Require(first.IsLit && first.RegionLight.enabled && !torch.TryIgnite(), "The first area must stay lit.");
            Vector2 center = first.transform.position;
            Require(first.ContainsWorldPoint(center + Vector2.up * (first.regionSize.y * 0.49f)) &&
                first.ContainsWorldPoint(center - Vector2.up * (first.regionSize.y * 0.49f)),
                "The region must cover both ceiling and floor.");
            Require(!first.ContainsWorldPoint(center + Vector2.right * first.regionSize.x),
                "The area must have a finite boundary.");
            return "PASS V2: prison starts lit; torch regions start dark; selection only lights sensor; swept hit lights region A; region B stays dark; illumination persists; full-height bounded shape.";
        }
        finally { Object.DestroyImmediate(t); Object.DestroyImmediate(a); Object.DestroyImmediate(b); Object.DestroyImmediate(initial); }
    }

    private static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
