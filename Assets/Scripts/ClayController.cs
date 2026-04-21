using UnityEngine;

/// <summary>
/// Ring-based clay simulation.
/// The clay is modelled as a vertical stack of rings, each with its own radius.
/// Mass (volume) is conserved across all operations.
/// Deformation only applies when a hand is actively in contact.
/// All public API names match the original script so the rest of your system needs no changes.
/// </summary>
public class ClayController : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // Inspector – kept identical to original
    // ─────────────────────────────────────────────

    [Header("Clay Shape Limits")]
    public float minHeight = 0.08f;
    public float maxHeight = 0.35f;
    public float minRadius = 0.06f;
    public float maxRadius = 0.22f;

    [Header("Current Shape")]
    public float currentHeight = 0.14f;
    public float currentRadius = 0.18f;   // represents the average / base radius

    [Header("Deform Speed")]
    public float shapeSpeed = 5f;          // lerp speed toward target (viscosity)

    [Header("Clay Physics Feel")]
    public float volume = 1f;             // conserved mass constant

    [Header("Ring Profile (advanced)")]
    [Range(6, 24)]
    public int ringCount = 12;            // number of vertical slices

    [Tooltip("How many rings either side of the contact point are affected")]
    [Range(1, 6)]
    public int influenceRadius = 3;

    [Tooltip("How quickly the clay relaxes/smooths between rings (0 = instant, 1 = never)")]
    [Range(0f, 0.98f)]
    public float viscosity = 0.72f;

    // ─────────────────────────────────────────────
    // Internal state
    // ─────────────────────────────────────────────

    // Each element is the normalised radius of that ring (0–1, scaled by maxRadius at mesh time)
    private float[] ringRadii;
    private float[] targetRadii;

    // The ring heights sum to currentHeight; each ring has equal share
    private float ringHeight => currentHeight / ringCount;

    // Is a hand currently touching?
    private bool isBeingTouched = false;

    // Mesh components
    private Mesh clayMesh;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    // Cached for Update lerp
    private float targetHeight;
    private float smoothedHeight;

    // ─────────────────────────────────────────────
    // Unity lifecycle
    // ─────────────────────────────────────────────

    void Start()
    {
        meshFilter   = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();

        // Initialise rings to a uniform cylinder
        ringRadii  = new float[ringCount];
        targetRadii = new float[ringCount];

        float normRadius = currentRadius / maxRadius;
        for (int i = 0; i < ringCount; i++)
        {
            ringRadii[i]  = normRadius;
            targetRadii[i] = normRadius;
        }

        smoothedHeight = currentHeight;
        targetHeight   = currentHeight;

        // Set volume from initial shape so it's consistent
        volume = EstimateVolume();

        BuildMesh();
    }

    void Update()
    {
        // Smooth height toward target (viscous feel)
        smoothedHeight = Mathf.Lerp(smoothedHeight, targetHeight, Time.deltaTime * shapeSpeed);
        currentHeight  = smoothedHeight;

        // Smooth each ring radius toward its target
        bool meshDirty = false;
        for (int i = 0; i < ringCount; i++)
        {
            float newR = Mathf.Lerp(ringRadii[i], targetRadii[i], Time.deltaTime * shapeSpeed);
            if (Mathf.Abs(newR - ringRadii[i]) > 0.0001f)
            {
                ringRadii[i] = newR;
                meshDirty = true;
            }
        }

        // Derive currentRadius as the average ring radius (for external callers / inspector)
        currentRadius = AverageRadius();

        if (meshDirty || Mathf.Abs(smoothedHeight - targetHeight) > 0.0001f)
            BuildMesh();

        // Reset touch flag each frame — ClayShapingController must call PressInward/etc every frame it's active
        isBeingTouched = false;
    }

    // ─────────────────────────────────────────────
    // Public API  (identical signatures to original)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Call this every frame a hand is pressing inward at a given normalised height (0=base, 1=top).
    /// contactHeightNorm is optional; omit to affect the mid-band (matches old single-value behaviour).
    /// </summary>
    public void PressInward(float amount, float contactHeightNorm = 0.5f)
    {
        isBeingTouched = true;

        int centerRing = Mathf.RoundToInt(contactHeightNorm * (ringCount - 1));

        // Push inward — radius decreases around contact
        AffectRings(centerRing, -amount * 0.5f);

        // Mass conservation: height must compensate
        float newHeight = ConserveVolume();
        targetHeight = Mathf.Clamp(newHeight, minHeight, maxHeight);

        ApplyClampsToRings();
    }

    /// <summary>
    /// Call this every frame a hand is pressing down from the top.
    /// </summary>
    public void PressDown(float amount, float contactHeightNorm = 1f)
    {
        isBeingTouched = true;

        // Squash height
        targetHeight = Mathf.Clamp(targetHeight - amount * 0.5f, minHeight, maxHeight);

        // Clay spreads outward — affect rings near the top
        int centerRing = Mathf.RoundToInt(contactHeightNorm * (ringCount - 1));
        AffectRings(centerRing, amount * 0.3f);

        ApplyClampsToRings();
        ConserveVolumeByScalingRadii(); // keep mass honest
    }

    /// <summary>
    /// Call this every frame a hand is pulling upward.
    /// </summary>
    public void PullUp(float amount, float contactHeightNorm = 0.5f)
    {
        isBeingTouched = true;

        // Stretch height
        targetHeight = Mathf.Clamp(targetHeight + amount * 0.6f, minHeight, maxHeight);

        // Narrow the middle band
        int centerRing = Mathf.RoundToInt(contactHeightNorm * (ringCount - 1));
        AffectRings(centerRing, -amount * 0.2f);

        ApplyClampsToRings();
        ConserveVolumeByScalingRadii();
    }

    // ─────────────────────────────────────────────
    // Ring deformation helpers
    // ─────────────────────────────────────────────

    /// <summary>
    /// Applies a gaussian-weighted delta to rings around centerRing.
    /// </summary>
    private void AffectRings(int centerRing, float delta)
    {
        for (int i = 0; i < ringCount; i++)
        {
            int dist = Mathf.Abs(i - centerRing);
            if (dist > influenceRadius) continue;

            // Gaussian falloff so the effect is smooth, not boxy
            float weight = Mathf.Exp(-0.5f * (dist * dist) / (influenceRadius * 0.5f + 0.5f));
            targetRadii[i] = targetRadii[i] + delta * weight;
        }
    }

    private void ApplyClampsToRings()
    {
        float normMin = minRadius / maxRadius;
        float normMax = 1f;

        for (int i = 0; i < ringCount; i++)
            targetRadii[i] = Mathf.Clamp(targetRadii[i], normMin, normMax);
    }

    // ─────────────────────────────────────────────
    // Volume / mass conservation
    // ─────────────────────────────────────────────

    private float EstimateVolume()
    {
        // V ≈ sum of (π * r² * h) per ring, simplified to sum(r²) * ringHeight
        float sum = 0f;
        for (int i = 0; i < ringCount; i++)
        {
            float r = targetRadii[i] * maxRadius;
            sum += r * r;
        }
        return sum * ringHeight * Mathf.PI;
    }

    /// <summary>
    /// After an inward press, solve for the height that preserves volume.
    /// Returns the new target height.
    /// </summary>
    private float ConserveVolume()
    {
        float sumRsq = 0f;
        for (int i = 0; i < ringCount; i++)
        {
            float r = targetRadii[i] * maxRadius;
            sumRsq += r * r;
        }

        if (sumRsq < 0.0001f) return targetHeight;

        // V = sumRsq * (H/ringCount) * π  →  H = V * ringCount / (sumRsq * π)
        float newH = volume * ringCount / (sumRsq * Mathf.PI);
        return newH;
    }

    /// <summary>
    /// After a push-down or pull-up (height changed), scale all radii to preserve volume.
    /// </summary>
    private void ConserveVolumeByScalingRadii()
    {
        float currentVol = EstimateVolume();
        if (currentVol < 0.0001f) return;

        float scale = Mathf.Sqrt(volume / currentVol); // r² scaling → sqrt
        for (int i = 0; i < ringCount; i++)
            targetRadii[i] = Mathf.Clamp(targetRadii[i] * scale, minRadius / maxRadius, 1f);
    }

    private float AverageRadius()
    {
        float sum = 0f;
        for (int i = 0; i < ringCount; i++)
            sum += ringRadii[i];
        return (sum / ringCount) * maxRadius;
    }

    // ─────────────────────────────────────────────
    // Procedural mesh — lathe / revolution surface
    // ─────────────────────────────────────────────

    /// <summary>
    /// Builds a lathe mesh from the ring profile.
    /// Each ring is a circle; adjacent rings are connected by quads.
    /// The mesh stays grounded — base never moves below Y=0.
    /// </summary>
    private void BuildMesh()
    {
        const int segments = 24; // angular subdivisions around the axis

        int vertsPerRing  = segments + 1; // +1 to close the loop cleanly
        int totalRings    = ringCount + 1; // +1 for the extra cap ring at top
        int totalVerts    = totalRings * vertsPerRing + 2; // +2 for centre caps

        Vector3[] verts  = new Vector3[totalVerts];
        Vector2[] uvs    = new Vector2[totalVerts];
        Vector3[] norms  = new Vector3[totalVerts];
        int[] tris;

        float angleStep = 2f * Mathf.PI / segments;
        float hStep     = currentHeight / ringCount;

        // Build ring vertices — base sits at Y=0, grows upward
        for (int ring = 0; ring <= ringCount; ring++)
        {
            float y = ring * hStep;

            // Lerp radius between this ring and the next for smoother normals on the top cap
            float normR = (ring < ringCount) ? ringRadii[ring] : ringRadii[ringCount - 1];
            float r = normR * maxRadius;

            for (int seg = 0; seg <= segments; seg++)
            {
                float angle = seg * angleStep;
                float x = Mathf.Cos(angle) * r;
                float z = Mathf.Sin(angle) * r;

                int idx = ring * vertsPerRing + seg;
                verts[idx] = new Vector3(x, y, z);
                uvs[idx]   = new Vector2((float)seg / segments, (float)ring / ringCount);

                // Normal points outward (simple cylindrical approximation)
                norms[idx] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)).normalized;
            }
        }

        // Bottom cap centre
        int botCentre = totalRings * vertsPerRing;
        verts[botCentre] = new Vector3(0f, 0f, 0f);
        uvs[botCentre]   = new Vector2(0.5f, 0f);
        norms[botCentre] = Vector3.down;

        // Top cap centre
        int topCentre = botCentre + 1;
        verts[topCentre] = new Vector3(0f, currentHeight, 0f);
        uvs[topCentre]   = new Vector2(0.5f, 1f);
        norms[topCentre] = Vector3.up;

        // Triangles
        int sideTriCount = ringCount * segments * 2;
        int capTriCount  = segments * 2;
        tris = new int[(sideTriCount + capTriCount) * 3];

        int t = 0;

        // Side quads
        for (int ring = 0; ring < ringCount; ring++)
        {
            for (int seg = 0; seg < segments; seg++)
            {
                int a = ring * vertsPerRing + seg;
                int b = a + 1;
                int c = a + vertsPerRing;
                int d = c + 1;

                // Two triangles per quad
                tris[t++] = a; tris[t++] = c; tris[t++] = b;
                tris[t++] = b; tris[t++] = c; tris[t++] = d;
            }
        }

        // Bottom cap
        for (int seg = 0; seg < segments; seg++)
        {
            tris[t++] = botCentre;
            tris[t++] = seg + 1;
            tris[t++] = seg;
        }

        // Top cap
        int topRingStart = ringCount * vertsPerRing;
        for (int seg = 0; seg < segments; seg++)
        {
            tris[t++] = topCentre;
            tris[t++] = topRingStart + seg;
            tris[t++] = topRingStart + seg + 1;
        }

        // Apply to mesh
        if (clayMesh == null)
        {
            clayMesh = new Mesh();
            clayMesh.name = "ClayMesh";
        }

        clayMesh.Clear();
        clayMesh.vertices  = verts;
        clayMesh.triangles = tris;
        clayMesh.uv        = uvs;
        clayMesh.normals   = norms;
        clayMesh.RecalculateNormals();  // smooth shading
        clayMesh.RecalculateBounds();

        if (meshFilter != null)
            meshFilter.sharedMesh = clayMesh;

        // Update collider so hands register contact accurately
        if (meshCollider != null)
        {
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = clayMesh;
        }
    }

    // ─────────────────────────────────────────────
    // Debug gizmos — shows ring profile in Scene view
    // ─────────────────────────────────────────────

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (ringRadii == null || ringRadii.Length == 0) return;

        Gizmos.color = new Color(0.8f, 0.5f, 0.2f, 0.6f);
        float hStep = currentHeight / ringCount;

        for (int i = 0; i < ringCount; i++)
        {
            float y = i * hStep + hStep * 0.5f;
            float r = ringRadii[i] * maxRadius;
            Vector3 centre = transform.position + new Vector3(0, y, 0);
            Gizmos.DrawWireSphere(centre, r);
        }
    }
#endif
}
