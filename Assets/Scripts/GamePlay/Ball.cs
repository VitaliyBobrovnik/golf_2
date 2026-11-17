using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Golf.GamePlay
{
	[MovedFrom(true, sourceNamespace: "")]
	public class Ball : MonoBehaviour
	{
		public event Action<Collider> onCollision;
		public event Action<Collider> onTrigger;

		private Rigidbody m_body;

		public void Drop(Vector3 velocity)
		{
			m_body.isKinematic = false;
			m_body.useGravity = true;
			m_body.linearVelocity = velocity;
		}

		private void Awake()
		{
			m_body = GetComponent<Rigidbody>();
			m_body.isKinematic = true;
			m_body.useGravity = false;
		}

		private void OnCollisionEnter(Collision collision)
		{
			if (!m_body.isKinematic)
			{
				onCollision?.Invoke(collision.collider);
			}
		}

		private void OnTriggerEnter(Collider other)
		{
			if (!m_body.isKinematic)
			{
				onTrigger?.Invoke(other);
			}
		}
	}
}
