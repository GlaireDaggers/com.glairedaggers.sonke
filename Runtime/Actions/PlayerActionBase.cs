using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Current phase of an action input
/// </summary>
public enum ActionPhase {
	Enter,
	Exit,
}

/// <summary>
/// Base class for player performable actions.
/// </summary>
[RequireComponent(typeof(PlayerPhysics))]
public abstract class PlayerActionBase : MonoBehaviour {
	#region Properties
	/// <summary>
	/// Gets the attached PlayerPhysics component
	/// </summary>
	public PlayerPhysics physics { get; private set; }

	/// <summary>
	/// Gets the attached PlayerAnimationController component, if any
	/// </summary>
	public PlayerAnimationController animator { get; private set; }

	/// <summary>
	/// Gets whether this action is a hold+release type
	/// </summary>
	public virtual bool holdRelease => false;
	#endregion

	#region Inspector Fields
	/// <summary>
	/// The action set this action belongs to
	/// </summary>
	[Tooltip("The action set this action belongs to")]
	public ActionSet action = ActionSet.Action0;
	#endregion

	#region Overridable Methods
	protected virtual void Awake() {
		physics = GetComponent<PlayerPhysics>();
		animator = GetComponent<PlayerAnimationController>();
	}

	public virtual bool IsValid(ActionPhase phase) {
		return true;
	}

	public virtual IEnumerator Execute(ActionPhase phase) {
		yield break;
	}
	#endregion
}