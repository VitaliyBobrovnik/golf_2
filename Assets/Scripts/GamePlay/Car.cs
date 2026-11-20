using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Golf.GamePlay
{
	[MovedFrom(true, sourceNamespace: "")]
	public class Car : MonoBehaviour
	{
		private const string ApproachClipName = "CarApproach";
		private const string LaunchClipName = "CarLaunch";

		public bool isReady => isActiveAndEnabled && !isLaunched
			&& m_approachState != null && m_approachState.enabled
			&& m_approachState.time >= m_approachState.length;

		public bool isLaunched { get; private set; }

		public bool isLaunchFinished => isLaunched && m_launchState.time >= m_launchState.length;

		[SerializeField]
		private Animation m_animation;

		[SerializeField]
		private AnimationClip m_approachClip;

		[SerializeField]
		private AnimationClip m_launchClip;

		private AnimationState m_approachState;
		private AnimationState m_launchState;

		public bool TryLaunch()
		{
			if (!isReady || !m_animation.Play(LaunchClipName))
			{
				return false;
			}

			isLaunched = true;
			m_animation.Sample();
			return true;
		}

		public void DisableCollisions()
		{
			foreach (Collider collider in GetComponentsInChildren<Collider>(true))
			{
				collider.enabled = false;
			}
		}

		private void Awake()
		{
			m_animation.playAutomatically = false;
			m_animation.cullingType = AnimationCullingType.AlwaysAnimate;
			m_animation.updateMode = AnimationUpdateMode.Fixed;
			m_animation.AddClip(m_approachClip, ApproachClipName);
			m_animation.AddClip(m_launchClip, LaunchClipName);

			m_approachState = m_animation[ApproachClipName];
			m_launchState = m_animation[LaunchClipName];
			m_approachState.wrapMode = WrapMode.ClampForever;
			m_launchState.wrapMode = WrapMode.ClampForever;

			if (m_animation.Play(ApproachClipName))
			{
				m_animation.Sample();
			}
		}
	}
}
