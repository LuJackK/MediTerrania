using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter))]
public sealed class DynamicSeaSurface : MonoBehaviour
{
    private const int MaxSegments = 180;
    private const float TwoPi = Mathf.PI * 2f;

    [Header("Generated surface")]
    [SerializeField, Range(8, MaxSegments)] private int segments = 96;
    [SerializeField, Min(1f)] private float localSize = 10f;
    [SerializeField] private bool updateMeshCollider;

    [Header("Primary swell")]
    [SerializeField, Min(0f)] private float swellAmplitude = 0.055f;
    [SerializeField, Min(0.01f)] private float swellWavelength = 1.7f;
    [SerializeField] private float swellSpeed = 0.42f;
    [SerializeField, Range(0f, 360f)] private float swellDirection = 18f;

    [Header("Cross waves")]
    [SerializeField, Min(0f)] private float crossAmplitude = 0.022f;
    [SerializeField, Min(0.01f)] private float crossWavelength = 0.65f;
    [SerializeField] private float crossSpeed = 0.88f;
    [SerializeField, Range(0f, 360f)] private float crossDirection = 128f;

    [Header("Surface ripples")]
    [SerializeField, Min(0f)] private float rippleAmplitude = 0.007f;
    [SerializeField, Min(0.01f)] private float rippleWavelength = 0.19f;
    [SerializeField] private float rippleSpeed = 1.85f;
    [SerializeField, Range(0f, 360f)] private float rippleDirection = 72f;
    [SerializeField, Range(0f, 1f)] private float choppiness = 0.16f;

    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private Mesh originalMesh;
    private Mesh originalColliderMesh;
    private Mesh generatedMesh;
    private Vector3[] baseVertices;
    private Vector3[] animatedVertices;
    private int builtSegments;
    private float builtLocalSize;

    private void Awake()
    {
        CacheComponents();
        BuildMesh();
    }

    private void OnEnable()
    {
        CacheComponents();
        BuildMesh();
    }

    private void OnValidate()
    {
        segments = Mathf.Clamp(segments, 8, MaxSegments);
        localSize = Mathf.Max(1f, localSize);
        swellWavelength = Mathf.Max(0.01f, swellWavelength);
        crossWavelength = Mathf.Max(0.01f, crossWavelength);
        rippleWavelength = Mathf.Max(0.01f, rippleWavelength);
    }

    private void Update()
    {
        if (generatedMesh == null || builtSegments != segments || !Mathf.Approximately(builtLocalSize, localSize))
        {
            BuildMesh();
        }

        AnimateSurface(Time.time);
    }

    private void OnDisable()
    {
        if (meshFilter != null && originalMesh != null)
        {
            meshFilter.sharedMesh = originalMesh;
        }

        if (meshCollider != null)
        {
            meshCollider.sharedMesh = originalColliderMesh;
        }

        DestroyGeneratedMesh();
    }

    private void CacheComponents()
    {
        if (meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();
            originalMesh = meshFilter.sharedMesh;
        }

        if (meshCollider == null)
        {
            meshCollider = GetComponent<MeshCollider>();
            if (meshCollider != null)
            {
                originalColliderMesh = meshCollider.sharedMesh;
            }
        }
    }

    private void BuildMesh()
    {
        CacheComponents();

        if (generatedMesh != null && builtSegments == segments && Mathf.Approximately(builtLocalSize, localSize))
        {
            return;
        }

        DestroyGeneratedMesh();

        int rowLength = segments + 1;
        int vertexCount = rowLength * rowLength;
        baseVertices = new Vector3[vertexCount];
        animatedVertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        int[] triangles = new int[segments * segments * 6];
        float halfSize = localSize * 0.5f;
        float step = localSize / segments;

        for (int z = 0; z < rowLength; z++)
        {
            for (int x = 0; x < rowLength; x++)
            {
                int index = z * rowLength + x;
                float localX = -halfSize + x * step;
                float localZ = -halfSize + z * step;
                baseVertices[index] = new Vector3(localX, 0f, localZ);
                animatedVertices[index] = baseVertices[index];
                uvs[index] = new Vector2((float)x / segments, (float)z / segments);
            }
        }

        int triangleIndex = 0;
        for (int z = 0; z < segments; z++)
        {
            for (int x = 0; x < segments; x++)
            {
                int index = z * rowLength + x;
                triangles[triangleIndex++] = index;
                triangles[triangleIndex++] = index + rowLength;
                triangles[triangleIndex++] = index + 1;
                triangles[triangleIndex++] = index + 1;
                triangles[triangleIndex++] = index + rowLength;
                triangles[triangleIndex++] = index + rowLength + 1;
            }
        }

        generatedMesh = new Mesh
        {
            name = "Dynamic Sea Surface Mesh",
            indexFormat = vertexCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16
        };
        generatedMesh.MarkDynamic();
        generatedMesh.vertices = animatedVertices;
        generatedMesh.uv = uvs;
        generatedMesh.triangles = triangles;
        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateBounds();

        meshFilter.sharedMesh = generatedMesh;
        if (updateMeshCollider && meshCollider != null)
        {
            meshCollider.sharedMesh = generatedMesh;
        }

        builtSegments = segments;
        builtLocalSize = localSize;
    }

    private void AnimateSurface(float time)
    {
        if (generatedMesh == null || baseVertices == null || animatedVertices == null)
        {
            return;
        }

        Vector2 swell = DirectionFromDegrees(swellDirection);
        Vector2 cross = DirectionFromDegrees(crossDirection);
        Vector2 ripple = DirectionFromDegrees(rippleDirection);

        for (int i = 0; i < baseVertices.Length; i++)
        {
            Vector3 vertex = baseVertices[i];
            Vector2 horizontal = new Vector2(vertex.x, vertex.z);
            Vector2 chop = Vector2.zero;

            float height = SampleWave(horizontal, swell, swellAmplitude, swellWavelength, swellSpeed, time, ref chop);
            height += SampleWave(horizontal, cross, crossAmplitude, crossWavelength, crossSpeed, time, ref chop);
            height += SampleWave(horizontal, ripple, rippleAmplitude, rippleWavelength, rippleSpeed, time, ref chop);

            animatedVertices[i] = new Vector3(vertex.x + chop.x, height, vertex.z + chop.y);
        }

        generatedMesh.vertices = animatedVertices;
        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateBounds();

        if (updateMeshCollider && meshCollider != null)
        {
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = generatedMesh;
        }
    }

    private float SampleWave(
        Vector2 position,
        Vector2 direction,
        float amplitude,
        float wavelength,
        float speed,
        float time,
        ref Vector2 chop)
    {
        if (amplitude <= 0f)
        {
            return 0f;
        }

        float phase = Vector2.Dot(position, direction) * TwoPi / wavelength + time * speed;
        float wave = Mathf.Sin(phase) * amplitude;
        chop += direction * (Mathf.Cos(phase) * amplitude * choppiness);
        return wave;
    }

    private static Vector2 DirectionFromDegrees(float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)).normalized;
    }

    private void DestroyGeneratedMesh()
    {
        if (generatedMesh == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(generatedMesh);
        }
        else
        {
            DestroyImmediate(generatedMesh);
        }

        generatedMesh = null;
    }
}
