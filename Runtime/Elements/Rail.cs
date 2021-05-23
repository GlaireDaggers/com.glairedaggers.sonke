using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implements a rail gameplay element
/// </summary>
[RequireComponent(typeof(BezierCurve))]
public class Rail : MonoBehaviour, IHomingPointProvider {
	#region Static
	/// <summary>
	/// A list of all rails in the scene
	/// </summary>
	public static List<Rail> rails = new List<Rail>();
	#endregion

	#region Public Properties
	/// <summary>
	/// Gets the first node of the rail
	/// </summary>
	public BezierNode startNode => _curve.nodes[0];

	/// <summary>
	/// Gets the last node of the rail
	/// </summary>
	public BezierNode endNode => _curve.nodes[_curve.nodes.Length - 1];

	/// <summary>
	/// Gets the bezier curve representing this rail
	/// </summary>
	public BezierCurve curve => _curve;
	#endregion

	#region Private Fields
	private BezierCurve _curve;
	#endregion

	#region Public Methods
	public Vector3 GetHomingLocation(Vector3 pos) {
		return _curve.GetClosestPoint(pos, out _);
	}
	#endregion

	#region Unity Methods
	void Awake() {
		_curve = GetComponent<BezierCurve>();
		rails.Add(this);
	}

	void OnDestroy() {
		rails.Remove(this);
	}
	#endregion
}