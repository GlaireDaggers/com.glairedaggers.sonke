using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implementation of a spin dash action
/// </summary>
public class SpinDashAction : PlayerActionBase {
	#region Inspector Fields
	/// <summary>
	/// Maximum boost which can be applied
	/// </summary>
	[Tooltip("Maximum boost which can be applied")]
	public float maxBoost = 40f;

	/// <summary>
	/// How long player needs to hold button down until maximum boost charge
	/// </summary>
	[Tooltip("How long player needs to hold button down until maximum boost charge")]
	public float chargeRate = 1f;
	#endregion

	#region Private Fields
	private float _charge;
	private bool _charging;
	#endregion

	#region Unity Methods
	private void Update() {
		if(_charging) {
			physics.RotateTowardsInput(physics.rotationRateOverSpeed.Evaluate(0f));
			_charge += Time.deltaTime / chargeRate;
			if(_charge > 1f) _charge = 1f;
		}
	}
	#endregion

	#region Overrides
	public override bool holdRelease => true;

	public override bool IsValid(ActionPhase phase) {
		return base.IsValid(phase) && physics.isGrounded && !physics.locked;
	}

	public override IEnumerator Execute(ActionPhase phase) {
		if(phase == ActionPhase.Enter) {
			physics.LockState();
			physics.ForceRolling(true);
			GetComponent<Rigidbody>().velocity = Vector3.zero;
			_charge = 0f;
			_charging = true;
		} else {
			physics.UnlockState();
			physics.Boost(maxBoost * _charge);
			_charging = false;
		}
		yield break;
	}
	#endregion
}