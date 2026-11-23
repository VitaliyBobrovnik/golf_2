using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Golf.View
{
	public class ScoreView : MonoBehaviour
	{
		[SerializeField] private TMP_Text m_scoreText;
		[SerializeField] private TMP_Text m_bonusText;
		[SerializeField] private TMP_Text m_hitResultText;

		[SerializeField] private Image[] m_attemptImages;

		private double m_bonusEndsAt;
		private int m_bonusMultiplier;
		private int m_secondsShown;

		public void Show()
		{
			gameObject.SetActive(true);
		}

		public void Hide()
		{
			CancelInvoke(nameof(HideHitResult));
			HideHitResult();
			gameObject.SetActive(false);
		}

		public void SetScore(int score)
		{
			m_scoreText.text = score.ToString("D8");
		}

		public void ShowHitResult(string result, Color color)
		{
			if (m_hitResultText == null)
			{
				return;
			}

			m_hitResultText.font = m_scoreText.font;
			m_hitResultText.fontSharedMaterial = m_scoreText.fontSharedMaterial;
			m_hitResultText.fontStyle = m_scoreText.fontStyle;
			m_hitResultText.text = result;
			m_hitResultText.color = Color.Lerp(color, Color.white, 0.6f);
			m_hitResultText.gameObject.SetActive(true);
			CancelInvoke(nameof(HideHitResult));
			Invoke(nameof(HideHitResult), 0.9f);
		}

		private void HideHitResult()
		{
			if (m_hitResultText != null)
			{
				m_hitResultText.gameObject.SetActive(false);
			}
		}

		public void SetBonus(int multiplier, double endsAt)
		{
			m_bonusMultiplier = multiplier;
			m_bonusEndsAt = endsAt;
			m_secondsShown = -1;
			UpdateBonusText();
		}

		private void Update()
		{
			if (m_bonusEndsAt > 0d)
			{
				UpdateBonusText();
			}
		}

		private void UpdateBonusText()
		{
			if (m_bonusText == null)
			{
				return;
			}

			int seconds = Mathf.CeilToInt((float)(m_bonusEndsAt - Time.timeAsDouble));
			if (seconds <= 0)
			{
				m_bonusEndsAt = 0d;
				m_bonusText.gameObject.SetActive(false);
				return;
			}

			if (seconds != m_secondsShown)
			{
				m_secondsShown = seconds;
				m_bonusText.SetText("X{0}  {1}S", m_bonusMultiplier, seconds);
				m_bonusText.gameObject.SetActive(true);
			}
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
