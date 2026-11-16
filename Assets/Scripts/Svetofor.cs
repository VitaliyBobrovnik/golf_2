using System;
using Golf.GamePlay;
using UnityEngine;

public class Svetofor : MonoBehaviour
{
    private const float ExtraSignalDelay = 1f;

    private static readonly Color[] s_colors =
    {
        Color.red, Color.yellow, Color.green, Color.blue
    };

    public event Action onSignalShown;

    public static int colorCount => s_colors.Length;
    public int colorIndex { get; private set; }

    private LightColor m_light;

    private void Awake()
    {
        m_light = new LightColor(GetComponent<Renderer>());
    }

    private void Update()
    {
        m_light.Update();
    }

    public static Color GetColor(int colorIndex)
    {
        return s_colors[colorIndex];
    }

    public void ChooseColor()
    {
        colorIndex = UnityEngine.Random.Range(0, colorCount);
    }

    public void WaitForSignal(float minDelay, float maxDelay, float delayScale)
    {
        float delay = (UnityEngine.Random.Range(minDelay, maxDelay) + ExtraSignalDelay) * delayScale;
        if (delay <= 0f)
        {
            ShowSignal();
            return;
        }

        Invoke(nameof(ShowSignal), delay);
    }

    public void Hide()
    {
        CancelInvoke(nameof(ShowSignal));
        m_light.Hide();
    }

    private void ShowSignal()
    {
        m_light.Show(GetColor(colorIndex));
        onSignalShown?.Invoke();
    }
}