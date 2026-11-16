using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Golf.View
{
    public class ScoreView : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_scoreText;
        [SerializeField] private Image[] m_attemptImages;

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetScore(int score)
        {
            m_scoreText.text = score.ToString("D8");
        }

        public void SetAttempts(int attempts)
        {
            for (int i = 0; i < m_attemptImages.Length; i++)
            {
                m_attemptImages[i].enabled = i < attempts;
            }
        }
    }
}