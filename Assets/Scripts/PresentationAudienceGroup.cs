using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public sealed class PresentationAudienceGroup : MonoBehaviour
{
    private const string GeneratedPrefix = "_Audience_";

    [Header("Layout")]
    [SerializeField, Min(1)] private int rowCount = 5;
    [SerializeField, Min(1)] private int seatsPerSide = 4;
    [SerializeField] private float firstSeatX = 1.85f;
    [SerializeField] private float seatSpacingX = 1.25f;
    [SerializeField] private float firstRowZ = 3.35f;
    [SerializeField] private float rowSpacingZ = 1.55f;
    [SerializeField] private float firstSeatY = 0.4f;
    [SerializeField] private float rowRiseY = 0.28f;
    [SerializeField, Range(0f, 1f)] private float occupancy = 0.82f;

    [Header("Shape")]
    [SerializeField] private Vector3 bodyScale = new Vector3(0.34f, 0.46f, 0.22f);
    [SerializeField] private Vector3 headScale = new Vector3(0.21f, 0.25f, 0.2f);
    [SerializeField] private Vector3 hairScale = new Vector3(0.23f, 0.07f, 0.2f);

    [Header("Materials")]
    [SerializeField] private Material[] clothingMaterials;
    [SerializeField] private Material[] skinMaterials;
    [SerializeField] private Material hairMaterial;

#if UNITY_EDITOR
    private bool rebuildQueued;
#endif

    private void Awake()
    {
        if (Application.isPlaying)
        {
            RebuildAudience();
        }
    }

    private void OnEnable()
    {
        QueueRebuild();
    }

    private void OnValidate()
    {
        QueueRebuild();
    }

    [ContextMenu("Rebuild Audience")]
    public void RebuildAudience()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && PrefabUtility.IsPartOfPrefabAsset(gameObject))
        {
            return;
        }
#endif

        ClearGeneratedAudience();

        int visualIndex = 0;
        for (int row = 0; row < rowCount; row++)
        {
            float seatY = firstSeatY + row * rowRiseY;
            float seatZ = firstRowZ + row * rowSpacingZ;

            for (int seat = 0; seat < seatsPerSide; seat++)
            {
                float leftX = -(firstSeatX + (seatsPerSide - 1 - seat) * seatSpacingX);
                CreateAudienceMember(leftX, seatY, seatZ, row, seat, "L", visualIndex++);
            }

            for (int seat = 0; seat < seatsPerSide; seat++)
            {
                float rightX = firstSeatX + seat * seatSpacingX;
                CreateAudienceMember(rightX, seatY, seatZ, row, seat, "R", visualIndex++);
            }
        }
    }

    private void QueueRebuild()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (PrefabUtility.IsPartOfPrefabAsset(gameObject) || rebuildQueued)
            {
                return;
            }

            rebuildQueued = true;
            EditorApplication.delayCall += () =>
            {
                if (this == null)
                {
                    return;
                }

                rebuildQueued = false;
                RebuildAudience();
            };
            return;
        }
#endif

        RebuildAudience();
    }

    private void CreateAudienceMember(
        float x,
        float seatY,
        float z,
        int row,
        int seat,
        string side,
        int visualIndex
    )
    {
        if (!ShouldOccupySeat(visualIndex))
        {
            return;
        }

        GameObject member = new GameObject($"{GeneratedPrefix}R{row + 1}_{side}_{seat + 1}");
        member.layer = gameObject.layer;
        MarkGeneratedObject(member);
        member.transform.SetParent(transform, false);
        member.transform.localPosition = new Vector3(x, seatY + 0.12f, z - 0.07f);
        member.transform.localRotation = Quaternion.Euler(0f, AudienceYaw(visualIndex), 0f);
        member.transform.localScale = Vector3.one;

        Material clothing = PickMaterial(clothingMaterials, visualIndex);
        Material skin = PickMaterial(skinMaterials, visualIndex + row);
        float heightScale = 0.94f + (visualIndex % 4) * 0.035f;
        float widthScale = 0.94f + (visualIndex % 3) * 0.035f;
        Vector3 scaledBody = new Vector3(
            bodyScale.x * widthScale,
            bodyScale.y * heightScale,
            bodyScale.z
        );
        Vector3 scaledHead = new Vector3(
            headScale.x * widthScale,
            headScale.y * heightScale,
            headScale.z
        );

        CreatePart(
            member.transform,
            "Torso",
            PrimitiveType.Cube,
            new Vector3(0f, 0.38f, -0.05f),
            scaledBody,
            clothing
        );
        CreatePart(
            member.transform,
            "Shoulders",
            PrimitiveType.Cube,
            new Vector3(0f, 0.62f, -0.05f),
            new Vector3(0.56f * widthScale, 0.13f, 0.23f),
            clothing
        );
        CreatePart(
            member.transform,
            "Lap",
            PrimitiveType.Cube,
            new Vector3(0f, 0.15f, -0.11f),
            new Vector3(0.42f * widthScale, 0.11f, 0.33f),
            clothing
        );
        CreatePart(
            member.transform,
            "LeftArm",
            PrimitiveType.Cube,
            new Vector3(-0.29f * widthScale, 0.4f, -0.05f),
            new Vector3(0.08f, 0.34f, 0.1f),
            clothing,
            new Vector3(0f, 0f, -8f)
        );
        CreatePart(
            member.transform,
            "RightArm",
            PrimitiveType.Cube,
            new Vector3(0.29f * widthScale, 0.4f, -0.05f),
            new Vector3(0.08f, 0.34f, 0.1f),
            clothing,
            new Vector3(0f, 0f, 8f)
        );
        CreatePart(
            member.transform,
            "LeftHand",
            PrimitiveType.Cube,
            new Vector3(-0.23f * widthScale, 0.2f, -0.15f),
            new Vector3(0.08f, 0.05f, 0.08f),
            skin
        );
        CreatePart(
            member.transform,
            "RightHand",
            PrimitiveType.Cube,
            new Vector3(0.23f * widthScale, 0.2f, -0.15f),
            new Vector3(0.08f, 0.05f, 0.08f),
            skin
        );
        CreatePart(
            member.transform,
            "Neck",
            PrimitiveType.Cube,
            new Vector3(0f, 0.73f, -0.04f),
            new Vector3(0.11f, 0.13f, 0.1f),
            skin
        );
        CreatePart(
            member.transform,
            "Head",
            PrimitiveType.Sphere,
            new Vector3(0f, 0.89f, -0.04f),
            scaledHead,
            skin
        );
        CreatePart(
            member.transform,
            "HairCap",
            PrimitiveType.Cube,
            new Vector3(0f, 1.02f, -0.045f),
            new Vector3(hairScale.x * widthScale, hairScale.y, hairScale.z),
            hairMaterial
        );
        CreatePart(
            member.transform,
            "HairBack",
            PrimitiveType.Cube,
            new Vector3(0f, 0.92f, 0.07f),
            new Vector3(0.22f * widthScale, 0.16f, 0.05f),
            hairMaterial
        );
    }

    private bool ShouldOccupySeat(int visualIndex)
    {
        if (occupancy >= 0.995f)
        {
            return true;
        }

        if (occupancy <= 0.005f)
        {
            return false;
        }

        int deterministicPercent = (visualIndex * 37 + 17) % 100;
        return deterministicPercent < Mathf.RoundToInt(occupancy * 100f);
    }

    private static float AudienceYaw(int visualIndex)
    {
        return (visualIndex % 5 - 2) * 3f;
    }

    private void CreatePart(
        Transform parent,
        string partName,
        PrimitiveType primitiveType,
        Vector3 localPosition,
        Vector3 localScale,
        Material material,
        Vector3 localEulerAngles = default
    )
    {
        GameObject part = GameObject.CreatePrimitive(primitiveType);
        part.name = partName;
        part.layer = gameObject.layer;
        MarkGeneratedObject(part);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.Euler(localEulerAngles);
        part.transform.localScale = localScale;

        if (part.TryGetComponent(out Renderer renderer) && material != null)
        {
            renderer.sharedMaterial = material;
        }

        if (part.TryGetComponent(out Collider partCollider))
        {
            DestroyGeneratedObject(partCollider);
        }
    }

    private void ClearGeneratedAudience()
    {
        for (int index = transform.childCount - 1; index >= 0; index--)
        {
            Transform child = transform.GetChild(index);
            if (child.name.StartsWith(GeneratedPrefix, StringComparison.Ordinal))
            {
                DestroyGeneratedObject(child.gameObject);
            }
        }
    }

    private static Material PickMaterial(Material[] materials, int index)
    {
        if (materials == null || materials.Length == 0)
        {
            return null;
        }

        int materialIndex = Mathf.Abs(index) % materials.Length;
        return materials[materialIndex];
    }

    private static void MarkGeneratedObject(GameObject generatedObject)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            generatedObject.hideFlags = HideFlags.DontSaveInEditor;
        }
#endif
    }

    private static void DestroyGeneratedObject(UnityEngine.Object target)
    {
        if (target == null)
        {
            return;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(target);
            return;
        }
#endif

        Destroy(target);
    }
}
