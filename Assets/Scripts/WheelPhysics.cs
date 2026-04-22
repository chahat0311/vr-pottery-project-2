using UnityEngine;

public class WheelPhysics : MonoBehaviour
{
    [Header("Spin Settings")]
    public float spinSpeed = 5f;
    public float wobbleAmount = 0.003f;

    private float wobbleTimer = 0f;

    void Update()
    {
        wobbleTimer += Time.deltaTime;
        float wobble = Mathf.Sin(wobbleTimer * 2.3f) * wobbleAmount;

        transform.Rotate(0f, spinSpeed * Mathf.Rad2Deg * Time.deltaTime, 0f, Space.Self);
        transform.localRotation *= Quaternion.Euler(wobble, 0f, wobble * 0.6f);
    }

    public float GetSpeed() => spinSpeed;
}
