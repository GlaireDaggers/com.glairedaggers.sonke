using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marks an object as being a potential target for HomingAttackAction
/// </summary>
public class HomingTarget : MonoBehaviour {
	#region Static Fields
	/// <summary>
	/// A list of all potential homing targets
	/// </summary>
	public static List<HomingTarget> targets = new List<HomingTarget>();
	#endregion

	#region Static Methods
	/// <summary>
	/// Get a list of homing targets in range
	/// </summary>
	public static void GetTargets(Vector3 origin, Vector3 forward, float maxDist, float maxAngle, List<HomingTarget> outTargets) {
		foreach(var target in targets) {
			Vector3 targetPos = target.GetHomingLocation(origin);
			if(Vector3.Distance(origin, targetPos) <= maxDist &&
				Vector3.Angle(forward, targetPos - origin) <= maxAngle) {
				outTargets.Add(target);
			}
		}
	}
	#endregion

	#region Inspector Fields
	/// <summary>
	/// If true, homing attack won't update position during attack
	/// </summary>
	[Tooltip("If true, homing attack won't update position during attack")]
	public bool lockPosition = false;

	/// <summary>
	/// If true, homing attack will predict slightly ahead
	/// </summary>
	[Tooltip("If true, homing attack will predict slightly ahead")]
	public bool predictAhead = false;
	#endregion

	#region Private Fields
	private IHomingPointProvider _pointProvider;
	#endregion

	#region Public Methods
	/// <summary>
	/// Gets the target point of this target
	/// </summary>
	public Vector3 GetHomingLocation(Vector3 playerPos) {
		return _pointProvider?.GetHomingLocation(playerPos) ?? transform.position;
	}
	#endregion

	#region Unity Methods
	private void Awake() {
		_pointProvider = GetComponent<IHomingPointProvider>();
	}

	private void OnEnable() {
		targets.Add(this);
	}

	private void OnDisable() {
		targets.Remove(this);
	}
	#endregion
}