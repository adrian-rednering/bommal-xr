using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PresentationMeadowSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/2. PresentaionRoom.unity";
    private const string EnvironmentRootName = "PresentationMeadowEnvironment";
    private const string ClassroomRootName = "PresentationRoom_Showroom";
    private const string MaterialsFolder = "Assets/Materials/PresentationMeadow";

    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        DisableExistingClassroom();
        DeleteExistingRoot(EnvironmentRootName);

        EnsureFolder("Assets/Materials");
        EnsureFolder(MaterialsFolder);

        var grass = CreateMaterial("PM_Grass_Meadow", new Color(0.66f, 0.88f, 0.43f), 0.46f);
        var hillLight = CreateMaterial("PM_Hill_Light", new Color(0.76f, 0.91f, 0.50f), 0.52f);
        var hillMid = CreateMaterial("PM_Hill_Mid", new Color(0.54f, 0.79f, 0.39f), 0.48f);
        var skybox = CreateSkyboxMaterial("PM_Skybox_Meadow");
        var cloud = CreateMaterial("PM_Cloud_SoftWhite", new Color(1.0f, 0.95f, 0.98f), 0.36f);
        var trunk = CreateMaterial("PM_Tree_Trunk", new Color(0.58f, 0.36f, 0.17f), 0.38f);
        var leafGreen = CreateMaterial("PM_Tree_MintPuff", new Color(0.79f, 0.95f, 0.61f), 0.58f);
        var leafPink = CreateMaterial("PM_Tree_PeachPuff", new Color(1.0f, 0.68f, 0.64f), 0.56f);
        var leafBlue = CreateMaterial("PM_Tree_BluePuff", new Color(0.82f, 0.94f, 0.98f), 0.54f);
        var flowerWhite = CreateMaterial("PM_Flower_White", new Color(1.0f, 0.98f, 0.94f), 0.44f);
        var flowerPink = CreateMaterial("PM_Flower_Pink", new Color(1.0f, 0.62f, 0.82f), 0.44f);
        var flowerYellow = CreateMaterial("PM_Flower_Yellow", new Color(1.0f, 0.83f, 0.26f), 0.42f);
        var flowerCenter = CreateMaterial("PM_Flower_Center", new Color(1.0f, 0.74f, 0.18f), 0.38f);
        var pastelBall = CreateMaterial("PM_Pastel_Ball", new Color(0.97f, 0.90f, 1.0f), 0.4f);

        var root = new GameObject(EnvironmentRootName);
        root.transform.position = Vector3.zero;

        ConfigureTeleportFloor(grass);
        ConfigureSkybox(skybox);
        BuildRoundedMounds(root.transform, grass, hillLight, hillMid);
        BuildTrees(root.transform, trunk, leafGreen, leafPink, leafBlue);
        BuildClouds(root.transform, cloud);
        BuildFlowers(root.transform, flowerWhite, flowerPink, flowerYellow, flowerCenter);
        BuildPastelGroundDetails(root.transform, pastelBall, flowerWhite, flowerPink);
        BuildPresenterStart(root.transform, flowerYellow);
        TuneLighting();
        ConfigureSceneCameras();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("Presentation rounded meadow environment generated in " + ScenePath);
    }

    [MenuItem("Tools/Build Presentation Meadow Environment")]
    private static void BuildFromMenu()
    {
        Build();
    }

    private static void DisableExistingClassroom()
    {
        var classroomRoot = GameObject.Find(ClassroomRootName);
        if (classroomRoot != null)
        {
            classroomRoot.SetActive(false);
            EditorUtility.SetDirty(classroomRoot);
        }
    }

    private static void DeleteExistingRoot(string rootName)
    {
        var oldRoot = GameObject.Find(rootName);
        if (oldRoot != null)
        {
            Object.DestroyImmediate(oldRoot);
        }
    }

    private static void BuildRoundedMounds(Transform root, Material grass, Material hillLight, Material hillMid)
    {
        CreateSphere(root, "Main_Rounded_Dongsan", new Vector3(0.0f, -0.62f, 5.5f), new Vector3(18.0f, 1.55f, 14.0f), grass, false);
        CreateSphere(root, "Near_Left_Rounded_Dongsan", new Vector3(-6.6f, -0.58f, 2.8f), new Vector3(8.2f, 1.28f, 6.0f), hillLight, false);
        CreateSphere(root, "Near_Right_Rounded_Dongsan", new Vector3(6.4f, -0.6f, 3.2f), new Vector3(8.8f, 1.36f, 6.5f), hillMid, false);
        CreateSphere(root, "Mid_Left_Rounded_Dongsan", new Vector3(-8.8f, -0.82f, 9.2f), new Vector3(9.0f, 1.8f, 7.0f), hillMid, false);
        CreateSphere(root, "Mid_Right_Rounded_Dongsan", new Vector3(8.2f, -0.78f, 9.5f), new Vector3(9.6f, 1.9f, 7.2f), hillLight, false);
        CreateSphere(root, "Back_Center_Rounded_Dongsan", new Vector3(0.0f, -1.0f, 14.0f), new Vector3(15.2f, 2.15f, 7.2f), hillLight, false);
        CreateSphere(root, "Far_Left_Soft_Dongsan", new Vector3(-11.0f, -1.2f, 16.0f), new Vector3(8.8f, 2.0f, 5.6f), hillMid, false);
        CreateSphere(root, "Far_Right_Soft_Dongsan", new Vector3(11.0f, -1.16f, 15.8f), new Vector3(8.8f, 2.0f, 5.6f), hillMid, false);
    }

    private static void BuildTrees(Transform root, Material trunk, Material leafGreen, Material leafPink, Material leafBlue)
    {
        CreateCottonTree(root, "Left_Mint_Cotton_Tree", new Vector3(-8.6f, 0.12f, 6.2f), 1.18f, trunk, leafGreen);
        CreateCottonTree(root, "Left_Back_Blue_Cotton_Tree", new Vector3(-6.8f, 0.14f, 10.8f), 0.55f, trunk, leafBlue);
        CreateCottonTree(root, "Far_YellowGreen_Cotton_Tree", new Vector3(-2.8f, 0.08f, 13.4f), 0.42f, trunk, leafGreen);
        CreateCottonTree(root, "Right_Peach_Cotton_Tree", new Vector3(8.6f, 0.12f, 7.0f), 1.06f, trunk, leafPink);
        CreateCottonTree(root, "Right_Back_Mint_Cotton_Tree", new Vector3(5.8f, 0.12f, 12.4f), 0.48f, trunk, leafGreen);
        CreateCottonTree(root, "Center_Back_Peach_Cotton_Tree", new Vector3(2.6f, 0.08f, 14.3f), 0.36f, trunk, leafPink);
    }

    private static void CreateCottonTree(Transform root, string name, Vector3 basePosition, float size, Material trunk, Material leaves)
    {
        var treeRoot = new GameObject(name);
        treeRoot.transform.SetParent(root, false);
        treeRoot.transform.position = basePosition;

        var trunkObject = CreateCylinder(treeRoot.transform, "Trunk", new Vector3(0.0f, 0.75f * size, 0.0f), new Vector3(0.16f * size, 1.5f * size, 0.16f * size), trunk, true);
        trunkObject.transform.rotation = Quaternion.Euler(0.0f, 0.0f, -3.5f);

        CreateCylinder(treeRoot.transform, "Branch_Left", new Vector3(-0.22f * size, 1.45f * size, 0.0f), new Vector3(0.07f * size, 0.82f * size, 0.07f * size), trunk, true)
            .transform.rotation = Quaternion.Euler(0.0f, 0.0f, 36.0f);
        CreateCylinder(treeRoot.transform, "Branch_Right", new Vector3(0.25f * size, 1.5f * size, 0.0f), new Vector3(0.07f * size, 0.86f * size, 0.07f * size), trunk, true)
            .transform.rotation = Quaternion.Euler(0.0f, 0.0f, -38.0f);

        var puffOffsets = new[]
        {
            new Vector3(0.0f, 2.15f, 0.0f),
            new Vector3(-0.42f, 2.04f, -0.08f),
            new Vector3(0.48f, 2.02f, 0.08f),
            new Vector3(-0.2f, 2.45f, 0.04f),
            new Vector3(0.26f, 2.42f, -0.1f),
            new Vector3(0.04f, 1.82f, 0.08f)
        };

        for (var i = 0; i < puffOffsets.Length; i++)
        {
            var scale = (i == 0 ? 0.96f : 0.78f) * size;
            CreateSphere(treeRoot.transform, "Leaf_Puff_" + (i + 1), puffOffsets[i] * size, new Vector3(scale, scale, scale), leaves, false);
        }
    }

    private static void BuildClouds(Transform root, Material cloud)
    {
        CreateCloud(root, "Cloud_Left_Back", new Vector3(-5.5f, 6.0f, 15.0f), 0.85f, cloud);
        CreateCloud(root, "Cloud_Center_Back", new Vector3(1.6f, 5.4f, 16.2f), 0.58f, cloud);
        CreateCloud(root, "Cloud_Right_Back", new Vector3(6.8f, 6.4f, 13.8f), 0.72f, cloud);
    }

    private static void CreateCloud(Transform root, string name, Vector3 position, float size, Material cloud)
    {
        var cloudRoot = new GameObject(name);
        cloudRoot.transform.SetParent(root, false);
        cloudRoot.transform.position = position;

        var offsets = new[]
        {
            new Vector3(-0.72f, 0.0f, 0.0f),
            new Vector3(-0.25f, 0.14f, 0.0f),
            new Vector3(0.22f, 0.0f, 0.0f),
            new Vector3(0.68f, 0.08f, 0.0f)
        };

        for (var i = 0; i < offsets.Length; i++)
        {
            CreateSphere(cloudRoot.transform, "Cloud_Puff_" + (i + 1), offsets[i] * size, new Vector3(0.82f * size, 0.42f * size, 0.35f * size), cloud, false);
        }
    }

    private static void BuildFlowers(Transform root, Material white, Material pink, Material yellow, Material center)
    {
        var flowerData = new[]
        {
            new Vector3(-5.8f, 0.16f, 3.2f), new Vector3(-4.2f, 0.17f, 4.7f), new Vector3(-2.3f, 0.18f, 5.2f),
            new Vector3(-0.9f, 0.17f, 3.5f), new Vector3(1.2f, 0.17f, 4.2f), new Vector3(2.9f, 0.18f, 5.4f),
            new Vector3(4.7f, 0.16f, 3.8f), new Vector3(6.3f, 0.16f, 5.0f), new Vector3(-7.1f, 0.14f, 7.5f),
            new Vector3(7.2f, 0.14f, 7.6f), new Vector3(-3.4f, 0.16f, 8.0f), new Vector3(3.8f, 0.16f, 8.3f),
            new Vector3(-6.2f, 0.12f, 10.0f), new Vector3(6.0f, 0.12f, 10.3f), new Vector3(0.0f, 0.15f, 9.3f)
        };

        for (var i = 0; i < flowerData.Length; i++)
        {
            var petal = i % 3 == 0 ? pink : (i % 3 == 1 ? white : yellow);
            CreateDaisy(root, "Meadow_Flower_" + (i + 1), flowerData[i], 0.18f + (i % 4) * 0.025f, petal, center);
        }
    }

    private static void CreateDaisy(Transform root, string name, Vector3 position, float size, Material petal, Material center)
    {
        var flowerRoot = new GameObject(name);
        flowerRoot.transform.SetParent(root, false);
        flowerRoot.transform.position = position;

        CreateCylinder(flowerRoot.transform, "Stem", new Vector3(0.0f, size * 0.75f, 0.0f), new Vector3(size * 0.08f, size * 1.45f, size * 0.08f), CreateMaterial("PM_Flower_Stem", new Color(0.38f, 0.72f, 0.28f), 0.35f), false);

        for (var i = 0; i < 6; i++)
        {
            var angle = Mathf.PI * 2.0f * i / 6.0f;
            var offset = new Vector3(Mathf.Cos(angle) * size * 0.42f, size * 1.55f, Mathf.Sin(angle) * size * 0.42f);
            CreateSphere(flowerRoot.transform, "Petal_" + (i + 1), offset, new Vector3(size * 0.36f, size * 0.16f, size * 0.24f), petal, false);
        }

        CreateSphere(flowerRoot.transform, "Center", new Vector3(0.0f, size * 1.56f, 0.0f), new Vector3(size * 0.24f, size * 0.24f, size * 0.24f), center, false);
    }

    private static void BuildPastelGroundDetails(Transform root, Material ball, Material white, Material pink)
    {
        var positions = new[]
        {
            new Vector3(-5.0f, 0.28f, 6.3f), new Vector3(-7.4f, 0.26f, 4.5f), new Vector3(5.2f, 0.28f, 6.8f),
            new Vector3(7.4f, 0.26f, 4.7f), new Vector3(-1.8f, 0.22f, 7.2f), new Vector3(2.0f, 0.22f, 7.8f)
        };

        for (var i = 0; i < positions.Length; i++)
        {
            var material = i % 3 == 0 ? ball : (i % 3 == 1 ? white : pink);
            CreateSphere(root, "Pastel_Ground_Ball_" + (i + 1), positions[i], new Vector3(0.36f, 0.36f, 0.36f), material, true);
        }
    }

    private static void BuildPresenterStart(Transform root, Material marker)
    {
        var start = new GameObject("PresenterStartPosition");
        start.transform.SetParent(root, false);
        start.transform.position = new Vector3(0.0f, 0.18f, 1.4f);
        start.transform.rotation = Quaternion.identity;

        CreateCylinder(root, "PresenterStart_Flower_Marker", new Vector3(0.0f, 0.17f, 1.4f), new Vector3(0.78f, 0.02f, 0.78f), marker, false);
    }

    private static void ConfigureTeleportFloor(Material grass)
    {
        var floor = GameObject.Find("PresentationRoom_TeleportFloor");
        if (floor == null)
        {
            floor = GameObject.Find("Plane");
        }

        if (floor == null)
        {
            floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "PresentationRoom_TeleportFloor";
        }

        floor.SetActive(true);
        floor.transform.position = new Vector3(0.0f, -0.02f, 6.2f);
        floor.transform.rotation = Quaternion.identity;
        floor.transform.localScale = new Vector3(3.2f, 1.0f, 2.7f);

        var renderer = floor.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = grass;
            renderer.enabled = false;
        }

        EditorUtility.SetDirty(floor);
    }

    private static void TuneLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.82f, 0.88f, 0.94f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.76f, 0.87f, 1.0f);
        RenderSettings.fogDensity = 0.009f;

        var directionalLight = FindDirectionalLight();
        if (directionalLight == null)
        {
            var lightObject = new GameObject("Directional Light");
            directionalLight = lightObject.AddComponent<Light>();
            directionalLight.type = LightType.Directional;
        }

        directionalLight.name = "Directional Light";
        directionalLight.color = new Color(1.0f, 0.88f, 0.68f);
        directionalLight.intensity = 1.35f;
        directionalLight.shadows = LightShadows.Soft;
        directionalLight.transform.position = new Vector3(0.0f, 4.0f, 0.0f);
        directionalLight.transform.rotation = Quaternion.Euler(45.0f, -32.0f, 0.0f);
        EditorUtility.SetDirty(directionalLight);
    }

    private static void ConfigureSkybox(Material skybox)
    {
        if (skybox == null)
        {
            return;
        }

        RenderSettings.skybox = skybox;
        EditorUtility.SetDirty(skybox);
    }

    private static Light FindDirectionalLight()
    {
        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.type == LightType.Directional)
            {
                return light;
            }
        }

        return null;
    }

    private static void ConfigureSceneCameras()
    {
        foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            camera.clearFlags = CameraClearFlags.Skybox;
            EditorUtility.SetDirty(camera);
        }
    }

    private static GameObject CreateCube(Transform parent, string name, Vector3 position, Vector3 scale, Material material, Vector3? euler = null, bool keepCollider = true)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = position;
        cube.transform.localRotation = Quaternion.Euler(euler ?? Vector3.zero);
        cube.transform.localScale = scale;
        AssignMaterial(cube, material);
        RemoveColliderIfNeeded(cube, keepCollider);
        return cube;
    }

    private static GameObject CreateSphere(Transform parent, string name, Vector3 position, Vector3 scale, Material material, bool keepCollider)
    {
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        sphere.transform.SetParent(parent, false);
        sphere.transform.localPosition = position;
        sphere.transform.localRotation = Quaternion.identity;
        sphere.transform.localScale = scale;
        AssignMaterial(sphere, material);
        RemoveColliderIfNeeded(sphere, keepCollider);
        return sphere;
    }

    private static GameObject CreateCylinder(Transform parent, string name, Vector3 position, Vector3 scale, Material material, bool keepCollider)
    {
        var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = name;
        cylinder.transform.SetParent(parent, false);
        cylinder.transform.localPosition = position;
        cylinder.transform.localRotation = Quaternion.identity;
        cylinder.transform.localScale = scale;
        AssignMaterial(cylinder, material);
        RemoveColliderIfNeeded(cylinder, keepCollider);
        return cylinder;
    }

    private static void AssignMaterial(GameObject target, Material material)
    {
        var renderer = target.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }
    }

    private static void RemoveColliderIfNeeded(GameObject target, bool keepCollider)
    {
        if (keepCollider)
        {
            return;
        }

        var collider = target.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }
    }

    private static Material CreateMaterial(string name, Color baseColor, float smoothness)
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
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateSkyboxMaterial(string name)
    {
        var path = MaterialsFolder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            var shader = Shader.Find("Skybox/Procedural");
            if (shader == null)
            {
                shader = Shader.Find("Skybox/Panoramic");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.name = name;
        SetColorIfPresent(material, "_SkyTint", new Color(0.56f, 0.80f, 1.0f));
        SetColorIfPresent(material, "_GroundColor", new Color(0.92f, 0.92f, 1.0f));
        SetFloatIfPresent(material, "_AtmosphereThickness", 0.95f);
        SetFloatIfPresent(material, "_Exposure", 1.08f);
        SetFloatIfPresent(material, "_SunSize", 0.05f);
        SetFloatIfPresent(material, "_SunSizeConvergence", 5.0f);
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
