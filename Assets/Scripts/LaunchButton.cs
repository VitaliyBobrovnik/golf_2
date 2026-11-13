using System;
using UnityEngine;

public class LaunchButton : MonoBehaviour
{
    private const int LightMaterialIndex = 1;

    public event Action<LaunchButton> onPressed;

    public int colorIndex { get; private set; } = -1;

    [SerializeField]
    private Animation m_pressAnimation;

    private Renderer m_renderer;
    private MaterialPropertyBlock m_properties;

    public void SetColor(int colorIndex, Color color)
    {
        this.colorIndex = colorIndex;
        m_properties.SetColor("_BaseColor", color);
        m_properties.SetColor("_EmissionColor", color);
        m_renderer.SetPropertyBlock(m_properties, LightMaterialIndex);
    }

    public void Press()
    {
        onPressed?.Invoke(this);
    }

    public void PlayPressAnimation()
    {
        if (m_pressAnimation != null)
        {
            m_pressAnimation.Stop();
            m_pressAnimation.Play();
        }
    }

    private void Awake()
    {
        m_renderer = GetComponent<Renderer>();
        m_properties = new MaterialPropertyBlock();
    }
}