using UnityEngine;

public class ClayController : MonoBehaviour
{
    [Header("Clay Shape")]
    public float minHeight = 0.08f;    // flattest the clay can go
    public float maxHeight = 0.35f;    // tallest the clay can go
    public float minRadius = 0.06f;    // narrowest it can be
    public float maxRadius = 0.22f;    // widest it can be

    [Header("Current Shape - watch these change live!")]
    public float currentHeight = 0.14f;
    public float currentRadius = 0.18f;

    [Header("Deform Speed")]
    public float shapeSpeed = 0.4f;

    private Vector3 targetScale;

    void Start()
    {
        // Set initial shape
        targetScale = new Vector3(currentRadius * 2f, currentHeight, currentRadius * 2f);
        transform.localScale = targetScale;
    }

    void Update()
    {
        // Smoothly move toward target shape
        // This makes deformation feel fluid, not instant
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * shapeSpeed * 10f
        );
    }

    // Call this to push the clay inward (narrow it, raise it)
    public void PressInward(float amount)
    {
        currentRadius -= amount;
        currentHeight += amount * 0.8f;
        ClampShape();
        UpdateTarget();
    }

    // Call this to press down (widen it, flatten it)
    public void PressDown(float amount)
    {
        currentHeight -= amount;
        currentRadius += amount * 0.6f;
        ClampShape();
        UpdateTarget();
    }

    // Call this to pull upward (raise and narrow)
    public void PullUp(float amount)
    {
        currentHeight += amount;
        currentRadius -= amount * 0.4f;
        ClampShape();
        UpdateTarget();
    }

    void ClampShape()
    {
        // Make sure clay never goes beyond realistic limits
        currentHeight = Mathf.Clamp(currentHeight, minHeight, maxHeight);
        currentRadius = Mathf.Clamp(currentRadius, minRadius, maxRadius);
    }

    void UpdateTarget()
    {
        targetScale = new Vector3(currentRadius * 2f, currentHeight, currentRadius * 2f);
    }
}