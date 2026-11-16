using System;
using UnityEngine;

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

    public void StopColliding()
    {
        m_body.detectCollisions = false;
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
