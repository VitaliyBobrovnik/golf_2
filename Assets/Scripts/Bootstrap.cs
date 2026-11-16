using Golf.States;
using Golf.View;
using UnityEngine;

namespace Golf
{
    public class Bootstrap : MonoBehaviour
    {
        [SerializeField] private GameRound m_gameRound;
        [SerializeField] private MainMenuView m_mainMenuView;
        [SerializeField] private SettingsView m_settingsView;
        [SerializeField] private GameOverView m_gameOverView;
        [SerializeField] private ScoreView m_scoreView;

        private StateMachine m_stateMachine;

        private void Awake()
        {
            Application.targetFrameRate = 120;

            ScoreManager scoreManager = new ScoreManager();
            m_stateMachine = new StateMachine();
            m_stateMachine.Register(new MainMenuState(m_stateMachine, m_mainMenuView, scoreManager));
            m_stateMachine.Register(new SettingsState(m_stateMachine, m_settingsView));
            m_stateMachine.Register(new GamePlayState(m_stateMachine, m_gameRound, m_scoreView, scoreManager));
            m_stateMachine.Register(new GameOverState(m_stateMachine, m_gameOverView, scoreManager));
            m_stateMachine.ChangeState<MainMenuState>();
        }

        private void Update()
        {
            m_stateMachine.Update();
        }

        private void OnDestroy()
        {
            m_stateMachine?.Stop();
        }
    }
}
