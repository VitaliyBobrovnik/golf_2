using System;
using UnityEngine;

public class Svetofor : MonoBehaviour
{
    private const int LightMaterialIndex = 1;
    private const float ExtraSignalDelay = 1f;

    public static int colorCount => s_colors.Length;

    public event Action onSignalShown;

    public int colorIndex { get; private set; }

    private static readonly Color[] s_colors = { Color.red, Color.yellow, Color.green, Color.blue };

    private Renderer m_renderer;
    private MaterialPropertyBlock m_properties;

    public static Color GetColor(int colorIndex)
    {
        return s_colors[colorIndex];
    }

    public void ChooseColor()
    {
        colorIndex = UnityEngine.Random.Range(0, colorCount);
    }

    public void WaitForSignal(float minDelay, float maxDelay)
    {
        float delay = UnityEngine.Random.Range(minDelay, maxDelay) + ExtraSignalDelay;
        Invoke(nameof(ShowSignal), delay);
    }

    public void Hide()
    {
        CancelInvoke(nameof(ShowSignal));
        SetLightColor(Color.black);
    }

    private void Awake()
    {
        m_renderer = GetComponent<Renderer>();
        m_properties = new MaterialPropertyBlock();
        SetLightColor(Color.black);
    }

    private void ShowSignal()
    {
        SetLightColor(GetColor(colorIndex));
        onSignalShown?.Invoke();
    }

    private void SetLightColor(Color color)
    {
        m_properties.SetColor("_BaseColor", color);
        m_properties.SetColor("_EmissionColor", color);
        m_renderer.SetPropertyBlock(m_properties, LightMaterialIndex);
    }
}