using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Golf.GamePlay
{
	[MovedFrom(true, sourceNamespace: "", sourceClassName: "Svetofor")]
	public class TrafficLight : MonoBehaviour
	{
		private const float ExtraSignalDelay = 1f;

		public static int colorCount => s_colors.Length;

		public event Action onSignalShown;

		public int colorIndex { get; private set; }

		private static readonly Color[] s_colors =
		{
			Color.red, Color.yellow, Color.green, Color.blue
		};
		private LightColor m_light;

		public static Color GetColor(int colorIndex)
		{
			return s_colors[colorIndex];
		}

		public void ScheduleSignal(float minDelay, float maxDelay, float delayScale)
		{
			colorIndex = UnityEngine.Random.Range(0, colorCount);
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

		private void OnEnable()
		{
			if (m_light == null)
			{
				m_light = new LightColor(GetComponent<Renderer>());
			}
		}

		private void Update()
		{
			m_light.Update();
		}

		private void ShowSignal()
		{
			m_light.Show(GetColor(colorIndex));
			onSignalShown?.Invoke();
		}
	}
}
