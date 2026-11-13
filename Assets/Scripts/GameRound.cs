using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameRound : MonoBehaviour
{
    private const float OldCarLifetime = 5f;
    private const float HitBallLifetime = 2f;
    private const string GroundZoneTag = "GroundZone";

    [SerializeField] private Car m_carPrefab;
    [SerializeField] private Ball m_ballPrefab;
    [SerializeField] private Transform m_spawnPoint;
    [SerializeField] private Svetofor m_svetofor;
    [SerializeField] private LaunchButton m_firstButton;
    [SerializeField] private LaunchButton m_secondButton;
    [SerializeField] private TMP_Text m_scoreText;
    [SerializeField] private float m_n1 = 0.75f;
    [SerializeField] private float m_n2 = 2f;
    [SerializeField] private float m_signalLeadTime = 0.5f;

    private readonly List<GameObject> m_spawnedObjects = new List<GameObject>();
    private Car m_currentCar;
    private Car m_launchedCar;
    private Ball m_ball;
    private int m_score;
    private bool m_waitingForCar;
    private bool m_waitingForPress;

    private void Start()
    {
        Application.targetFrameRate = 120;
        m_svetofor.onSignalShown += OnSignalShownHandler;
        m_firstButton.onPressed += OnPressedHandler;
        m_secondButton.onPressed += OnPressedHandler;
        m_scoreText.text = "0";
        SpawnNextCar();
    }

    private void Update()
    {
        if (m_waitingForCar && m_ball == null && m_currentCar.isReady)
        {
            m_waitingForCar = false;
            m_svetofor.WaitForSignal(m_n1, m_n2);
        }
    }

    private void OnDestroy()
    {
        m_svetofor.onSignalShown -= OnSignalShownHandler;
        m_firstButton.onPressed -= OnPressedHandler;
        m_secondButton.onPressed -= OnPressedHandler;
        EndRound();
    }

    private void SpawnNextCar()
    {
        m_spawnedObjects.RemoveAll(item => item == null);
        m_currentCar = Instantiate(m_carPrefab, Vector3.zero, Quaternion.identity);
        m_spawnedObjects.Add(m_currentCar.gameObject);
        m_waitingForCar = true;
        m_svetofor.ChooseColor();
        ShuffleButtonColors();
    }

    private void OnSignalShownHandler()
    {
        m_ball = Instantiate(m_ballPrefab, m_spawnPoint.position, Quaternion.identity);
        m_spawnedObjects.Add(m_ball.gameObject);
        m_ball.onCollision += OnCollisionHandler;
        m_ball.onTrigger += OnTriggerHandler;
        Invoke(nameof(DropBall), m_signalLeadTime);
        m_waitingForPress = true;
    }

    private void DropBall()
    {
        m_ball.Drop();
    }

    private void OnPressedHandler(LaunchButton button)
    {
        if (!m_waitingForPress)
        {
            return;
        }

        if (button.colorIndex != m_svetofor.colorIndex)
        {
            ShuffleButtonColors();
            return;
        }

        if (!m_currentCar.TryLaunch())
        {
            return;
        }

        m_waitingForPress = false;
        button.PlayPressAnimation();
        m_svetofor.Hide();
        m_launchedCar = m_currentCar;
        SpawnNextCar();
        Destroy(m_launchedCar.gameObject, OldCarLifetime);
    }

    private void OnCollisionHandler(Collider other)
    {
        if (m_launchedCar == null || other.GetComponentInParent<Car>() != m_launchedCar)
        {
            return;
        }

        Destroy(m_ball.gameObject, HitBallLifetime);
        StopWatchingBall();
        m_launchedCar = null;
        m_score++;
        m_scoreText.text = m_score.ToString();
    }

    private void OnTriggerHandler(Collider other)
    {
        if (other.CompareTag(GroundZoneTag))
        {
            EndRound();
            Debug.Log("Промах. Игра окончена.");
        }
    }

    private void StopWatchingBall()
    {
        m_ball.onCollision -= OnCollisionHandler;
        m_ball.onTrigger -= OnTriggerHandler;
        m_ball = null;
    }

    private void EndRound()
    {
        m_waitingForCar = false;
        m_waitingForPress = false;
        CancelInvoke(nameof(DropBall));

        if (m_svetofor != null)
        {
            m_svetofor.Hide();
        }

        if (m_ball != null)
        {
            StopWatchingBall();
        }

        foreach (GameObject spawnedObject in m_spawnedObjects)
        {
            Destroy(spawnedObject);
        }

        m_spawnedObjects.Clear();
    }

    private void ShuffleButtonColors()
    {
        int signalColor = m_svetofor.colorIndex;
        int firstColor;
        int secondColor;

        do
        {
            int otherColor = Random.Range(0, Svetofor.colorCount - 1);
            if (otherColor >= signalColor)
            {
                otherColor++;
            }

            firstColor = signalColor;
            secondColor = otherColor;

            if (Random.Range(0, 2) == 0)
            {
                firstColor = otherColor;
                secondColor = signalColor;
            }
        }
        while (firstColor == m_firstButton.colorIndex && secondColor == m_secondButton.colorIndex);

        m_firstButton.SetColor(firstColor, Svetofor.GetColor(firstColor));
        m_secondButton.SetColor(secondColor, Svetofor.GetColor(secondColor));
    }
}
