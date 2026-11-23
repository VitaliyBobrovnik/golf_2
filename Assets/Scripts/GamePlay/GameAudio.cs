using System.Collections.Generic;
using UnityEngine;

namespace Golf.GamePlay
{
	public class GameAudio : MonoBehaviour
	{
		[SerializeField] private AudioClip m_ambient;
		[SerializeField] private AudioClip m_carApproach;
		[SerializeField] private AudioClip m_signal;
		[SerializeField] private AudioClip m_buttonPress;
		[SerializeField] private AudioClip m_wrongButtonPress;
		[SerializeField] private AudioClip m_ballHit;

		[SerializeField, Range(0f, 1f)] private float m_ambientVolume = 0.35f;
		[SerializeField, Range(0f, 1f)] private float m_effectVolume = 0.85f;
		[SerializeField, Range(0.1f, 3f)] private float m_minPitch = 0.9f;
		[SerializeField, Range(0.1f, 3f)] private float m_maxPitch = 1.1f;

		private readonly List<AudioSource> m_effectSources = new();

		public void PlayCarApproach() => PlayEffect(m_carApproach);
		public void PlaySignal() => PlayEffect(m_signal);
		public void PlayButtonPress() => PlayEffect(m_buttonPress);
		public void PlayWrongButtonPress() => PlayEffect(m_wrongButtonPress);
		public void PlayBallHit() => PlayEffect(m_ballHit);

		private void Start()
		{
			if (m_ambient == null)
			{
				return;
			}

			AudioSource ambient = gameObject.AddComponent<AudioSource>();
			ambient.clip = m_ambient;
			ambient.loop = true;
			ambient.volume = m_ambientVolume;
			ambient.pitch = 1f;
			ambient.Play();
		}

		private void PlayEffect(AudioClip clip)
		{
			if (clip == null)
			{
				return;
			}

			AudioSource source = m_effectSources.Find(effect => !effect.isPlaying);
			if (source == null)
			{
				source = gameObject.AddComponent<AudioSource>();
				m_effectSources.Add(source);
			}

			float minPitch = Mathf.Min(m_minPitch, m_maxPitch);
			float maxPitch = Mathf.Max(m_minPitch, m_maxPitch);
			source.clip = clip;
			source.volume = m_effectVolume;
			source.pitch = Random.Range(minPitch, maxPitch);
			source.Play();
		}
	}
}
