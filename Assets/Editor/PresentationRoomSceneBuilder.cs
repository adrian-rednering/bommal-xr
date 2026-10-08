using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PresentationRoomSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/2. PresentaionRoom.unity";
    private const string RootName = "PresentationRoom_Showroom";
    private const string MaterialsFolder = "Assets/Materials/PresentationRoom";

    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var oldRoot = GameObject.Find(RootName);
        if (oldRoot != null)
        {
            Object.DestroyImmediate(oldRoot);
        }

        EnsureFolder("Assets/Materials");
        EnsureFolder(MaterialsFolder);

        var wood = CreateMaterial("PR_WarmWood", new Color(0.78f, 0.53f, 0.29f), 0.38f);
        var stageWood = CreateMaterial("PR_StageWood", new Color(0.63f, 0.37f, 0.16f), 0.34f);
        var wall = CreateMaterial("PR_BrightWall", new Color(0.92f, 0.89f, 0.82f), 0.42f);
        var screen = CreateMaterial("PR_ScreenSurface", new Color(0.06f, 0.07f, 0.08f), 0.22f);
        var screenFrame = CreateMaterial("PR_MatteBlackFrame", new Color(0.015f, 0.015f, 0.018f), 0.16f);
        var seat = CreateMaterial("PR_SeatWarmGray", new Color(0.42f, 0.48f, 0.54f), 0.45f);
        var aisle = CreateMaterial("PR_AisleSoftTan", new Color(0.70f, 0.62f, 0.50f), 0.28f);
        var marker = CreateMaterial("PR_PresenterMarker", new Color(1.0f, 0.78f, 0.28f), 0.5f, new Color(1.0f, 0.45f, 0.08f));
        var lightGlow = CreateMaterial("PR_WarmLightGlow", new Color(1.0f, 0.82f, 0.47f), 0.15f, new Color(1.0f, 0.55f, 0.15f));

        var root = new GameObject(RootName);
        root.transform.position = Vector3.zero;

        ConfigureExistingTeleportFloor(wood);

        BuildRoomShell(root.transform, wall, wood);
        BuildStage(root.transform, stageWood, wood, screen, screenFrame, marker);
        BuildLectureSeating(root.transform, wood, seat, aisle);
        BuildLighting(root.transform, lightGlow);
        BuildPresenterStart(root.transform, marker);
        TuneDirectionalLight();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Presentation room showroom generated in " + ScenePath);
    }

    [MenuItem("Tools/Build Presentation Room Showroom")]
    private static void BuildFromMenu()
    {
        Build();
    }

    private static void BuildRoomShell(Transform root, Material wall, Material wood)
    {
        CreateCube(root, "Left_Wall", new Vector3(-8.2f, 2.0f, 4.4f), new Vector3(0.22f, 4.0f, 17.6f), wall);
        CreateCube(root, "Right_Wall", new Vector3(8.2f, 2.0f, 4.4f), new Vector3(0.22f, 4.0f, 17.6f), wall);
        CreateCube(root, "Back_Wall", new Vector3(0.0f, 2.0f, 13.1f), new Vector3(16.6f, 4.0f, 0.22f), wall);
        CreateCube(root, "Front_Screen_Wall", new Vector3(0.0f, 2.0f, -4.2f), new Vector3(16.6f, 4.0f, 0.22f), wall);
        CreateCube(root, "Ceiling_Panel", new Vector3(0.0f, 4.08f, 4.45f), new Vector3(16.6f, 0.18f, 17.7f), wall);

        for (var i = 0; i < 6; i++)
        {
            var z = -2.8f + i * 3.0f;
            CreateCube(root, "Left_Wood_AcousticPanel_" + (i + 1), new Vector3(-8.05f, 1.85f, z), new Vector3(0.12f, 1.7f, 1.4f), wood);
            CreateCube(root, "Right_Wood_AcousticPanel_" + (i + 1), new Vector3(8.05f, 1.85f, z), new Vector3(0.12f, 1.7f, 1.4f), wood);
        }

        for (var i = 0; i < 5; i++)
        {
            var x = -6.4f + i * 3.2f;
            CreateCube(root, "Ceiling_Wood_Beam_" + (i + 1), new Vector3(x, 3.92f, 4.45f), new Vector3(0.18f, 0.24f, 17.0f), wood);
        }

        CreateCube(root, "Back_Wall_Wood_Rail", new Vector3(0.0f, 1.1f, 12.96f), new Vector3(15.0f, 0.18f, 0.16f), wood);
    }

    private static void BuildStage(Transform root, Material stageWood, Material wood, Material screen, Material screenFrame, Material marker)
    {
        CreateCube(root, "Front_Presentation_Stage", new Vector3(0.0f, 0.2f, -0.85f), new Vector3(9.8f, 0.4f, 3.3f), stageWood);
        CreateCube(root, "Stage_Front_Lip", new Vector3(0.0f, 0.45f, 0.88f), new Vector3(9.9f, 0.14f, 0.18f), wood);
        CreateCube(root, "Stage_Left_Step", new Vector3(-5.1f, 0.12f, 0.6f), new Vector3(1.2f, 0.24f, 0.75f), stageWood);
        CreateCube(root, "Stage_Right_Step", new Vector3(5.1f, 0.12f, 0.6f), new Vector3(1.2f, 0.24f, 0.75f), stageWood);

        CreateCube(root, "Podium_Base", new Vector3(0.0f, 0.88f, 0.35f), new Vector3(0.9f, 1.15f, 0.55f), wood);
        CreateCube(root, "Podium_Top", new Vector3(0.0f, 1.48f, 0.28f), new Vector3(1.2f, 0.16f, 0.72f), wood, new Vector3(-7.0f, 0.0f, 0.0f));
        CreateCube(root, "Podium_Front_Panel", new Vector3(0.0f, 0.95f, 0.66f), new Vector3(0.72f, 0.62f, 0.05f), marker);

        CreateCube(root, "Large_Screen_Frame", new Vector3(0.0f, 2.25f, -4.02f), new Vector3(6.55f, 2.65f, 0.12f), screenFrame);
        CreateCube(root, "Large_Presentation_Screen", new Vector3(0.0f, 2.25f, -3.94f), new Vector3(5.95f, 2.12f, 0.08f), screen);
        CreateCube(root, "Screen_Bottom_Wood_Trim", new Vector3(0.0f, 0.86f, -3.89f), new Vector3(6.8f, 0.16f, 0.16f), wood);
    }

    private static void BuildLectureSeating(Transform root, Material wood, Material seat, Material aisle)
    {
        CreateCube(root, "Front_Aisle_Clear_Zone", new Vector3(0.0f, 0.03f, 2.0f), new Vector3(13.2f, 0.06f, 1.25f), aisle);

        var seatXs = new[] { -5.6f, -4.35f, -3.1f, -1.85f, 1.85f, 3.1f, 4.35f, 5.6f };

        for (var row = 0; row < 5; row++)
        {
            var z = 3.35f + row * 1.55f;
            var platformY = 0.12f + row * 0.28f;
            var seatY = platformY + 0.28f;

            CreateCube(root, "Tier_Platform_Row_" + (row + 1), new Vector3(0.0f, platformY, z), new Vector3(13.6f, 0.24f, 1.36f), wood);
            CreateCube(root, "Tier_Front_Riser_Row_" + (row + 1), new Vector3(0.0f, platformY + 0.12f, z - 0.75f), new Vector3(13.6f, 0.24f + row * 0.12f, 0.12f), wood);

            foreach (var x in seatXs)
            {
                var side = x < 0.0f ? "L" : "R";
                var index = Mathf.RoundToInt(Mathf.Abs(x) * 10.0f);
                var prefix = "Seat_R" + (row + 1) + "_" + side + "_" + index;
                CreateCube(root, prefix + "_Cushion", new Vector3(x, seatY, z - 0.05f), new Vector3(0.88f, 0.24f, 0.68f), seat);
                CreateCube(root, prefix + "_Back", new Vector3(x, seatY + 0.38f, z + 0.32f), new Vector3(0.88f, 0.74f, 0.16f), seat, new Vector3(-7.0f, 0.0f, 0.0f));
            }
        }

        CreateCube(root, "Center_Aisle_Path", new Vector3(0.0f, 0.08f, 6.45f), new Vector3(1.45f, 0.08f, 7.6f), aisle);
        CreateCube(root, "Left_Side_Aisle", new Vector3(-7.15f, 0.07f, 6.5f), new Vector3(0.7f, 0.08f, 8.4f), aisle);
        CreateCube(root, "Right_Side_Aisle", new Vector3(7.15f, 0.07f, 6.5f), new Vector3(0.7f, 0.08f, 8.4f), aisle);
    }

    private static void BuildLighting(Transform root, Material lightGlow)
    {
        var pendantPositions = new[]
        {
            new Vector3(-4.2f, 3.65f, -0.4f),
            new Vector3(4.2f, 3.65f, -0.4f),
            new Vector3(-4.2f, 3.65f, 5.2f),
            new Vector3(4.2f, 3.65f, 5.2f),
            new Vector3(0.0f, 3.65f, 9.6f)
        };

        for (var i = 0; i < pendantPositions.Length; i++)
        {
            var position = pendantPositions[i];
            CreateCube(root, "Warm_Light_Panel_" + (i + 1), position, new Vector3(1.15f, 0.08f, 0.55f), lightGlow);
            CreateLight(root, "Warm_Point_Light_" + (i + 1), position + Vector3.down * 0.35f, LightType.Point, new Color(1.0f, 0.72f, 0.42f), 2.2f, 6.0f);
        }

        var spot = CreateLight(root, "Stage_Podium_Spot_Light", new Vector3(0.0f, 3.25f, 2.15f), LightType.Spot, new Color(1.0f, 0.78f, 0.52f), 3.1f, 8.0f);
        spot.transform.rotation = Quaternion.Euler(62.0f, 180.0f, 0.0f);
        var light = spot.GetComponent<Light>();
        light.spotAngle = 42.0f;
        light.innerSpotAngle = 18.0f;
    }

    private static void BuildPresenterStart(Transform root, Material marker)
    {
        var start = new GameObject("PresenterStartPosition");
        start.transform.SetParent(root, false);
        start.transform.position = new Vector3(0.0f, 0.42f, -0.05f);
        start.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);

        CreateCube(root, "PresenterStart_Floor_Marker", new Vector3(0.0f, 0.42f, -0.05f), new Vector3(0.78f, 0.025f, 0.78f), marker);
    }

    private static void ConfigureExistingTeleportFloor(Material floorMaterial)
    {
        var floor = GameObject.Find("PresentationRoom_TeleportFloor");
        if (floor == null)
        {
            floor = GameObject.Find("Plane");
        }

        if (floor == null)
        {
            return;
        }

        floor.name = "PresentationRoom_TeleportFloor";
        floor.transform.position = new Vector3(0.0f, 0.0f, 4.45f);
        floor.transform.rotation = Quaternion.identity;
        floor.transform.localScale = new Vector3(1.66f, 1.0f, 1.78f);

        var renderer = floor.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = floorMaterial;
        }
    }

    private static GameObject CreateCube(Transform parent, string name, Vector3 position, Vector3 scale, Material material, Vector3? euler = null)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.position = position;
        cube.transform.rotation = Quaternion.Euler(euler ?? Vector3.zero);
        cube.transform.localScale = scale;

        var renderer = cube.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }

        return cube;
    }

    private static GameObject CreateLight(Transform parent, string name, Vector3 position, LightType type, Color color, float intensity, float range)
    {
        var lightObject = new GameObject(name);
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.position = position;

        var light = lightObject.AddComponent<Light>();
        light.type = type;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.Soft;

        return lightObject;
    }

    private static void TuneDirectionalLight()
    {
        var directionalLight = Object.FindFirstObjectByType<Light>();
        if (directionalLight == null || directionalLight.type != LightType.Directional)
        {
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional)
                {
                    directionalLight = light;
                    break;
                }
            }
        }

        if (directionalLight == null)
        {
            var lightObject = new GameObject("Directional Light");
            directionalLight = lightObject.AddComponent<Light>();
            directionalLight.type = LightType.Directional;
        }

        directionalLight.name = "Directional Light";
        directionalLight.color = new Color(1.0f, 0.84f, 0.62f);
        directionalLight.intensity = 1.45f;
        directionalLight.transform.position = new Vector3(0.0f, 3.0f, 0.0f);
        directionalLight.transform.rotation = Quaternion.Euler(48.0f, -35.0f, 0.0f);
    }

    private static Material CreateMaterial(string name, Color baseColor, float smoothness, Color? emission = null)
    {
        var path = MaterialsFolder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.name = name;
        SetColorIfPresent(material, "_BaseColor", baseColor);
        SetColorIfPresent(material, "_Color", baseColor);
        SetFloatIfPresent(material, "_Smoothness", smoothness);

        if (emission.HasValue)
        {
            material.EnableKeyword("_EMISSION");
            SetColorIfPresent(material, "_EmissionColor", emission.Value);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else
        {
            material.DisableKeyword("_EMISSION");
            SetColorIfPresent(material, "_EmissionColor", Color.black);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void SetColorIfPresent(Material material, string property, Color value)
    {
        if (material.HasProperty(property))
        {
            material.SetColor(property, value);
        }
    }

    private static void SetFloatIfPresent(Material material, string property, float value)
    {
        if (material.HasProperty(property))
        {
            material.SetFloat(property, value);
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        var folder = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(folder))
        {
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
