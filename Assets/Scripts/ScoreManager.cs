using System;
using UnityEngine;

namespace Golf
{
	public class ScoreManager
	{
		private const string BestScoreKey = "Golf.BestScore";

		public event Action<int> onScoreChanged;

		public int score { get; private set; }
		public int bestScore { get; private set; }

		public ScoreManager()
		{
			bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
		}

		public void ResetScore()
		{
			score = 0;
			onScoreChanged?.Invoke(score);
		}

		public void AddScore(int amount)
		{
			SetScore((long)score + amount);
		}

		public void MultiplyScore(int multiplier)
		{
			SetScore((long)score * multiplier);
		}

		private void SetScore(long newScore)
		{
			score = (int)Math.Max(0, Math.Min(int.MaxValue, newScore));

			if (score > bestScore)
			{
				bestScore = score;
				PlayerPrefs.SetInt(BestScoreKey, bestScore);
				PlayerPrefs.Save();
			}

			onScoreChanged?.Invoke(score);
		}
	}
}
