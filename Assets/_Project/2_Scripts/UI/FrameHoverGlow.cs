using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// brightens the button frame's glow while the mouse hovers over the button
public class FrameHoverGlow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image frame;    // ButtonFrame's Image
    [SerializeField] private Outline glow;   // ButtonFrame's Outline

    [Header("Normal")]
    [SerializeField] private Color normalFrame = new Color(0.22f, 1f, 0.42f, 0.8f);
    [SerializeField] private float normalGlowAlpha = 0.15f;
    [SerializeField] private Vector2 normalGlowSize = new Vector2(2f, -2f);

    [Header("Hover")]
    [SerializeField] private Color hoverFrame = new Color(0.62f, 1f, 0.71f, 1f);
    [SerializeField] private float hoverGlowAlpha = 0.6f;
    [SerializeField] private Vector2 hoverGlowSize = new Vector2(5f, -5f);

    [SerializeField] private float fadeSpeed = 10f;

    private bool hovering;

    public void OnPointerEnter(PointerEventData e) => hovering = true;
    public void OnPointerExit(PointerEventData e) => hovering = false;

    void Update()
    {
        float t = Time.unscaledDeltaTime * fadeSpeed;

        frame.color = Color.Lerp(frame.color, hovering ? hoverFrame : normalFrame, t);

        Color g = glow.effectColor;
        g.r = 0.22f; g.g = 1f; g.b = 0.42f;  // HUD green
        g.a = Mathf.Lerp(g.a, hovering ? hoverGlowAlpha : normalGlowAlpha, t);
        glow.effectColor = g;

        glow.effectDistance = Vector2.Lerp(glow.effectDistance, hovering ? hoverGlowSize : normalGlowSize, t);
    }
}