using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implementation of a jump dash action
/// </summary>
public class JumpDashAction : PlayerActionBase {
	#region Public Properties
	/// <summary>
	/// Gets whether the player is currently dashing
	/// </summary>
	public bool isActive { get; private set; }
	#endregion

	#region Inspector Fields
	/// <summary>
	/// Speed to move forwards
	/// </summary>
	[Tooltip("Speed to move forwards")]
	public float dashSpeed = 20f;

	/// <summary>
	/// How long the dash lasts
	/// </summary>
	[Tooltip("How long the dash lasts")]
	public float duration = 0.5f;

	/// <summary>
	/// How fast to rotate towards the input direction
	/// </summary>
	[Tooltip("How fast to rotate towards the input direction")]
	public float rotationSpeed = 360f;

	/// <summary>
	/// How much gravity to apply while dashing
	/// </summary>
	[Tooltip("How much gravity to apply while dashing")]
	public float gravity = 0f;

	/// <summary>
	/// Effect to play when dashing
	/// </summary>
	[Tooltip("Effect to play when dashing")]
	public ParticleSystem effect;
	#endregion

	#region Private Fields
	private bool _isJumping;
	private bool _isConsumed;
	private bool _collided;
	#endregion

	#region Unity Methods
	protected override void Awake() {
		base.Awake();
		physics.onJump.AddListener(OnJump);
	}

	private void LateUpdate() {
		if(physics.isGrounded) {
			_isJumping = false;
			_isConsumed = false;
		}
	}

	private void OnDestroy() {
		physics.onJump.RemoveListener(OnJump);
	}
	#endregion

	#region Overrides
	public override bool IsValid(ActionPhase phase) {
		return base.IsValid(phase) && _isJumping && !_isConsumed && phase == ActionPhase.Enter && !physics.locked;
	}

	public override IEnumerator Execute(ActionPhase phase) {
		var emit = effect.emission;
		emit.enabled = true;

		isActive = true;
		_isConsumed = true;
		physics.LockState();
		animator.targetAnimator.SetBool("jumpDash", true);

		float timer = duration;
		var rb = GetComponent<Rigidbody>();

		rb.velocity = Vector3.zero;
		_collided = false;

		while(timer > 0f && !_collided) {
			timer -= Time.deltaTime;
			physics.RotateTowardsInput(rotationSpeed);

			Vector3 targetV = physics.facingDirection * dashSpeed;
			targetV.y = rb.velocity.y - (gravity * Time.deltaTime);

			rb.velocity = targetV;
			yield return null;
		}

		animator.targetAnimator.SetBool("jumpDash", false);
		physics.UnlockState();
		isActive = false;
		emit.enabled = false;
	}
	#endregion

	#region Private Methods
	private void OnJump() {
		_isJumping = true;
	}

	private void OnCollisionEnter(Collision collision) {
		_collided = true;
	}
	#endregion
}