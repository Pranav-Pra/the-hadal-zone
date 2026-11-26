using UnityEngine;

public class DepthAtmosphere : MonoBehaviour
{
    [Header("References")]
    public Transform robot;
    public Light sun;

    [Header("Descent")]
    public float maxDepth = 500f;
    public bool testDescend = true;
    public float descendSpeed = 10f;

    [Header("Look over depth (0 = surface, 1 = bottom)")]
    public Gradient waterColor = MakeWaterGradient();
    public AnimationCurve fogDensity = new AnimationCurve(
        new Keyframe(0f, 0.03f), new Keyframe(0.4f, 0.08f), new Keyframe(1f, 0.15f));
    public AnimationCurve sunlight = new AnimationCurve(
        new Keyframe(0f, 1f), new Keyframe(0.2f, 0.35f), new Keyframe(0.4f, 0f), new Keyframe(1f, 0f));
    [Range(0f, 1f)] public float ambientStrength = 0.6f;

    [Header("Sky")]
    public Color surfaceLight = new Color(0.75f, 0.95f, 1f);   // color looking up near the surface
    [Range(0f, 1f)] public float abyssDarkness = 0.3f;         // how much darker looking down is

    Material sky;

    void Start()
    {
        // Work on a copy so Play mode doesn't permanently change the material asset
        if (RenderSettings.skybox)
        {
            sky = new Material(RenderSettings.skybox);
            RenderSettings.skybox = sky;
        }
    }

    void Update()
    {
        if (!robot) return;

        if (testDescend && robot.position.y > -maxDepth)
            robot.position += Vector3.down * descendSpeed * Time.deltaTime;

        float t = Mathf.Clamp01(-robot.position.y / maxDepth);
        Color water = waterColor.Evaluate(t);
        float light = sunlight.Evaluate(t);

        RenderSettings.fogColor = water;               // fog matches the horizon so objects blend in
        RenderSettings.fogDensity = fogDensity.Evaluate(t);
        RenderSettings.ambientLight = water * ambientStrength * light;
        if (sun) sun.intensity = light;

        if (sky)
        {
            sky.SetColor("_HorizonColor", water);
            sky.SetColor("_TopColor", Color.Lerp(water, surfaceLight, light));
            sky.SetColor("_BottomColor", water * abyssDarkness);
            sky.SetFloat("_SunStrength", light);
            if (sun) sky.SetVector("_SunDir", -sun.transform.forward);
        }
    }

    static Gradient MakeWaterGradient()
    {
        var g = new Gradient();
        g.SetKeys(
            new[] {
                new GradientColorKey(new Color32(0x2D, 0x90, 0xFF, 255), 0f),    // surface
                new GradientColorKey(new Color32(0x12, 0x55, 0xA8, 255), 0.2f),  // bottom of Sunlight Zone
                new GradientColorKey(new Color32(0x04, 0x16, 0x30, 255), 0.4f),  // bottom of Twilight Zone: no sunlight left
                new GradientColorKey(new Color32(0x01, 0x06, 0x0E, 255), 0.6f),  // Midnight Zone
                new GradientColorKey(Color.black, 0.8f),    
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        return g;
    }
}