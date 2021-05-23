using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implementation of a jump action
/// </summary>
public class JumpAction : PlayerActionBase {
	#region Overrides
	public override bool IsValid(ActionPhase phase) {
		return base.IsValid(phase);
	}

	public override IEnumerator Execute(ActionPhase phase) {
		physics.SetJumpPressed(phase == ActionPhase.Enter);
		yield break;
	}
	#endregion
}