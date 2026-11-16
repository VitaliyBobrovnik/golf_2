using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Golf.View
{
	public class SettingsView : MonoBehaviour
	{
		public event Action<int> onPresetChanged;
		public event Action onBack;

		[SerializeField] private Button m_lowButton;

		[SerializeField] private Button m_mediumButton;

		[SerializeField] private Button m_highButton;

		[SerializeField] private Button m_backButton;

		public void SetPreset(int index)
		{
			m_lowButton.interactable = index != 0;
			m_mediumButton.interactable = index != 1;
			m_highButton.interactable = index != 2;
		}

		public void Show()
		{
			gameObject.SetActive(true);
		}

		public void Hide()
		{
			gameObject.SetActive(false);
		}

		private void Awake()
		{
			foreach (TMP_Text label in GetComponentsInChildren<TMP_Text>(true))
			{
				label.raycastTarget = false;
			}

			m_lowButton.onClick.AddListener(OnLowClickHandler);
			m_mediumButton.onClick.AddListener(OnMediumClickHandler);
			m_highButton.onClick.AddListener(OnHighClickHandler);
			m_backButton.onClick.AddListener(OnBackClickHandler);
		}

		private void OnDestroy()
		{
			m_lowButton.onClick.RemoveListener(OnLowClickHandler);
			m_mediumButton.onClick.RemoveListener(OnMediumClickHandler);
			m_highButton.onClick.RemoveListener(OnHighClickHandler);
			m_backButton.onClick.RemoveListener(OnBackClickHandler);
		}

		private void OnLowClickHandler()
		{
			onPresetChanged?.Invoke(0);
		}

		private void OnMediumClickHandler()
		{
			onPresetChanged?.Invoke(1);
		}

		private void OnHighClickHandler()
		{
			onPresetChanged?.Invoke(2);
		}

		private void OnBackClickHandler()
		{
			onBack?.Invoke();
		}
	}
}
