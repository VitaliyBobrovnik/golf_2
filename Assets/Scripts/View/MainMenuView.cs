using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Golf.View
{
	public class MainMenuView : MonoBehaviour
	{
		public event Action onGameStart;
		public event Action onSettingsOpen;

		[SerializeField] private Button m_startButton;

		[SerializeField] private Button m_settingsButton;

		[SerializeField] private TMP_Text m_bestScoreText;

		public void Show()
		{
			gameObject.SetActive(true);
		}

		public void Hide()
		{
			gameObject.SetActive(false);
		}

		public void SetBestScore(int bestScore)
		{
			m_bestScoreText.text = bestScore.ToString("D8");
		}

		private void Awake()
		{
			m_startButton.onClick.AddListener(OnStartClickHandler);
			m_settingsButton.onClick.AddListener(OnSettingsClickHandler);
		}

		private void OnDestroy()
		{
			m_startButton.onClick.RemoveListener(OnStartClickHandler);
			m_settingsButton.onClick.RemoveListener(OnSettingsClickHandler);
		}

		private void OnStartClickHandler()
		{
			onGameStart?.Invoke();
		}

		private void OnSettingsClickHandler()
		{
			onSettingsOpen?.Invoke();
		}
	}
}
