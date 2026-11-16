using Golf.GamePlay;
using Golf.States;
using Golf.View;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Golf.Source
{
	[MovedFrom(true, sourceNamespace: "Golf")]
	public class Bootstrap : MonoBehaviour
	{
		private const int TargetFrameRate = 120;

		[SerializeField] private GameRound m_gameRound;

		[SerializeField] private MainMenuView m_mainMenuView;

		[SerializeField] private SettingsView m_settingsView;

		[SerializeField] private GameOverView m_gameOverView;

		[SerializeField] private ScoreView m_scoreView;

		private StateMachine m_stateMachine;

		private void Awake()
		{
			Application.targetFrameRate = TargetFrameRate;

			ScoreManager scoreManager = new ScoreManager();
			m_stateMachine = new StateMachine();
			m_stateMachine.Register(new MainMenuState(m_stateMachine, m_mainMenuView, scoreManager));
			m_stateMachine.Register(new SettingsState(m_stateMachine, m_settingsView));
			m_stateMachine.Register(new GamePlayState(m_stateMachine, m_gameRound, m_scoreView, scoreManager));
			m_stateMachine.Register(new GameOverState(m_stateMachine, m_gameOverView, scoreManager));
			m_stateMachine.ChangeState<MainMenuState>();
		}

		private void OnDestroy()
		{
			m_stateMachine?.Stop();
		}
	}
}
