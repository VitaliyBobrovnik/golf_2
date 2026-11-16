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

        private void Awake()
        {
            foreach (TMP_Text label in GetComponentsInChildren<TMP_Text>(true))
            {
                label.raycastTarget = false;
            }

            m_lowButton.onClick.AddListener(OnLowClicked);
            m_mediumButton.onClick.AddListener(OnMediumClicked);
            m_highButton.onClick.AddListener(OnHighClicked);
            m_backButton.onClick.AddListener(OnBackClicked);
        }

        private void OnDestroy()
        {
            m_lowButton.onClick.RemoveListener(OnLowClicked);
            m_mediumButton.onClick.RemoveListener(OnMediumClicked);
            m_highButton.onClick.RemoveListener(OnHighClicked);
            m_backButton.onClick.RemoveListener(OnBackClicked);
        }

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

        private void OnLowClicked()
        {
            onPresetChanged?.Invoke(0);
        }

        private void OnMediumClicked()
        {
            onPresetChanged?.Invoke(1);
        }

        private void OnHighClicked()
        {
            onPresetChanged?.Invoke(2);
        }

        private void OnBackClicked()
        {
            onBack?.Invoke();
        }
    }
}
