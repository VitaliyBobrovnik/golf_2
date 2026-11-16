using System;
using Golf.GamePlay;
using UnityEngine;

public class LaunchButton : MonoBehaviour
{
    public event Action<LaunchButton> onPressed;

    public int colorIndex { get; private set; } = -1;

    [SerializeField]
    private Animation m_pressAnimation;

	private LightColor m_light;

    public void SetColor(int colorIndex, Color color)
    {
        this.colorIndex = colorIndex;
		m_light.Show(color);
	}

	public void HideLight()
	{
		m_light.Hide();
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
		m_light = new LightColor(GetComponent<Renderer>());
	}

	private void Update()
	{
		m_light.Update();
    }
}
