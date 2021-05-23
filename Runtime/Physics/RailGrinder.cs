using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Component which implements rail grinding
/// </summary>
public class RailGrinder : MonoBehaviour {
	#region Public Properties
	/// <summary>
	/// Gets the rail we're currently grinding on, if any
	/// </summary>
	public Rail activeRail => _activeRail;
	#endregion

	#region Inspector Fields
	[Tooltip("Radius of rail grind detection shape")]
	public float detectionDistance = 0.5f;

	[Tooltip("Local-space offset of rail grind detection shape")]
	public Vector3 detectionOffset = new Vector3(0f, 1f, 0f);

	[Tooltip("Gravity when going uphill")]
	public float gravityUphill = 10f;

	[Tooltip("Gravity when going downhill")]
	public float gravityDownhill = 20f;

	[Tooltip("Amount of momentum lost when switching directions")]
	public float directionSwitchMomentumLoss = 0.1f;

	[Tooltip("Maximum speed player can move along the rail")]
	public float maxSpeed = 40f;

	[Tooltip("Initial boost to give player upon entering rail")]
	public float initialBoost = 20f;
	#endregion

	#region Private Fields
	private Rail _activeRail = null;
	private PlayerPhysics _physics;
	private PlayerAnimationController _animation;
	private Rigidbody _rb;
	private float _forceDetachHack;
	private float _railSpeed;
	private float _facingDir;
	#endregion

	#region Public Methods
	/// <summary>
	/// If grinding, boost along the rail in the direction we're facing
	/// </summary>
	public void Boost(float amount) {
		if(_activeRail != null) {
			if(_railSpeed * _facingDir < 0f) {
				_railSpeed = amount * _facingDir;
			} else {
				_railSpeed += amount * _facingDir;
			}
		}
	}

	/// <summary>
	/// Switch which direction we're facing on the rail
	/// </summary>
	public void Switch() {
		if(_activeRail != null) {
			_facingDir *= -1f;
			_railSpeed *= 1f - directionSwitchMomentumLoss;
		}
	}

	/// <summary>
	/// Force rail grinder to check for rail collisions, returning true if we started grinding
	/// </summary>
	public bool ForceCheckRail() {
		Vector3 c = transform.TransformPoint(detectionOffset);

		foreach(var rail in Rail.rails) {
			Vector3 pt = rail.curve.GetClosestPoint(c, out var rot);
			Vector3 toPt = pt - c;

			if(Vector3.Dot(_rb.velocity, toPt) > 0f && Vector3.Distance(pt, c) <= detectionDistance) {
				_activeRail = rail;
				_physics.LockState();
				_railSpeed = Vector3.Dot(_rb.velocity + (_physics.facingDirection * initialBoost), rot * Vector3.forward);
				_facingDir = Mathf.Sign(_railSpeed);
				_animation.targetAnimator.SetBool("railGrind", true);
				break;
			}
		}

		return _activeRail != null;
	}
	#endregion

	void Awake() {
		_physics = GetComponent<PlayerPhysics>();
		_rb = GetComponent<Rigidbody>();
		_animation = GetComponent<PlayerAnimationController>();
		_physics.onJump.AddListener(OnJump);
	}

	void OnDrawGizmosSelected() {
		Gizmos.DrawWireSphere(transform.TransformPoint(detectionOffset), detectionDistance);
	}

	void OnDestroy() {
		_physics.onJump.RemoveListener(OnJump);
	}

	void Update() {
		if(_activeRail == null) {
			if(_forceDetachHack > 0f) {
				_forceDetachHack -= Time.deltaTime;
			} else if(!_physics.locked) {
				ForceCheckRail();
			}
		} else {
			Vector3 pt = _activeRail.curve.GetClosestPoint(_rb.position, out var rot);
			_rb.MovePosition(pt);
			_rb.velocity = rot * Vector3.forward * _railSpeed;
			_physics.ForceUpAlignment(rot * Vector3.up);
			_physics.RotateTowardsDirection(rot * Vector3.forward * _facingDir, 720f);
			_physics.ForceGrounded(true);
			_physics.ForceRolling(false);

			Vector3 fwd = rot * Vector3.forward;
			float gravity = Vector3.Dot(Vector3.down, fwd);

			// switch gravity depending on whether we're going uphill or downhill
			if(Mathf.Sign(gravity * _railSpeed) < 0f) {
				_railSpeed += gravity * gravityUphill * Time.deltaTime;
			} else {
				_railSpeed += gravity * gravityDownhill * Time.deltaTime;
			}

			_railSpeed = Mathf.Clamp(_railSpeed, -maxSpeed, maxSpeed);

			// exit rail
			if(!_activeRail.curve.Loop) {
				if(_railSpeed > 0f && Vector3.Distance(_rb.position, _activeRail.endNode.transform.position) < 0.1f) {
					Detach();
				} else if(_railSpeed < 0f && Vector3.Distance(_rb.position, _activeRail.startNode.transform.position) < 0.1f) {
					Detach();
				}
			}
		}
	}

	void Detach() {
		_activeRail = null;
		_physics.UnlockState();
		_forceDetachHack = 0.1f;
		_animation.targetAnimator.SetBool("railGrind", false);
	}

	void OnJump() {
		if(_activeRail != null) {
			Detach();
		}
	}
}