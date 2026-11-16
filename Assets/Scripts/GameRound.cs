using System;
using System.Collections.Generic;
using UnityEngine;

public class GameRound : MonoBehaviour
{
	private const float OldCarLifetime = 5f;
	private const float HitBallLifetime = 2f;

	public event Action onBallHit;
	public event Action onMissed;

	[SerializeField] private Car m_carPrefab;
	[SerializeField] private Ball m_ballPrefab;
	[SerializeField] private Transform m_spawnPoint;
	[SerializeField] private Collider m_groundZone;
	[SerializeField] private Svetofor m_svetofor;
	[SerializeField] private LaunchButton m_firstButton;
	[SerializeField] private LaunchButton m_secondButton;
	[SerializeField] private float m_n1 = 0.75f;
	[SerializeField] private float m_n2 = 2f;
	[SerializeField, Min(1)] private int m_hitsForInstantSignal = 10;
	[SerializeField, Min(0.01f)] private float m_minReactionTime = 0.35f;
	[SerializeField, Range(0.01f, 1f)] private float m_reactionDifficulty = 0.15f;
	[SerializeField] private float m_signalLeadTime = 0.5f;
	[SerializeField, Min(0f)] private float m_launchDelayAfterDrop = 0.62f;
	[SerializeField, Min(0f)] private float m_earlyPressWindow = 0.25f;
	[SerializeField, Min(0.01f)] private float m_carTimeToBall = 0.76f;

	private readonly List<GameObject> m_spawnedObjects = new List<GameObject>();
	private Car m_currentCar;
	private Car m_launchedCar;
	private Ball m_ball;
	private bool m_isPlaying;
	private bool m_waitingForCar;
	private bool m_waitingForPress;
	private bool m_waitingForDrop;
	private bool m_launchQueued;
	private bool m_waitForTimedLaunch;
	private bool m_shotCanScore;
	private int m_hitCount;
	private double m_signalTime;
	private double m_reactionWindow;
	private float m_dropTime;
	private float m_launchTime;

	public void BeginRound()
	{
		if (m_isPlaying)
		{
			return;
		}

		m_isPlaying = true;
		m_hitCount = 0;
		m_svetofor.onSignalShown += OnSignalShownHandler;
		m_firstButton.onPressed += OnPressedHandler;
		m_secondButton.onPressed += OnPressedHandler;
		SpawnNextCar();
		m_waitingForCar = true;
	}

	public void EndRound()
	{
		if (!m_isPlaying)
		{
			return;
		}

		m_isPlaying = false;
		m_waitingForCar = false;
		m_waitingForPress = false;
		m_waitingForDrop = false;
		m_launchQueued = false;
		m_waitForTimedLaunch = false;
		m_shotCanScore = false;
		m_svetofor.onSignalShown -= OnSignalShownHandler;
		m_firstButton.onPressed -= OnPressedHandler;
		m_secondButton.onPressed -= OnPressedHandler;
		HideLights();

		if (m_ball != null)
		{
			StopWatchingBall();
		}

		foreach (GameObject spawnedObject in m_spawnedObjects)
		{
			Destroy(spawnedObject);
		}

		m_spawnedObjects.Clear();
		m_currentCar = null;
		m_launchedCar = null;
	}

	private void Update()
	{
		if (m_waitingForCar && m_currentCar.isReady)
		{
			m_waitingForCar = false;
			m_svetofor.ChooseColor();
			float delayScale = Mathf.Clamp01(1f - (float)m_hitCount / m_hitsForInstantSignal);
			m_svetofor.WaitForSignal(m_n1, m_n2, delayScale);
		}
	}

	private void FixedUpdate()
	{
		if (m_waitingForDrop && Time.fixedTime >= m_dropTime)
		{
			m_waitingForDrop = false;
			SpawnBall(Vector3.zero);
			m_launchTime = Time.fixedTime + m_launchDelayAfterDrop;
		}

		TryLaunchQueuedCar();
	}

	private void OnDestroy()
	{
		EndRound();
	}

	private void SpawnNextCar()
	{
		m_spawnedObjects.RemoveAll(item => item == null);
		m_currentCar = Instantiate(m_carPrefab, Vector3.zero, Quaternion.identity);
		m_spawnedObjects.Add(m_currentCar.gameObject);
	}

	private void OnSignalShownHandler()
	{
		ShuffleButtonColors();
		float dropDelay = Mathf.Max(m_signalLeadTime, m_earlyPressWindow);
		double minimum = Mathf.Max(m_minReactionTime, m_earlyPressWindow);
		double initialWindow = Math.Max(dropDelay + (double)m_launchDelayAfterDrop, minimum + 0.01);
		m_reactionWindow = minimum + (initialWindow - minimum) / (1d + m_hitCount * (double)m_reactionDifficulty);
		m_signalTime = Time.timeAsDouble;
		m_dropTime = Time.time + dropDelay;
		m_waitingForDrop = true;
		m_waitingForPress = true;
	}

	private void SpawnBall(Vector3 velocity)
	{
		m_ball = Instantiate(m_ballPrefab, m_spawnPoint.position, Quaternion.identity);
		m_spawnedObjects.Add(m_ball.gameObject);
		m_ball.onCollision += OnCollisionHandler;
		m_ball.onTrigger += OnTriggerHandler;
		m_ball.Drop(velocity);
	}

	private void OnPressedHandler(LaunchButton button)
	{
		button.PlayPressAnimation();

		if (m_launchQueued || m_launchedCar != null)
		{
			return;
		}

		if (!m_waitingForPress)
		{
			m_waitingForCar = false;
			HideLights();
		}

		double elapsed = Time.timeAsDouble - m_signalTime;
		m_shotCanScore = m_waitingForPress && elapsed <= m_reactionWindow && button.colorIndex == m_svetofor.colorIndex;
		m_waitingForPress = false;
		bool earlyPress = m_shotCanScore && m_waitingForDrop && elapsed < m_earlyPressWindow;
		m_waitForTimedLaunch = m_shotCanScore && !earlyPress;
		if (earlyPress)
		{
			m_waitingForDrop = false;
		}

		m_launchQueued = true;
		TryLaunchQueuedCar();
	}

	private void TryLaunchQueuedCar()
	{
		if (!m_launchQueued || (m_waitForTimedLaunch && (m_waitingForDrop || Time.fixedTime < m_launchTime)) || !m_currentCar.TryLaunch())
		{
			return;
		}

		m_launchQueued = false;
		m_launchedCar = m_currentCar;
		SpawnNextCar();
		Destroy(m_launchedCar.gameObject, OldCarLifetime);

		if (m_ball == null && !m_waitingForDrop)
		{
			Vector3 velocity = Vector3.zero;
			if (m_shotCanScore)
			{
				float fallTime = m_launchDelayAfterDrop + m_carTimeToBall;
				float timeFactor = (fallTime * fallTime - m_carTimeToBall * m_carTimeToBall) / (2f * m_carTimeToBall);
				velocity = Physics.gravity * timeFactor;
			}
			SpawnBall(velocity);
		}
	}

	private void OnCollisionHandler(Collider other)
	{
		if (m_launchedCar == null || other.GetComponentInParent<Car>() != m_launchedCar)
		{
			return;
		}

		bool scored = m_shotCanScore;
		m_ball.StopColliding();
		FinishShot(HitBallLifetime);
		if (scored)
		{
			m_hitCount++;
			onBallHit?.Invoke();
		}
		else
		{
			onMissed?.Invoke();
		}
	}

	private void OnTriggerHandler(Collider other)
	{
		if (other == m_groundZone)
		{
			m_waitForTimedLaunch = false;
			TryLaunchQueuedCar();
			FinishShot(0f);
			onMissed?.Invoke();
		}
	}

	private void FinishShot(float ballLifetime)
	{
		Destroy(m_ball.gameObject, ballLifetime);
		StopWatchingBall();
		m_launchedCar = null;
		m_waitingForPress = false;
		m_waitingForDrop = false;
		m_launchQueued = false;
		m_waitForTimedLaunch = false;
		m_shotCanScore = false;
		m_waitingForCar = true;
		HideLights();
	}

	private void HideLights()
	{
		if (m_svetofor != null)
		{
			m_svetofor.Hide();
		}

		if (m_firstButton != null)
		{
			m_firstButton.HideLight();
		}

		if (m_secondButton != null)
		{
			m_secondButton.HideLight();
		}
	}

	private void StopWatchingBall()
	{
		m_ball.onCollision -= OnCollisionHandler;
		m_ball.onTrigger -= OnTriggerHandler;
		m_ball = null;
	}

	private void ShuffleButtonColors()
	{
		int signalColor = m_svetofor.colorIndex;
		int firstColor;
		int secondColor;

		do
		{
			int otherColor = UnityEngine.Random.Range(0, Svetofor.colorCount - 1);
			if (otherColor >= signalColor)
			{
				otherColor++;
			}

			firstColor = signalColor;
			secondColor = otherColor;

			if (UnityEngine.Random.Range(0, 2) == 0)
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
