using UnityEngine;

public class Car : MonoBehaviour
{
    private const string ApproachClipName = "CarApproach";
    private const string LaunchClipName = "CarLaunch";

    public bool isReady => isActiveAndEnabled && !isLaunched
        && m_approachState != null && m_approachState.enabled
        && m_approachState.time >= m_approachState.length;

    public bool isLaunched { get; private set; }

    [SerializeField]
    private Animation m_animation;

    [SerializeField]
    private AnimationClip m_approachClip;

    [SerializeField]
    private AnimationClip m_launchClip;

    private AnimationState m_approachState;

    public void PlayApproachAnimation()
    {
        if (m_approachState == null || isLaunched)
        {
            return;
        }

        m_animation.Rewind(ApproachClipName);
        if (m_animation.Play(ApproachClipName))
        {
            m_animation.Sample();
        }
    }

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

    private void Awake()
    {
        m_animation.playAutomatically = false;
        m_animation.cullingType = AnimationCullingType.AlwaysAnimate;
        m_animation.AddClip(m_approachClip, ApproachClipName);
        m_animation.AddClip(m_launchClip, LaunchClipName);

        m_approachState = m_animation[ApproachClipName];
        m_approachState.wrapMode = WrapMode.ClampForever;
        m_animation[LaunchClipName].wrapMode = WrapMode.ClampForever;

        PlayApproachAnimation();
    }
}
