using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class responsible for translating physics behavior into animation data
/// </summary>
[RequireComponent(typeof(PlayerPhysics))]
public class PlayerAnimationController : MonoBehaviour {
	#region Constants
	/// <summary> Property name for run speed (float) </summary>
	public const string RUN_SPEED_PROP = "runSpeed";

	/// <summary> Property name for tilt direction (float) </summary>
	public const string RUN_TILT_PROP = "runTilt";

	/// <summary> Property name for falling (bool) </summary>
	public const string FALLING_PROP = "falling";

	/// <summary> Property name for jumping (bool) </summary>
	public const string JUMPING_PROP = "jumping";

	/// <summary> Property name for braking (bool) </summary>
	public const string BRAKING_PROP = "braking";

	/// <summary> Property name for rolling (bool) </summary>
	public const string ROLLING_PROP = "rolling";

	private static readonly int RUN_SPEED_PROP_ID = Animator.StringToHash(RUN_SPEED_PROP);
	private static readonly int RUN_TILT_PROP_ID = Animator.StringToHash(RUN_TILT_PROP);
	private static readonly int FALLING_PROP_ID = Animator.StringToHash(FALLING_PROP);
	private static readonly int JUMPING_PROP_ID = Animator.StringToHash(JUMPING_PROP);
	private static readonly int BRAKING_PROP_ID = Animator.StringToHash(BRAKING_PROP);
	private static readonly int ROLLING_PROP_ID = Animator.StringToHash(ROLLING_PROP);
	#endregion

	#region Inspector Fields
	/// <summary> Target animator to supply animation data to </summary>
	[Tooltip("Target animator to supply animation data to")]
	public Animator targetAnimator;
	#endregion

	#region Private Fields
	private PlayerPhysics _physics;
	private Rigidbody _rb;
	private bool _jumping;
	private bool _braking;
	#endregion

	#region Unity Methods
	private void Awake() {
		_physics = GetComponent<PlayerPhysics>();
		_rb = GetComponent<Rigidbody>();

		_physics.onJump.AddListener(OnJump);
	}

	private void Update() {
		Vector3 localSpeed = transform.InverseTransformDirection(_rb.velocity);
		localSpeed.y = 0f;

		Vector3 input = new Vector3(_physics.InputMoveAxes.x, 0f, _physics.InputMoveAxes.y);

		if(_rb.velocity.y < 0f || _physics.isGrounded) {
			_jumping = false;
		}

		float speedMag = localSpeed.magnitude;
		float normalizedSpeed = speedMag / _physics.maxSpeed;

		// calculate a signed tilt amount from -1..1 based on difference between actual velocity and input direction
		float tilt = 0f;

		if(speedMag > Mathf.Epsilon) {
			Vector3 fwd = localSpeed / speedMag;
			tilt = Vector3.SignedAngle(fwd, input, transform.up) / 90f;
		}

		targetAnimator.SetFloat(RUN_SPEED_PROP_ID, normalizedSpeed);
		targetAnimator.SetFloat(RUN_TILT_PROP_ID, tilt);
		targetAnimator.SetBool(FALLING_PROP_ID, !_physics.isGrounded);
		targetAnimator.SetBool(JUMPING_PROP_ID, _jumping);
		targetAnimator.SetBool(BRAKING_PROP_ID, _braking);
		targetAnimator.SetBool(ROLLING_PROP_ID, _physics.isRolling);
	}

	private void OnDestroy() {
		_physics.onJump.RemoveListener(OnJump);
	}
	#endregion

	#region Private Methods
	private void OnJump() {
		_jumping = true;
	}

	private void OnBrakeBegin() {
		_braking = true;
	}

	private void OnBrakeEnd() {
		_braking = false;
	}
	#endregion
}