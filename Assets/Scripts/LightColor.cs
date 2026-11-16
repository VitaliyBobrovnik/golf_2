using UnityEngine;

namespace Golf.GamePlay
{
    public class LightColor
    {
        private const int LightMaterialIndex = 1;
        private const float FadeTime = 0.15f;

        private readonly Renderer m_renderer;
        private readonly MaterialPropertyBlock m_properties = new();

        private Color m_current;
        private Color m_start;
        private Color m_target = Color.black;
        private float m_elapsed = FadeTime;

        public LightColor(Renderer renderer)
        {
            m_renderer = renderer;
            Apply(Color.black);
        }

        public void Show(Color color)
        {
            if (m_target == color)
            {
                return;
            }

            m_start = Color.black;
            m_target = color;
            m_elapsed = 0f;
            Apply(m_start);
        }

        public void Hide()
        {
            if (m_target == Color.black)
            {
                return;
            }

            m_start = m_current;
            m_target = Color.black;
            m_elapsed = 0f;
        }

        public void Update()
        {
            if (m_elapsed < FadeTime)
            {
                m_elapsed += Time.deltaTime;
                Apply(Color.Lerp(m_start, m_target, m_elapsed / FadeTime));
            }
        }

        private void Apply(Color color)
        {
            m_current = color;
            m_properties.SetColor("_BaseColor", color);
            m_properties.SetColor("_EmissionColor", color);
            m_renderer.SetPropertyBlock(m_properties, LightMaterialIndex);
        }
    }
}