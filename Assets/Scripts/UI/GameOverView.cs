using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Golf.View
{
    public class GameOverView : MonoBehaviour
    {
        public event Action onRestart;
        public event Action onMainMenu;

        [SerializeField] private Button m_restartButton;
        [SerializeField] private Button m_mainMenuButton;
        [SerializeField] private TMP_Text m_scoreText;
        [SerializeField] private TMP_Text m_bestScoreText;

        private void Awake()
        {
            m_restartButton.onClick.AddListener(OnRestartButtonClick);
            m_mainMenuButton.onClick.AddListener(OnMainMenuButtonClick);
        }

        private void OnDestroy()
        {
            m_restartButton.onClick.RemoveListener(OnRestartButtonClick);
            m_mainMenuButton.onClick.RemoveListener(OnMainMenuButtonClick);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetScore(int score, int bestScore)
        {
            m_scoreText.text = score.ToString("D8");
            m_bestScoreText.text = bestScore.ToString("D8");
        }

        private void OnRestartButtonClick()
        {
            onRestart?.Invoke();
        }

        private void OnMainMenuButtonClick()
        {
            onMainMenu?.Invoke();
        }
    }
}