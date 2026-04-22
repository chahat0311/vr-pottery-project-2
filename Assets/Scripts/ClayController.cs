using UnityEngine;

public class ClayController : MonoBehaviour
{
    [Header("Shape Limits")]
    public float minHeight = 0.10f;
    public float maxHeight = 0.40f;
    public float minRadius = 0.06f;
    public float maxRadius = 0.24f;

    [Header("Starting Shape")]
    public float currentHeight = 0.18f;
    public float currentRadius = 0.14f;

    [Header("Feel")]
    public float shapeSpeed  = 3f;    // lower = more viscous/resistant
    public float resistance  = 0.85f; // 0=no resistance, 1=clay never moves

    private float targetHeight;
    private float targetRadius;
    private float smoothHeight;
    private float smoothRadius;
    private float baseVolume; // locked in at Start, never changes
    private Mesh  mesh;

    void Start()
    {
        transform.localRotation = Quaternion.identity;
        transform.localScale    = Vector3.one;

        targetHeight = currentHeight;
        targetRadius = currentRadius;
        smoothHeight = currentHeight;
        smoothRadius = currentRadius;

        // Lock the volume from the starting shape — never recalculate this
        baseVolume = Volume(currentRadius, currentHeight);

        mesh = new Mesh { name = "Clay" };
        GetComponent<MeshFilter>().sharedMesh = mesh;
        RebuildMesh();
    }

    void Update()
    {
        // Lerp toward target — shapeSpeed controls viscosity
        smoothHeight  = Mathf.Lerp(smoothHeight, targetHeight, Time.deltaTime * shapeSpeed);
        smoothRadius  = Mathf.Lerp(smoothRadius, targetRadius, Time.deltaTime * shapeSpeed);
        currentHeight = smoothHeight;
        currentRadius = smoothRadius;
        RebuildMesh();
    }

    // ── Public API ────────────────────────────────────────────────

    public void PullUp(float amount)
    {
        // Apply resistance — hand has to work harder to move the clay
        float force = amount * (1f - resistance);
        targetHeight = Mathf.Clamp(targetHeight + force, minHeight, maxHeight);
        // Conserve volume — radius must shrink to compensate
        targetRadius = Mathf.Clamp(RadiusForVolume(baseVolume, targetHeight), minRadius, maxRadius);
        // Re-solve height in case radius hit a clamp
        targetHeight = Mathf.Clamp(HeightForVolume(baseVolume, targetRadius), minHeight, maxHeight);
    }

    public void PressDown(float amount)
    {
        float force = amount * (1f - resistance);
        targetHeight = Mathf.Clamp(targetHeight - force, minHeight, maxHeight);
        // Conserve volume — radius spreads out
        targetRadius = Mathf.Clamp(RadiusForVolume(baseVolume, targetHeight), minRadius, maxRadius);
        targetHeight = Mathf.Clamp(HeightForVolume(baseVolume, targetRadius), minHeight, maxHeight);
    }

    public void PressInward(float amount)
    {
        float force = amount * (1f - resistance);
        targetRadius = Mathf.Clamp(targetRadius - force * 0.5f, minRadius, maxRadius);
        // Conserve volume — height rises when squeezed inward
        targetHeight = Mathf.Clamp(HeightForVolume(baseVolume, targetRadius), minHeight, maxHeight);
        targetRadius = Mathf.Clamp(RadiusForVolume(baseVolume, targetHeight), minRadius, maxRadius);
    }

    // ── Volume math (cylinder: V = π r² h) ───────────────────────

    float Volume(float r, float h)          => Mathf.PI * r * r * h;
    float RadiusForVolume(float vol, float h) => Mathf.Sqrt(vol / (Mathf.PI * Mathf.Max(h, 0.001f)));
    float HeightForVolume(float vol, float r) => vol / (Mathf.PI * Mathf.Max(r * r, 0.0001f));

    // ── Mesh ──────────────────────────────────────────────────────

    void RebuildMesh()
    {
        const int seg = 40;
        float r = smoothRadius;
        float h = smoothHeight;

        int rv    = seg + 1;
        int bRing = 0;
        int bCen  = rv;
        int tRing = bCen + 1;
        int tCen  = tRing + rv;
        int sBot  = tCen + 1;
        int sTop  = sBot + rv;

        Vector3[] v = new Vector3[sTop + rv];
        Vector2[] u = new Vector2[sTop + rv];

        for (int i = 0; i <= seg; i++)
        {
            float a  = i / (float)seg * Mathf.PI * 2f;
            float cx = Mathf.Cos(a);
            float cz = Mathf.Sin(a);
            float t  = i / (float)seg;

            v[bRing + i] = new Vector3(cx * r, 0, cz * r);
            v[tRing + i] = new Vector3(cx * r, h, cz * r);
            v[sBot  + i] = new Vector3(cx * r, 0, cz * r);
            v[sTop  + i] = new Vector3(cx * r, h, cz * r);

            u[bRing + i] = new Vector2(cx * .5f + .5f, cz * .5f + .5f);
            u[tRing + i] = new Vector2(cx * .5f + .5f, cz * .5f + .5f);
            u[sBot  + i] = new Vector2(t, 0);
            u[sTop  + i] = new Vector2(t, 1);
        }

        v[bCen] = Vector3.zero;
        v[tCen] = new Vector3(0, h, 0);
        u[bCen] = new Vector2(.5f, .5f);
        u[tCen] = new Vector2(.5f, .5f);

        int[] tris = new int[seg * 4 * 3];
        int ti = 0;

        for (int i = 0; i < seg; i++)
        {
            int n = i + 1;
            tris[ti++]=bCen;      tris[ti++]=bRing+i; tris[ti++]=bRing+n;
            tris[ti++]=tCen;      tris[ti++]=tRing+n; tris[ti++]=tRing+i;
            tris[ti++]=sBot+i;    tris[ti++]=sTop+i;  tris[ti++]=sBot+n;
            tris[ti++]=sBot+n;    tris[ti++]=sTop+i;  tris[ti++]=sTop+n;
        }

        mesh.Clear();
        mesh.vertices  = v;
        mesh.triangles = tris;
        mesh.uv        = u;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}