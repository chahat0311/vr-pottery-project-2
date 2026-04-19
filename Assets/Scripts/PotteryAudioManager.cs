using UnityEngine;

public class PotteryAudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource wheelHumSource;
    public AudioSource ambienceSource;
    public AudioSource claySfxSource;

    [Header("Audio Clips - drag in from Project panel")]
    public AudioClip wheelHumClip;
    public AudioClip ambienceClip;
    public AudioClip[] clayContactClips;

    [Header("Volume Settings")]
    public float wheelHumVolume = 0.4f;
    public float ambienceVolume = 0.2f;

    private WheelPhysics wheel;
    private float clayCooldown = 0f;

    void Start()
    {
        wheel = Object.FindFirstObjectByType<WheelPhysics>();

        // Set up wheel hum
        if (wheelHumClip != null)
        {
            wheelHumSource.clip = wheelHumClip;
            wheelHumSource.loop = true;
            wheelHumSource.volume = 0f;
            wheelHumSource.Play();
        }

        // Set up ambience
        if (ambienceClip != null)
        {
            ambienceSource.clip = ambienceClip;
            ambienceSource.loop = true;
            ambienceSource.volume = ambienceVolume;
            ambienceSource.Play();
        }
    }

    void Update()
    {
        clayCooldown -= Time.deltaTime;

        if (wheel == null) return;

        // Wheel hum pitch and volume rise with spin speed
        // GetSpeed() returns 0-10ish, we normalize it
        float normalizedSpeed = Mathf.Clamp01(wheel.GetSpeed() / 10f);

        wheelHumSource.volume = Mathf.Lerp(0f, wheelHumVolume, normalizedSpeed);
        wheelHumSource.pitch = Mathf.Lerp(0.6f, 1.3f, normalizedSpeed);
    }

    // Call this from ToolInteraction when tool touches clay
    public void PlayClaySound()
    {
        if (clayCooldown > 0f) return;
        if (clayContactClips.Length == 0) return;

        // Pick a random clay sound each time so it doesn't feel repetitive
        AudioClip clip = clayContactClips[Random.Range(0, clayContactClips.Length)];
        claySfxSource.pitch = Random.Range(0.9f, 1.1f);
        claySfxSource.PlayOneShot(clip, 0.5f);
        clayCooldown = 0.1f;
    }
}