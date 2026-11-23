using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace Golf.GamePlay
{
	[MovedFrom(true, sourceNamespace: "")]
	public class GameRound : MonoBehaviour
	{
		private const float OldCarLifetime = 5f;
		private const float HitBallLifetime = 2f;
		private const float TableBounceTime = 0.4f;
		private const double MinimumBallSpawnInterval = 0.5;

		public event Action<BallStats> onBallHit;
		public event Action onMissed;

		[SerializeField] private Car m_carPrefab;
		[SerializeField] private Ball m_ballPrefab;
		[SerializeField] private Transform m_spawnPoint;
		[SerializeField] private Collider m_groundZone;
		[SerializeField] private Collider m_tableSurface;
		[SerializeField] private Transform m_flightRingCenter;

		[FormerlySerializedAs("m_svetofor")]
		[SerializeField] private TrafficLight m_trafficLight;

		[SerializeField] private LaunchButton m_firstButton;
		[SerializeField] private LaunchButton m_secondButton;
		[SerializeField] private GameAudio m_audio;

		[FormerlySerializedAs("m_n1")]
		[SerializeField] private float m_minSignalDelay = 0.75f;

		[FormerlySerializedAs("m_n2")]
		[SerializeField] private float m_maxSignalDelay = 2f;

		[SerializeField, Min(1)] private int m_hitsForInstantSignal = 10;
		[SerializeField, Min(0.35f)] private float m_initialTimeToRing = 1f;
		[SerializeField, Min(0.35f)] private float m_minTimeToRing = 0.8f;
		[FormerlySerializedAs("m_reactionDifficulty")]
		[SerializeField, Range(0.01f, 1f)] private float m_ballSpeedProgression = 0.15f;
		[SerializeField] private Vector3 m_ballTarget;

		[SerializeField] private BallStats[] m_ballTypes;

		private readonly List<GameObject> m_spawnedObjects = new();
		private RoundPhase m_phase;
		private Car m_currentCar;
		private Car m_launchedCar;
		private Ball m_ball;
		private bool m_ballLanded;
		private bool m_ballHitCar;
		private int m_scoredHitCount;
		private double m_nextBallSpawnTime;
		private float m_ballArrivalTime;
		private float m_ballExpiresAt;
		private float m_tableBounceEndsAt;
		private float m_showColorsAt;

		public void BeginRound()
		{
			if (m_phase != RoundPhase.Inactive)
			{
				return;
			}

			m_scoredHitCount = 0;
			m_trafficLight.onSignalShown += OnSignalShownHandler;
			m_firstButton.onPressed += OnPressedHandler;
			m_secondButton.onPressed += OnPressedHandler;
			SpawnNextCar();
			m_phase = RoundPhase.WaitingForCar;
		}

		public void RecordScoredHit()
		{
			m_scoredHitCount++;
		}

		public void EndRound()
		{
			if (m_phase == RoundPhase.Inactive)
			{
				return;
			}

			m_phase = RoundPhase.Inactive;
			m_ballLanded = false;
			m_tableBounceEndsAt = 0f;
			m_ballHitCar = false;
			m_trafficLight.onSignalShown -= OnSignalShownHandler;
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
			if (m_phase == RoundPhase.RerollingColors)
			{
				if (Time.time >= m_showColorsAt && m_tableBounceEndsAt <= 0f && !m_ballLanded)
				{
					m_trafficLight.RerollSignal();
				}
				return;
			}

			if (m_phase != RoundPhase.WaitingForCar || Time.timeAsDouble < m_nextBallSpawnTime || !m_currentCar.isReady)
			{
				return;
			}

			m_phase = RoundPhase.WaitingForSignal;
			float delayScale = Mathf.Clamp01(1f - (float)m_scoredHitCount / m_hitsForInstantSignal);
			m_trafficLight.ScheduleSignal(m_minSignalDelay, m_maxSignalDelay, delayScale);
		}

		private void FixedUpdate()
		{
			if (m_ball != null)
			{
				if (m_tableBounceEndsAt > 0f)
				{
					if (Time.fixedTime >= m_tableBounceEndsAt)
					{
						FinishShot(false);
					}
					return;
				}

				if (m_ballLanded || m_ball.transform.position.y < m_groundZone.bounds.min.y)
				{
					FinishShot(false);
					return;
				}

				if (m_ballHitCar)
				{
					FinishShot(true);
					return;
				}

				bool jumpFinished = m_phase == RoundPhase.Launched
					&& (m_launchedCar == null || m_launchedCar.isLaunchFinished);
				if (Time.fixedTime >= m_ballExpiresAt
					|| (jumpFinished && Time.fixedTime >= m_ballArrivalTime))
				{
					FinishShot(false);
					return;
				}
			}

			TryLaunchCar();
		}

		private void OnDestroy()
		{
			EndRound();
		}

		private void OnSignalShownHandler()
		{
			ShuffleButtonColors();
			m_audio?.PlaySignal();
			if (m_phase == RoundPhase.WaitingForSignal)
			{
				SpawnBall();
			}
			m_phase = RoundPhase.WaitingForPress;
		}

		private void OnPressedHandler(LaunchButton button)
		{
			if (m_tableBounceEndsAt > 0f)
			{
				return;
			}

			if (m_phase == RoundPhase.WaitingForPress && button.colorIndex != m_trafficLight.colorIndex)
			{
				button.PlayPressAnimation();
				m_audio?.PlayWrongButtonPress();
				HideLights();
				m_showColorsAt = Time.time + LightColor.FadeTime;
				m_phase = RoundPhase.RerollingColors;
				return;
			}

			if (m_phase != RoundPhase.WaitingForCar
				&& m_phase != RoundPhase.WaitingForSignal
				&& m_phase != RoundPhase.WaitingForPress)
			{
				return;
			}

			if (m_phase != RoundPhase.WaitingForPress)
			{
				HideLights();
			}

			m_phase = RoundPhase.LaunchWhenReady;
			button.PlayPressAnimation();
			m_audio?.PlayButtonPress();
			TryLaunchCar();
		}

		private void TryLaunchCar()
		{
			if (m_phase != RoundPhase.LaunchWhenReady
				|| (m_ball == null && Time.timeAsDouble < m_nextBallSpawnTime)
				|| !m_currentCar.TryLaunch())
			{
				return;
			}

			if (m_ball == null)
			{
				SpawnBall();
			}

			m_phase = RoundPhase.Launched;
			m_launchedCar = m_currentCar;
			m_ball.SetCarCollision(m_launchedCar, true);
			SpawnNextCar();
			m_ball.SetCarCollision(m_currentCar, false);
			Destroy(m_launchedCar.gameObject, OldCarLifetime);
		}

		private void OnCollisionHandler(Collision collision)
		{
			m_audio?.PlayBallHit();
			if (collision.collider == m_tableSurface)
			{
				if (m_tableBounceEndsAt <= 0f)
				{
					m_ball.StopArc();
					m_tableBounceEndsAt = Time.fixedTime + TableBounceTime;
					HideLights();
				}
				return;
			}

			if (!m_ballHitCar && !m_ballLanded && m_tableBounceEndsAt <= 0f
				&& m_launchedCar != null && collision.collider.GetComponentInParent<Car>() == m_launchedCar)
			{
				m_ball.PrepareDeflection(collision.GetContact(0).normal);
				m_ballHitCar = true;
			}
		}

		private void OnTriggerHandler(Collider other)
		{
			if (other == m_groundZone)
			{
				m_ballLanded = true;
			}
		}

		private void SpawnNextCar()
		{
			m_spawnedObjects.RemoveAll(item => item == null);
			m_currentCar = Instantiate(m_carPrefab, Vector3.zero, Quaternion.identity);
			m_spawnedObjects.Add(m_currentCar.gameObject);
			m_audio?.PlayCarApproach();
		}

		private void SpawnBall()
		{
			BallStats stats = PickBallStats();
			Vector3 ringCenter = m_flightRingCenter.position;
			Vector3 ringToTarget = Vector3.ProjectOnPlane(m_ballTarget - ringCenter, Vector3.up);
			Vector3 direction = ringToTarget.normalized;
			float distanceToRing = Mathf.Max(1f, Vector3.Dot(ringCenter - m_spawnPoint.position, direction));
			Vector3 start = ringCenter - direction * distanceToRing;
			start.y = m_spawnPoint.position.y;

			float initialSpeed = distanceToRing / m_initialTimeToRing;
			float maximumSpeed = distanceToRing / Mathf.Min(m_minTimeToRing, m_initialTimeToRing);
			float progress = 1f - 1f / (1f + m_scoredHitCount * m_ballSpeedProgression);
			float speed = Mathf.Lerp(initialSpeed, maximumSpeed, progress) * stats.speedMultiplier;

			float timeToRing = distanceToRing / speed;
			float flightTime = (distanceToRing + ringToTarget.magnitude) / speed;

			m_nextBallSpawnTime = Time.timeAsDouble + MinimumBallSpawnInterval;
			m_ball = Instantiate(m_ballPrefab, start, Quaternion.identity);
			m_ball.Configure(stats);
			m_ball.SetCarCollision(m_currentCar, false);

			m_spawnedObjects.Add(m_ball.gameObject);
			m_ball.onCollision += OnCollisionHandler;
			m_ball.onTrigger += OnTriggerHandler;
			m_ball.Launch(m_ballTarget, ringCenter.y, timeToRing, flightTime);
			m_ballArrivalTime = Time.time + flightTime;
			m_ballExpiresAt = m_ballArrivalTime + 4f;
		}

		private BallStats PickBallStats()
		{
			int totalWeight = 0;
			foreach (BallStats stats in m_ballTypes)
			{
				totalWeight += Mathf.Max(1, stats.spawnWeight);
			}

			int roll = UnityEngine.Random.Range(0, totalWeight);
			foreach (BallStats stats in m_ballTypes)
			{
				roll -= Mathf.Max(1, stats.spawnWeight);
				if (roll < 0)
				{
					return stats;
				}
			}

			return m_ballTypes[0];
		}

		private void FinishShot(bool hit)
		{
			BallStats stats = m_ball.stats;
			bool costsLife = m_ball.costsLife;
			if (hit)
			{
				m_ball.Deflect();
			}

			m_ball.DisableCollisions();
			if (m_launchedCar != null)
			{
				m_launchedCar.DisableCollisions();
			}

			float ballLifetime = hit ? HitBallLifetime : (m_tableBounceEndsAt > 0f ? 0.3f : 0f);
			Destroy(m_ball.gameObject, ballLifetime);
			StopWatchingBall();
			m_launchedCar = null;
			m_ballLanded = false;
			m_tableBounceEndsAt = 0f;
			m_ballHitCar = false;
			m_phase = RoundPhase.WaitingForCar;
			HideLights();

			if (hit)
			{
				onBallHit?.Invoke(stats);
			}
			else if (costsLife)
			{
				onMissed?.Invoke();
			}
		}

		private void StopWatchingBall()
		{
			m_ball.onCollision -= OnCollisionHandler;
			m_ball.onTrigger -= OnTriggerHandler;
			m_ball = null;
		}

		private void HideLights()
		{
			if (m_trafficLight != null)
			{
				m_trafficLight.Hide();
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

		private void ShuffleButtonColors()
		{
			int signalColor = m_trafficLight.colorIndex;
			int firstColor;
			int secondColor;

			do
			{
				int otherColor = UnityEngine.Random.Range(0, TrafficLight.colorCount - 1);
				if (otherColor >= signalColor)
				{
					otherColor++;
				}

				bool signalOnFirstButton = UnityEngine.Random.Range(0, 2) != 0;
				firstColor = signalOnFirstButton ? signalColor : otherColor;
				secondColor = signalOnFirstButton ? otherColor : signalColor;
			}
			while (firstColor == m_firstButton.colorIndex && secondColor == m_secondButton.colorIndex);

			m_firstButton.SetColor(firstColor, TrafficLight.GetColor(firstColor));
			m_secondButton.SetColor(secondColor, TrafficLight.GetColor(secondColor));
		}
	}
}
