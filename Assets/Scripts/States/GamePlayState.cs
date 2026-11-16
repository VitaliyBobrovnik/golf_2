using Golf.States.Interfaces;
using Golf.View;

namespace Golf.States
{
    public class GamePlayState : IGameState, IActivate
    {
        private const int MaxAttempts = 3;

        private readonly StateMachine m_stateMachine;
        private readonly GameRound m_gameRound;
        private readonly ScoreView m_scoreView;
        private readonly ScoreManager m_scoreManager;
        private int m_attempts;

        public GamePlayState(StateMachine stateMachine, GameRound gameRound, ScoreView scoreView, ScoreManager scoreManager)
        {
            m_stateMachine = stateMachine;
            m_gameRound = gameRound;
            m_scoreView = scoreView;
            m_scoreManager = scoreManager;
        }

        public void Enter()
        {
            m_attempts = MaxAttempts;
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
            m_gameRound.onBallHit += OnBallHit;
            m_gameRound.onMissed += OnMissed;
            m_scoreManager.onScoreChanged += m_scoreView.SetScore;
        }

        public void Deactivate()
        {
            m_gameRound.onBallHit -= OnBallHit;
            m_gameRound.onMissed -= OnMissed;
            m_scoreManager.onScoreChanged -= m_scoreView.SetScore;
        }

        private void OnBallHit()
        {
            m_scoreManager.AddScore(1);
        }

        private void OnMissed()
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
