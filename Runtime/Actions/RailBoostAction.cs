using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implementation of a rail boost action
/// </summary>
[RequireComponent(typeof(RailGrinder))]
public class RailBoostAction : PlayerActionBase {
	#region Inspector Fields
	/// <summary>
	/// Amount to boost
	/// </summary>
	[Tooltip("Amount to boost")]
	public float boostAmount = 10f;

	/// <summary>
	/// Minimum time between boosts
	/// </summary>
	[Tooltip("Minimum time between boosts")]
	public float cooldown = 0.25f;
	#endregion

	#region Private Fields
	private RailGrinder _grind;
	private float _cool;
	#endregion

	#region Overrides
	protected override void Awake() {
		base.Awake();
		_grind = GetComponent<RailGrinder>();
	}

	public override bool IsValid(ActionPhase phase) {
		return base.IsValid(phase) && _grind.activeRail != null && phase == ActionPhase.Enter && _cool <= 0f;
	}

	public override IEnumerator Execute(ActionPhase phase) {
		animator.targetAnimator.SetTrigger("railBoost");
		_grind.Boost(boostAmount);
		_cool = cooldown;
		yield break;
	}
	#endregion

	#region Unity Methods
	void Update() {
		if(_cool > 0f) {
			_cool -= Time.deltaTime;
		}
	}
	#endregion
}