using UnityEngine;

public sealed class GuestSpriteMotion : MonoBehaviour
{
    private Vector3 basePosition;
    private Vector3 baseScale;
    private float phase;
    private float hoverAmount;
    private bool isHovered;

    public void Initialize(Vector3 position, Vector3 scale, float phaseOffset)
    {
        basePosition = position;
        baseScale = scale;
        phase = phaseOffset;
        hoverAmount = 0;
        isHovered = false;
        transform.position = basePosition;
        transform.localScale = baseScale;
    }

    public void SetHovered(bool value)
    {
        isHovered = value;
    }

    private void Update()
    {
        Tick(Time.unscaledDeltaTime, Time.unscaledTime);
    }

    public void Tick(float dt, float time)
    {
        hoverAmount = Mathf.MoveTowards(hoverAmount, isHovered ? 1f : 0f, dt * 7f);
        float hoverBob = Mathf.Sin(time * 4f + phase) * .01f * hoverAmount;
        transform.position = basePosition + Vector3.up * (.02f * hoverAmount + hoverBob);
        // Scale around the seated hip, so a hovered guest stays attached to the chair.
        transform.localScale = baseScale * (1f + Mathf.SmoothStep(0f, .045f, hoverAmount));
    }
}
