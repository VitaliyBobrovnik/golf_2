using Golf.States.Interfaces;
using Golf.View;

namespace Golf.States
{
	public class GameOverState : IGameState, IActivate
	{
		private readonly StateMachine m_stateMachine;
		private readonly GameOverView m_gameOverView;
		private readonly ScoreManager m_scoreManager;

		public GameOverState(StateMachine stateMachine, GameOverView gameOverView, ScoreManager scoreManager)
		{
			m_stateMachine = stateMachine;
			m_gameOverView = gameOverView;
			m_scoreManager = scoreManager;
		}

		public void Enter()
		{
			m_gameOverView.SetScore(m_scoreManager.score, m_scoreManager.bestScore);
			m_gameOverView.Show();
		}

		public void Exit()
		{
			if (m_gameOverView != null)
			{
				m_gameOverView.Hide();
			}
		}

		public void Activate()
		{
			m_gameOverView.onRestart += OnRestartHandler;
			m_gameOverView.onMainMenu += OnMainMenuHandler;
		}

		public void Deactivate()
		{
			m_gameOverView.onRestart -= OnRestartHandler;
			m_gameOverView.onMainMenu -= OnMainMenuHandler;
		}

		private void OnRestartHandler()
		{
			m_stateMachine.ChangeState<GamePlayState>();
		}

		private void OnMainMenuHandler()
		{
			m_stateMachine.ChangeState<MainMenuState>();
		}
	}
}
