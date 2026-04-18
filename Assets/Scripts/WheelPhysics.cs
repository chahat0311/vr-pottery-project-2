using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class WheelPhysics : MonoBehaviour
{
    [Header("Spin Settings")]
    public float spinSpeed = 5f;         // Main spin speed
    public float wobbleAmount = 0.003f;  // Tiny imperfection (keep this small!)

    private Rigidbody rb;
    private float wobbleTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePosition
                       | RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationZ;
    }

    void FixedUpdate()
    {
        // Keep the wheel spinning at a steady speed
        rb.angularVelocity = new Vector3(0f, spinSpeed, 0f);

        // Add a tiny wobble over time - makes it feel real and handmade
        wobbleTimer += Time.fixedDeltaTime;
        float wobble = Mathf.Sin(wobbleTimer * 2.3f) * wobbleAmount;
        transform.localRotation *= Quaternion.Euler(wobble, 0f, wobble * 0.6f);
    }

    public float GetSpeed()
    {
        return spinSpeed;
    }
}