using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interface for components which can provide a custom homing location to a HomingTarget
/// </summary>
public interface IHomingPointProvider {
	/// <summary>
	/// Get the location the player should target
	/// </summary>
	Vector3 GetHomingLocation(Vector3 playerPosition);
}
