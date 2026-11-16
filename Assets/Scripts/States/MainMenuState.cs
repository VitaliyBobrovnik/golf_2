using Golf.States.Interfaces;
using Golf.View;

namespace Golf.States
{
	public class MainMenuState : IGameState, IActivate
	{
		private readonly StateMachine m_stateMachine;
		private readonly MainMenuView m_mainMenuView;
		private readonly ScoreManager m_scoreManager;

		public MainMenuState(StateMachine stateMachine, MainMenuView mainMenuView, ScoreManager scoreManager)
		{
			m_stateMachine = stateMachine;
			m_mainMenuView = mainMenuView;
			m_scoreManager = scoreManager;
		}

		public void Enter()
		{
			m_mainMenuView.SetBestScore(m_scoreManager.bestScore);
		}

		public void Exit() { }

		public void Activate()
		{
			m_mainMenuView.onGameStart += OnGameStartHandler;
			m_mainMenuView.onSettingsOpen += OnSettingsOpenHandler;
			m_mainMenuView.Show();
		}

		public void Deactivate()
		{
			m_mainMenuView.onGameStart -= OnGameStartHandler;
			m_mainMenuView.onSettingsOpen -= OnSettingsOpenHandler;

			if (m_mainMenuView != null)
			{
				m_mainMenuView.Hide();
			}
		}

		private void OnGameStartHandler()
		{
			m_stateMachine.ChangeState<GamePlayState>();
		}

		private void OnSettingsOpenHandler()
		{
			m_stateMachine.PushState<SettingsState>();
		}
	}
}
