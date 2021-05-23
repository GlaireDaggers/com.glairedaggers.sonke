using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implementation of a homing attack action
/// </summary>
public class HomingAttackAction : PlayerActionBase {
	#region Public Properties
	/// <summary>
	/// Gets whether the player is currently homing
	/// </summary>
	public bool isActive { get; private set; }
	#endregion

	#region Inspector Fields
	/// <summary>
	/// Speed to move towards targets
	/// </summary>
	[Tooltip("Speed to move towards targets")]
	public float homingSpeed = 20f;

	/// <summary>
	/// Maximum range to consider homing targets within
	/// </summary>
	[Tooltip("Maximum range to consider homing targets within")]
	public float maxAttackRange = 10f;

	/// <summary>
	/// Maximum angle from target direction to consider homing targets within
	/// </summary>
	[Tooltip("Maximum angle from target direction to consider homing targets within")]
	public float maxTargetAngle = 45f;

	/// <summary>
	/// Keep targets around for this long until switching to a new target
	/// </summary>
	[Tooltip("Keep targets around for this long until switching to a new target")]
	public float stickyTimer = 0.1f;

	/// <summary>
	/// If we can't hit the target within this many seconds, give up
	/// </summary>
	[Tooltip("If we can't hit the target within this many seconds, give up")]
	public float giveUpTimer = 3f;

	/// <summary>
	/// Radius to check for collision with target
	/// </summary>
	[Tooltip("Radius to check for collision with target")]
	public float collisionRadius = 0.5f;

	/// <summary>
	/// Local space offset for collision shape
	/// </summary>
	[Tooltip("Local space offset for collision shape")]
	public Vector3 collisionOffset = Vector3.zero;
	#endregion

	#region Private Fields
	private bool _isJumping;
	private List<HomingTarget> _tmpTargets = new List<HomingTarget>();
	private HomingTarget _activeTarget;
	private float _targetTimer = 0f;
	#endregion

	#region Unity Methods
	protected override void Awake() {
		base.Awake();
		physics.onJump.AddListener(OnJump);
	}

	private void LateUpdate() {
		if(physics.isGrounded) {
			_isJumping = false;
		}

		if(_activeTarget == null || _targetTimer <= 0f) {
			_targetTimer = stickyTimer;
			_activeTarget = GetHomingTarget();
		} else {
			_targetTimer -= Time.deltaTime;
		}
	}

	void OnDrawGizmos() {
		if(Application.isPlaying) {
			if(_activeTarget != null) {
				Gizmos.color = Color.red;
				Gizmos.DrawWireSphere(GetTargetPos(_activeTarget), 1f);
			}
		}

		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.TransformPoint(collisionOffset), collisionRadius);
	}

	private void OnDestroy() {
		physics.onJump.RemoveListener(OnJump);
	}
	#endregion

	#region Overrides
	public override bool IsValid(ActionPhase phase) {
		return base.IsValid(phase) && _isJumping && _activeTarget != null && phase == ActionPhase.Enter && !physics.locked;
	}

	public override IEnumerator Execute(ActionPhase phase) {
		isActive = true;
		physics.LockState();
		animator.targetAnimator.SetBool("homing", true);

		// start moving towards the target, wait until either timeout or collision with target
		var target = _activeTarget;
		float timer = giveUpTimer;
		var targetPos = GetTargetPos(_activeTarget);

		var rb = GetComponent<Rigidbody>();

		while(timer > 0f && target != null) {
			timer -= Time.deltaTime;

			if(!target.lockPosition) {
				targetPos = GetTargetPos(target);
			}

			Vector3 myPos = transform.TransformPoint(collisionOffset);

			if(Vector3.Distance(myPos, targetPos) <= collisionRadius) {
				break;
			}

			rb.velocity = (targetPos - myPos).normalized * homingSpeed;
			yield return null;
		}

		animator.targetAnimator.SetBool("homing", false);
		physics.UnlockState();
		isActive = false;
	}
	#endregion

	#region Private Methods
	private void OnJump() {
		_isJumping = true;
	}

	private Vector3 GetTargetPos(HomingTarget target) {
		if(target.predictAhead) {
			return target.GetHomingLocation(transform.position + (GetComponent<Rigidbody>().velocity * 0.1f));
		} else {
			return target.GetHomingLocation(transform.position);
		}
	}

	private HomingTarget GetHomingTarget() {
		Vector3 inputDir = new Vector3(physics.InputMoveAxes.x, 0f, physics.InputMoveAxes.y);
		Vector3 targetFwd = transform.TransformDirection(inputDir);

		// if player isn't touching control stick, use fallback forward vector
		if(targetFwd.sqrMagnitude < Mathf.Epsilon) {
			targetFwd = physics.facingDirection;
		}

		Debug.DrawRay(transform.position, targetFwd, Color.green);

		// grab a list of all homing attack targets in range
		_tmpTargets.Clear();
		HomingTarget.GetTargets(transform.position, targetFwd, maxAttackRange, maxTargetAngle, _tmpTargets);

		// pick target with least distance*angle
		float lowestScore = Mathf.Infinity;
		HomingTarget bestTarget = null;
		foreach(var target in _tmpTargets) {
			Vector3 targetPos = target.GetHomingLocation(transform.position);
			float score = Vector3.Distance(targetPos, transform.position) *
				(Vector3.Angle(targetFwd, targetPos - transform.position) / 90f);
			if(score < lowestScore) {
				bestTarget = target;
				lowestScore = score;
			}
		}

		return bestTarget;
	}
	#endregion
}