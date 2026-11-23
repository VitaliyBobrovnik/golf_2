using Golf.GamePlay;
using Golf.States.Interfaces;
using Golf.View;
using UnityEngine;

namespace Golf.States
{
	public class GamePlayState : IGameState, IActivate
	{
		private const int MaxAttempts = 3;
		private const double ScoreBonusDuration = 30d;

		private readonly StateMachine m_stateMachine;
		private readonly GameRound m_gameRound;
		private readonly ScoreView m_scoreView;
		private readonly ScoreManager m_scoreManager;
		private int m_attempts;
		private double m_bonusEndsAt;
		private int m_bonusMultiplier;

		public GamePlayState(
			StateMachine stateMachine,
			GameRound gameRound,
			ScoreView scoreView,
			ScoreManager scoreManager)
		{
			m_stateMachine = stateMachine;
			m_gameRound = gameRound;
			m_scoreView = scoreView;
			m_scoreManager = scoreManager;
		}

		public void Enter()
		{
			m_attempts = MaxAttempts;
			m_bonusEndsAt = 0d;
			m_scoreView.SetBonus(1, 0d);
			m_scoreManager.ResetScore();
			m_scoreView.SetScore(m_scoreManager.score);
			m_scoreView.SetAttempts(m_attempts);
			m_scoreView.Show();
			m_gameRound.BeginRound();
		}

		public void Exit()
		{
			if (m_gameRound != null)
			{
				m_gameRound.EndRound();
			}

			if (m_scoreView != null)
			{
				m_scoreView.Hide();
			}
		}

		public void Activate()
		{
			m_gameRound.onBallHit += OnBallHitHandler;
			m_gameRound.onMissed += OnMissedHandler;
			m_scoreManager.onScoreChanged += OnScoreChangedHandler;
		}

		public void Deactivate()
		{
			m_gameRound.onBallHit -= OnBallHitHandler;
			m_gameRound.onMissed -= OnMissedHandler;
			m_scoreManager.onScoreChanged -= OnScoreChangedHandler;
		}

		private void OnScoreChangedHandler(int score) => m_scoreView.SetScore(score);

		private void OnBallHitHandler(BallStats stats)
		{
			int previousScore = m_scoreManager.score;
			switch (stats.effect)
			{
				case BallEffect.Add:
					int multiplier = Time.timeAsDouble < m_bonusEndsAt ? m_bonusMultiplier : 1;
					m_scoreManager.AddScore(stats.value, multiplier);
					break;
				case BallEffect.Multiply:
					m_bonusMultiplier = stats.value;
					m_bonusEndsAt = Time.timeAsDouble + ScoreBonusDuration;
					m_scoreView.SetBonus(m_bonusMultiplier, m_bonusEndsAt);
					break;
				case BallEffect.Subtract:
					m_scoreManager.AddScore(-stats.value);
					break;
			}

			int scoreChange = m_scoreManager.score - previousScore;
			string result = stats.effect == BallEffect.Multiply
				? $"×{stats.value}"
				: scoreChange.ToString("+0;-0;0");
			m_scoreView.ShowHitResult(result, stats.color);

			if (scoreChange > 0)
			{
				m_gameRound.RecordScoredHit();
			}
		}

		private void OnMissedHandler()
		{
			m_attempts--;
			m_scoreView.SetAttempts(m_attempts);

			if (m_attempts == 0)
			{
				m_stateMachine.ChangeState<GameOverState>();
			}
		}
	}
}
