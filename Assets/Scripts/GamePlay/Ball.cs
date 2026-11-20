using System;
using TMPro;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Golf.GamePlay
{
	public enum BallEffect
	{
		Add,
		Multiply,
		Subtract
	}

	[Serializable]
	public class BallSettings
	{
		public BallEffect effect;
		[Min(1)] public int value = 1;
		public Color color = Color.white;
	}

	[MovedFrom(true, sourceNamespace: "")]
	public class Ball : MonoBehaviour
	{
		public event Action<Collision> onCollision;
		public event Action<Collider> onTrigger;

		public BallSettings settings { get; private set; }
		public bool costsLife => settings.effect != BallEffect.Subtract;

		[SerializeField] private TMP_Text m_label;

		private Rigidbody m_body;
		private Collider m_collider;
		private Camera m_camera;
		private Vector3 m_acceleration;
		private Vector3 m_incomingVelocity;
		private Vector3 m_reboundVelocity;
		private bool m_isFlying;

		public void Configure(BallSettings settings)
		{
			this.settings = settings;

			string prefix = "+";
			if (settings.effect == BallEffect.Multiply)
			{
				prefix = "×";
			}
			else if (settings.effect == BallEffect.Subtract)
			{
				prefix = "−";
			}

			if (m_label != null)
			{
				m_label.text = prefix + settings.value;
				m_label.color = settings.color;
			}
		}

		public void Launch(Vector3 target, float ringHeight, float timeToRing, float flightTime)
		{
			Vector3 start = m_body.position;
			float straightHeight = Mathf.Lerp(start.y, target.y, timeToRing / flightTime);
			float verticalAcceleration = -2f * (ringHeight - straightHeight) / (timeToRing * (flightTime - timeToRing));
			m_acceleration = Vector3.up * verticalAcceleration;
			m_body.isKinematic = false;
			m_body.linearVelocity = (target - start) / flightTime
				- m_acceleration * (flightTime + Time.fixedDeltaTime) * 0.5f;
			m_isFlying = true;
		}

		public void SetCarCollision(Car car, bool enabled)
		{
			foreach (Collider collider in car.GetComponentsInChildren<Collider>())
			{
				Physics.IgnoreCollision(m_collider, collider, !enabled);
			}
		}

		public void PrepareDeflection(Vector3 normal)
		{
			m_reboundVelocity = Vector3.Reflect(m_incomingVelocity, normal) * 1.25f;
			m_reboundVelocity.y = Mathf.Max(18f, m_reboundVelocity.y);
		}

		public void Deflect()
		{
			StopArc();
			m_body.linearVelocity = m_reboundVelocity;
		}

		public void DisableCollisions()
		{
			m_collider.enabled = false;
		}

		public void StopArc()
		{
			m_isFlying = false;
			m_body.useGravity = true;
		}

		private void Awake()
		{
			m_body = GetComponent<Rigidbody>();
			m_collider = GetComponent<Collider>();
			m_camera = Camera.main;
			m_body.useGravity = false;
		}

		private void FixedUpdate()
		{
			if (m_isFlying)
			{
				m_incomingVelocity = m_body.linearVelocity;
				m_body.AddForce(m_acceleration, ForceMode.Acceleration);
			}
		}

		private void LateUpdate()
		{
			if (m_label != null && m_label.isActiveAndEnabled && m_camera != null)
			{
				m_label.transform.rotation = m_camera.transform.rotation;
			}
		}

		private void OnCollisionEnter(Collision collision)
		{
			onCollision?.Invoke(collision);
		}

		private void OnTriggerEnter(Collider other)
		{
			onTrigger?.Invoke(other);
		}
	}
}
