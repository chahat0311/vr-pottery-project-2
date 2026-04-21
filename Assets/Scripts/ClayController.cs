using UnityEngine;

public class ClayController : MonoBehaviour
{
    [Header("Clay Shape Limits")]
    public float minHeight = 0.08f;
    public float maxHeight = 0.35f;
    public float minRadius = 0.06f;
    public float maxRadius = 0.22f;

    [Header("Current Shape")]
    public float currentHeight = 0.14f;
    public float currentRadius = 0.18f;

    [Header("Deform Speed")]
    public float shapeSpeed = 8f;

    private Vector3 targetScale;

    void Start()
    {
        UpdateTarget();
        transform.localScale = targetScale;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * shapeSpeed
        );
    }

    public void PressInward(float amount)
    {
        currentRadius -= amount;
        currentHeight += amount * 0.8f;
        ApplyClampAndUpdate();
    }

    public void PressDown(float amount)
    {
        currentHeight -= amount;
        currentRadius += amount * 0.6f;
        ApplyClampAndUpdate();
    }

    public void PullUp(float amount)
    {
        currentHeight += amount;
        currentRadius -= amount * 0.4f;
        ApplyClampAndUpdate();
    }

    private void ApplyClampAndUpdate()
    {
        currentHeight = Mathf.Clamp(currentHeight, minHeight, maxHeight);
        currentRadius = Mathf.Clamp(currentRadius, minRadius, maxRadius);
        UpdateTarget();
    }

    private void UpdateTarget()
    {
        targetScale = new Vector3(currentRadius * 2f, currentHeight, currentRadius * 2f);
    }
}