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
		private const double MinimumReactionMargin = 0.01;

		public event Action onBallHit;
		public event Action onMissed;

		[SerializeField] private Car m_carPrefab;

		[SerializeField] private Ball m_ballPrefab;

		[SerializeField] private Transform m_spawnPoint;

		[SerializeField] private Collider m_groundZone;

		[FormerlySerializedAs("m_svetofor")]
		[SerializeField]
		private TrafficLight m_trafficLight;

		[SerializeField] private LaunchButton m_firstButton;

		[SerializeField] private LaunchButton m_secondButton;

		[FormerlySerializedAs("m_n1")]
		[SerializeField]
		private float m_minSignalDelay = 0.75f;

		[FormerlySerializedAs("m_n2")]
		[SerializeField]
		private float m_maxSignalDelay = 2f;

		[SerializeField]
		[Min(1)]
		private int m_hitsForInstantSignal = 10;

		[SerializeField]
		[Min(0.01f)]
		private float m_minReactionTime = 0.35f;

		[SerializeField]
		[Range(0.01f, 1f)]
		private float m_reactionDifficulty = 0.15f;

		[SerializeField] private float m_signalLeadTime = 0.5f;

		[SerializeField]
		[Min(0f)]
		private float m_launchDelayAfterDrop = 0.62f;

		[SerializeField]
		[Min(0f)]
		private float m_earlyPressWindow = 0.25f;

		[SerializeField]
		[Min(0.01f)]
		private float m_carTimeToBall = 0.76f;

		private readonly List<GameObject> m_spawnedObjects = new();
		private RoundPhase m_phase;
		private Car m_currentCar;
		private Car m_launchedCar;
		private Ball m_ball;
		private bool m_ballDropScheduled;
		private bool m_ballLanded;
		private bool m_ballHitCar;
		private int m_hitCount;
		private double m_signalTime;
		private double m_reactionWindow;
		private float m_dropTime;
		private float m_launchTime;

		public void BeginRound()
		{
			if (m_phase != RoundPhase.Inactive)
			{
				return;
			}

			m_hitCount = 0;
			m_trafficLight.onSignalShown += OnSignalShownHandler;
			m_firstButton.onPressed += OnPressedHandler;
			m_secondButton.onPressed += OnPressedHandler;
			SpawnNextCar();
			m_phase = RoundPhase.WaitingForCar;
		}

		public void EndRound()
		{
			if (m_phase == RoundPhase.Inactive)
			{
				return;
			}

			m_phase = RoundPhase.Inactive;
			m_ballDropScheduled = false;
			m_ballLanded = false;
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
			if (m_phase != RoundPhase.WaitingForCar || !m_currentCar.isReady)
			{
				return;
			}

			m_phase = RoundPhase.WaitingForSignal;
			float delayScale = Mathf.Clamp01(1f - (float)m_hitCount / m_hitsForInstantSignal);
			m_trafficLight.ScheduleSignal(m_minSignalDelay, m_maxSignalDelay, delayScale);
		}

		private void FixedUpdate()
		{
			if (m_ballHitCar && !m_ballLanded)
			{
				FinishShot(HitBallLifetime, true);
				return;
			}

			if (m_ballLanded || (m_ball != null && m_ball.transform.position.y < m_groundZone.bounds.min.y))
			{
				m_ballLanded = true;
				if (m_phase == RoundPhase.LaunchAfterDrop)
				{
					m_phase = RoundPhase.LaunchWhenReady;
					TryLaunchCar();
				}

				if (m_phase == RoundPhase.Launched && Time.fixedTime < m_launchTime + m_carTimeToBall)
				{
					if (m_ball != null)
					{
						m_ball.gameObject.SetActive(false);
					}

					return;
				}

				FinishShot(0f, false);
				return;
			}

			if (m_phase == RoundPhase.Launched && (m_launchedCar == null || m_launchedCar.isLaunchFinished))
			{
				FinishShot(0f, false);
				return;
			}

			if (m_ballDropScheduled && Time.fixedTime >= m_dropTime)
			{
				m_ballDropScheduled = false;
				SpawnBall(Vector3.zero);
				m_launchTime = Time.fixedTime + m_launchDelayAfterDrop;
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
			float dropDelay = Mathf.Max(m_signalLeadTime, m_earlyPressWindow);
			double minimum = Mathf.Max(m_minReactionTime, m_earlyPressWindow);
			double initialWindow = Math.Max(dropDelay + (double)m_launchDelayAfterDrop, minimum + MinimumReactionMargin);
			m_reactionWindow = minimum + (initialWindow - minimum) / (1d + m_hitCount * (double)m_reactionDifficulty);
			m_signalTime = Time.timeAsDouble;
			m_dropTime = Time.time + dropDelay;
			m_ballDropScheduled = true;
			m_phase = RoundPhase.WaitingForPress;
		}

		private void OnPressedHandler(LaunchButton button)
		{
			if (m_phase == RoundPhase.WaitingForPress && button.colorIndex != m_trafficLight.colorIndex)
			{
				return;
			}

			switch (m_phase)
			{
				case RoundPhase.WaitingForCar:
				case RoundPhase.WaitingForSignal:
				{
					HideLights();
					m_phase = RoundPhase.LaunchWhenReady;
					break;
				}
				case RoundPhase.WaitingForPress:
					QueueSignalLaunch();
					break;
				default:
					return;
			}

			button.PlayPressAnimation();
			TryLaunchCar();
		}

		private void OnCollisionHandler(Collider other)
		{
			if (m_launchedCar != null && other.GetComponentInParent<Car>() == m_launchedCar)
			{
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

		private void QueueSignalLaunch()
		{
			double elapsed = Time.timeAsDouble - m_signalTime;
			if (elapsed > m_reactionWindow)
			{
				m_phase = RoundPhase.LaunchWhenReady;
				return;
			}

			if (m_ballDropScheduled && elapsed < m_earlyPressWindow)
			{
				m_ballDropScheduled = false;
				m_phase = RoundPhase.LaunchWhenReady;
				SpawnBall(GetEarlyBallVelocity());
				return;
			}

			m_phase = RoundPhase.LaunchAfterDrop;
		}

		private void TryLaunchCar()
		{
			if (m_phase == RoundPhase.LaunchAfterDrop && !m_ballDropScheduled && Time.fixedTime >= m_launchTime)
			{
				m_phase = RoundPhase.LaunchWhenReady;
			}

			if (m_phase != RoundPhase.LaunchWhenReady || !m_currentCar.TryLaunch())
			{
				return;
			}

			m_phase = RoundPhase.Launched;
			m_launchTime = Time.time;
			m_launchedCar = m_currentCar;
			SpawnNextCar();
			Destroy(m_launchedCar.gameObject, OldCarLifetime);

			if (m_ball == null && !m_ballDropScheduled)
			{
				SpawnBall(Vector3.zero);
			}
		}

		private Vector3 GetEarlyBallVelocity()
		{
			float regularFallTime = m_launchDelayAfterDrop + m_carTimeToBall;
			float timeFactor = (regularFallTime * regularFallTime - m_carTimeToBall * m_carTimeToBall) / (2f * m_carTimeToBall);
			return Physics.gravity * timeFactor;
		}

		private void SpawnNextCar()
		{
			m_spawnedObjects.RemoveAll(item => item == null);
			m_currentCar = Instantiate(m_carPrefab, Vector3.zero, Quaternion.identity);
			m_spawnedObjects.Add(m_currentCar.gameObject);
		}

		private void SpawnBall(Vector3 velocity)
		{
			m_ball = Instantiate(m_ballPrefab, m_spawnPoint.position, Quaternion.identity);
			m_spawnedObjects.Add(m_ball.gameObject);
			m_ball.onCollision += OnCollisionHandler;
			m_ball.onTrigger += OnTriggerHandler;
			m_ball.Drop(velocity);
		}

		private void FinishShot(float ballLifetime, bool scored)
		{
			if (m_ball != null)
			{
				Destroy(m_ball.gameObject, ballLifetime);
				StopWatchingBall();
			}

			m_launchedCar = null;
			m_ballDropScheduled = false;
			m_ballLanded = false;
			m_ballHitCar = false;
			m_phase = RoundPhase.WaitingForCar;
			HideLights();

			if (scored)
			{
				m_hitCount++;
				onBallHit?.Invoke();
				return;
			}

			onMissed?.Invoke();
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
