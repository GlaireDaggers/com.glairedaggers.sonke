using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implementation of a bounce action
/// </summary>
public class BounceAction : PlayerActionBase {
	#region Inspector Fields
	/// <summary>
	/// How fast the player should move down
	/// </summary>
	[Tooltip("How fast the player should move down")]
	public float downForce = 40f;

	/// <summary>
	/// How much velocity is retained after bounce
	/// </summary>
	[Tooltip("How much velocity is retained after bounce")]
	public float bounciness = 0.5f;

	/// <summary>
	/// How much bounciness increases with each bounce
	/// </summary>
	[Tooltip("How much bounciness increases with each bounce")]
	public float bounceBoost = 0.1f;

	/// <summary>
	/// Maximum amount bounciness can increase to
	/// </summary>
	[Tooltip("Maximum amount bounciness can increase to")]
	public float bounceCap = 1f;
	#endregion

	#region Private Fields
	private float _bounciness = 0f;
	private Vector3? _collisionNormal;
	private RailGrinder _railGrinder;
	private bool _abort;
	#endregion

	#region Unity Methods
	void Update() {
		// if player ever just lands on the ground without bouncing, reset bounciness
		if(physics.isGrounded) {
			_bounciness = bounciness;
		}
	}

	void OnCollisionEnter(Collision collision) {
		_collisionNormal = collision.GetContact(0).normal;
	}

	void OnDestroy() {
		physics.onBounce.RemoveListener(OnBounce);
	}
	#endregion

	#region Overrides
	protected override void Awake() {
		base.Awake();
		_bounciness = bounciness;
		_railGrinder = GetComponent<RailGrinder>();
		physics.onBounce.AddListener(OnBounce);
	}

	public override bool IsValid(ActionPhase phase) {
		return base.IsValid(phase) && phase == ActionPhase.Enter && !physics.isGrounded && !physics.locked;
	}

	public override IEnumerator Execute(ActionPhase phase) {
		// lock physics, force rigidbody straight down, wait for collision to bounce back up
		var rb = GetComponent<Rigidbody>();
		physics.LockState();
		animator.targetAnimator.SetBool("bouncing", true);
		rb.velocity = Vector3.down * downForce;
		_collisionNormal = null;
		_abort = false;

		while(_collisionNormal == null) {
			// HACK: manually check for rail collisions and abort if one is found
			if(_railGrinder?.ForceCheckRail() ?? false) {
				yield break;
			}

			if(_abort) {
				physics.UnlockState();
				yield break;
			}

			yield return null;
		}

		animator.targetAnimator.SetBool("bouncing", false);
		physics.UnlockState();

		if(_collisionNormal.HasValue) {
			physics.Bounce(_collisionNormal.Value * downForce * _bounciness, 0f);
		}

		_collisionNormal = null;
		_bounciness += bounceBoost;

		if(_bounciness > bounceCap) {
			_bounciness = bounceCap;
		}

		yield break;
	}
	#endregion

	#region Private Methods
	private void OnBounce() {
		// HACK: if we hit a spring, abort bounce
		_abort = true;
	}
	#endregion
}