using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>
/// Reads state from PlayerController and drives an Animator.
/// Uses the Playables API to mix a dash animation on top of standard locomotion without complex Animator transitions.
/// </summary>
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    private PlayerController _controller;
    private Animator         _animator;

    private static readonly int SpeedHash     = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash= Animator.StringToHash("IsGrounded");
    
    // Playables API for custom blending
    private PlayableGraph          _graph;
    private AnimationMixerPlayable _mixer;
    private AnimationClipPlayable  _basePlayable;
    private AnimationClipPlayable  _dashPlayable;

    [Header("Clips (For Playables fallback)")]
    public AnimationClip BaseLocomotionClip;
    public AnimationClip DashClip;

    private bool _usePlayables = false;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _animator   = GetComponent<Animator>();

        // If Playables setup is desired, uncomment InitializePlayables()
        // InitializePlayables();
    }

    private void InitializePlayables()
    {
        if (BaseLocomotionClip == null || DashClip == null) return;

        _usePlayables = true;
        _graph = PlayableGraph.Create("PlayerAnimatorGraph");
        _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        // Output definition
        var animOutput = AnimationPlayableOutput.Create(_graph, "Animation", _animator);

        // Create mixer with 2 inputs: 0 = Locomotion (managed by normal Animator or BaseClip), 1 = Dash
        _mixer = AnimationMixerPlayable.Create(_graph, 2);
        animOutput.SetSourcePlayable(_mixer);

        _basePlayable = AnimationClipPlayable.Create(_graph, BaseLocomotionClip);
        _dashPlayable = AnimationClipPlayable.Create(_graph, DashClip);

        _graph.Connect(_basePlayable, 0, _mixer, 0);
        _graph.Connect(_dashPlayable, 0, _mixer, 1);

        _mixer.SetInputWeight(0, 1f);
        _mixer.SetInputWeight(1, 0f);

        _graph.Play();
    }

    private void LateUpdate()
    {
        // Standard Animator parameters
        _animator.SetFloat(SpeedHash, _controller.IsMoving ? 1f : 0f);
        _animator.SetBool(IsGroundedHash, _controller.IsGrounded);

        // Playables Dash Override
        if (_usePlayables)
        {
            float targetDashWeight = _controller.IsDashing ? 1f : 0f;
            _mixer.SetInputWeight(1, Mathf.MoveTowards(_mixer.GetInputWeight(1), targetDashWeight, Time.deltaTime * 10f));
            _mixer.SetInputWeight(0, 1f - _mixer.GetInputWeight(1));
        }
        else
        {
            // Standard Animator fallback
            _animator.SetBool("IsDashing", _controller.IsDashing);
        }
    }

    private void OnDestroy()
    {
        if (_usePlayables && _graph.IsValid())
        {
            _graph.Destroy();
        }
    }
}
