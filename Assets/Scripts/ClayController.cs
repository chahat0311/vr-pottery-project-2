using UnityEngine;

public class ClayController : MonoBehaviour
{
    [Header("Shape Limits")]
    public float minHeight    = 0.10f;
    public float maxHeight    = 0.40f;
    public float minRadius    = 0.06f;
    public float maxRadius    = 0.24f;
    public float maxHoleRadius = 0.10f; // hole can't be wider than wall

    [Header("Starting Shape")]
    public float currentHeight = 0.18f;
    public float currentRadius = 0.14f;

    [Header("Feel")]
    public float shapeSpeed = 4f;
    public float resistance = 0.45f;

    // internal targets
    private float targetHeight;
    private float targetRadius;
    private float targetHoleRadius = 0f; // 0 = no hole
    private float smoothHeight;
    private float smoothRadius;
    private float smoothHole;
    private float baseVolume;
    private Mesh  mesh;

    void Start()
    {
        transform.localRotation = Quaternion.identity;
        transform.localScale    = Vector3.one;

        targetHeight     = currentHeight;
        targetRadius     = currentRadius;
        smoothHeight     = currentHeight;
        smoothRadius     = currentRadius;
        smoothHole       = 0f;
        baseVolume       = Vol(currentRadius, currentHeight);

        mesh = new Mesh { name = "Clay" };
        GetComponent<MeshFilter>().sharedMesh = mesh;
        RebuildMesh();
    }

    void Update()
    {
        smoothHeight = Mathf.Lerp(smoothHeight, targetHeight,     Time.deltaTime * shapeSpeed);
        smoothRadius = Mathf.Lerp(smoothRadius, targetRadius,     Time.deltaTime * shapeSpeed);
        smoothHole   = Mathf.Lerp(smoothHole,   targetHoleRadius, Time.deltaTime * shapeSpeed);
        currentHeight = smoothHeight;
        currentRadius = smoothRadius;
        RebuildMesh();
    }

    // ── Public API ────────────────────────────────────────────────

    public void PullUp(float amount)
    {
        float f      = amount * (1f - resistance);
        targetHeight = Mathf.Clamp(targetHeight + f, minHeight, maxHeight);
        targetRadius = Mathf.Clamp(R(baseVolume, targetHeight), minRadius, maxRadius);
        targetHeight = Mathf.Clamp(H(baseVolume, targetRadius), minHeight, maxHeight);
    }

    public void PressDown(float amount)
    {
        float f      = amount * (1f - resistance);
        targetHeight = Mathf.Clamp(targetHeight - f, minHeight, maxHeight);
        targetRadius = Mathf.Clamp(R(baseVolume, targetHeight), minRadius, maxRadius);
        targetHeight = Mathf.Clamp(H(baseVolume, targetRadius), minHeight, maxHeight);
    }

    public void PressInward(float amount)
    {
        float f      = amount * (1f - resistance);
        targetRadius = Mathf.Clamp(targetRadius - f * 0.5f, minRadius, maxRadius);
        targetHeight = Mathf.Clamp(H(baseVolume, targetRadius), minHeight, maxHeight);
        targetRadius = Mathf.Clamp(R(baseVolume, targetHeight), minRadius, maxRadius);
    }

    // Push thumb DOWN into top of clay to open a hole
    public void PushHole(float amount)
    {
        float f          = amount * (1f - resistance);
        // hole grows, but can never reach the outer wall
        targetHoleRadius = Mathf.Clamp(
            targetHoleRadius + f,
            0f,
            Mathf.Min(maxHoleRadius, targetRadius - 0.02f));
    }

    // ── Volume helpers ────────────────────────────────────────────
    float Vol(float r, float h) => Mathf.PI * r * r * h;
    float R(float v, float h)   => Mathf.Sqrt(v / (Mathf.PI * Mathf.Max(h, 0.001f)));
    float H(float v, float r)   => v / (Mathf.PI * Mathf.Max(r * r, 0.0001f));

    // ── Mesh ──────────────────────────────────────────────────────
    // Builds a hollow pot:
    //   outer wall, inner wall (if hole > 0), bottom solid disc,
    //   top rim ring connecting outer to inner edge
    void RebuildMesh()
    {
        const int seg  = 40;
        float outer    = smoothRadius;
        float hole     = smoothHole;          // inner radius (0 = solid top)
        float h        = smoothHeight;
        bool  hasHole  = hole > 0.005f;
        float inner    = hasHole ? hole : 0f;

        // ── vertex rings ─────────────────────────────────────────
        // 0: outer bottom   1: outer top
        // 2: inner bottom   3: inner top   (only if hasHole)
        // side rings duplicated for hard normals
        // sBot/sTop: outer side   iBot/iTop: inner side

        int rv   = seg + 1;
        int obR  = 0;            // outer bottom ring
        int otR  = rv;           // outer top ring
        int ibR  = rv * 2;       // inner bottom ring
        int itR  = rv * 3;       // inner top ring
        int sbR  = rv * 4;       // side outer bottom
        int stR  = rv * 5;       // side outer top
        int siB  = rv * 6;       // side inner bottom
        int siT  = rv * 7;       // side inner top
        int bCen = rv * 8;       // bottom centre (solid base)
        int total = bCen + 1;

        Vector3[] v = new Vector3[total];
        Vector2[] u = new Vector2[total];

        for (int i = 0; i <= seg; i++)
        {
            float a  = i / (float)seg * Mathf.PI * 2f;
            float cx = Mathf.Cos(a), cz = Mathf.Sin(a);
            float t  = i / (float)seg;

            // cap rings (flat normals via disc UVs)
            v[obR + i] = new Vector3(cx * outer, 0,  cz * outer);
            v[otR + i] = new Vector3(cx * outer, h,  cz * outer);
            v[ibR + i] = new Vector3(cx * inner, 0,  cz * inner);
            v[itR + i] = new Vector3(cx * inner, h,  cz * inner);
            u[obR + i] = new Vector2(cx * .5f + .5f, cz * .5f + .5f);
            u[otR + i] = new Vector2(cx * .5f + .5f, cz * .5f + .5f);
            u[ibR + i] = new Vector2(cx * .5f + .5f, cz * .5f + .5f);
            u[itR + i] = new Vector2(cx * .5f + .5f, cz * .5f + .5f);

            // side rings (cylindrical UVs)
            v[sbR + i] = new Vector3(cx * outer, 0, cz * outer);
            v[stR + i] = new Vector3(cx * outer, h, cz * outer);
            v[siB + i] = new Vector3(cx * inner, 0, cz * inner);
            v[siT + i] = new Vector3(cx * inner, h, cz * inner);
            u[sbR + i] = new Vector2(t, 0);
            u[stR + i] = new Vector2(t, 1);
            u[siB + i] = new Vector2(t, 0);
            u[siT + i] = new Vector2(t, 1);
        }

        v[bCen] = Vector3.zero;
        u[bCen] = new Vector2(.5f, .5f);

        // ── triangles ────────────────────────────────────────────
        // faces: outer side(2) + bottom(2 or 1) + top rim(2) + inner side(2 if hole)
        int faceCount = hasHole ? 8 : 4;
        int[] tris = new int[seg * faceCount * 3];
        int ti = 0;

        for (int i = 0; i < seg; i++)
        {
            int n = i + 1;

            // Outer side wall
            tris[ti++]=sbR+i; tris[ti++]=stR+i; tris[ti++]=sbR+n;
            tris[ti++]=sbR+n; tris[ti++]=stR+i; tris[ti++]=stR+n;

            if (hasHole)
            {
                // Inner side wall (reversed winding — faces inward)
                tris[ti++]=siB+i; tris[ti++]=siB+n; tris[ti++]=siT+i;
                tris[ti++]=siB+n; tris[ti++]=siT+n; tris[ti++]=siT+i;

                // Bottom ring (annulus: outer base to inner base)
                tris[ti++]=obR+i; tris[ti++]=ibR+n; tris[ti++]=obR+n;
                tris[ti++]=obR+i; tris[ti++]=ibR+i; tris[ti++]=ibR+n;

                // Top rim (annulus: outer top to inner top — the "wall edge")
                tris[ti++]=otR+i; tris[ti++]=otR+n; tris[ti++]=itR+i;
                tris[ti++]=otR+n; tris[ti++]=itR+n; tris[ti++]=itR+i;
            }
            else
            {
                // Solid bottom disc
                tris[ti++]=bCen;   tris[ti++]=obR+i; tris[ti++]=obR+n;
                // Solid top disc
                tris[ti++]=bCen;   tris[ti++]=otR+n; tris[ti++]=otR+i;
                // reuse bCen as a stand-in top centre (Y=0 is wrong but
                // we override with a proper top centre below)
            }
        }

        // Fix: if no hole, add proper top disc with correct centre
        if (!hasHole)
        {
            // Recalculate — solid top needs its own centre vertex
            // We already used bCen(Y=0) for bottom, so rebuild tris properly
            // by reserving an extra vertex. Since we already allocated the
            // array, we patch the top disc tris written above to point at
            // the correct Y=h centre. We'll just move bCen to Y=0 and
            // write a second pass for the top. Array is already the right
            // size so patch inline:
            v[bCen] = Vector3.zero; // bottom centre stays at Y=0
            // patch top disc: we wrote tris[ti - seg*3 .. ti] using bCen
            // but bCen.y=0 is wrong for top. Simplest fix: add a top
            // centre vert by extending array — but array is fixed size.
            // Instead: top disc uses the outer top ring wound to a cone
            // toward ring centre. Already correct if we treat it as a fan
            // from a virtual centre at v[bCen] displaced to h — swap Y:
            // Actually easiest: just leave top as-is and shift bCen.y
            // to h/2 (average). Not perfect but invisible on a solid lump.
            // REAL fix: allocate +1 vert. Let's do that properly below.
        }

        // ── clean rebuild with proper top centre for solid case ───
        if (!hasHole)
        {
            // Re-do with 2 centre verts
            int bC2 = rv * 8;      // bottom centre Y=0
            int tC2 = rv * 8 + 1;  // top centre    Y=h
            Vector3[] v2 = new Vector3[rv * 8 + 2];
            Vector2[] u2 = new Vector2[rv * 8 + 2];
            System.Array.Copy(v, v2, rv * 8);
            System.Array.Copy(u, u2, rv * 8);
            v2[bC2] = Vector3.zero;        u2[bC2] = new Vector2(.5f,.5f);
            v2[tC2] = new Vector3(0,h,0);  u2[tC2] = new Vector2(.5f,.5f);

            int[] t2 = new int[seg * 4 * 3];
            int ti2  = 0;
            for (int i = 0; i < seg; i++)
            {
                int n = i + 1;
                // outer side
                t2[ti2++]=sbR+i; t2[ti2++]=stR+i; t2[ti2++]=sbR+n;
                t2[ti2++]=sbR+n; t2[ti2++]=stR+i; t2[ti2++]=stR+n;
                // bottom disc
                t2[ti2++]=bC2;   t2[ti2++]=obR+i; t2[ti2++]=obR+n;
                // top disc
                t2[ti2++]=tC2;   t2[ti2++]=otR+n; t2[ti2++]=otR+i;
            }

            mesh.Clear();
            mesh.vertices  = v2;
            mesh.triangles = t2;
            mesh.uv        = u2;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return;
        }

        mesh.Clear();
        mesh.vertices  = v;
        mesh.triangles = tris;
        mesh.uv        = u;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}