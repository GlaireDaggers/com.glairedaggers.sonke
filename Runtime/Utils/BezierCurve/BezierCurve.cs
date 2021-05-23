using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic implementation of a quadratic bezier curve
/// </summary>
public class BezierCurve : MonoBehaviour {
	#region Public Properties
	/// <summary>
	/// Gets the nodes of the spline
	/// </summary>
	public BezierNode[] nodes => _nodes;
	#endregion

	#region Public Fields
	/// <summary>
	/// Whether the curve forms a closed loop or not
	/// </summary>
	[Tooltip("Whether the curve forms a closed loop or not")]
	public bool Loop;
	#endregion

	#region Private Fields
	private BezierNode[] _nodes;
	private List<Segment> _tmpSegments = new List<Segment>();
	#endregion

	#region Unity Methods
	void Awake() {
		_nodes = GetComponentsInChildren<BezierNode>();
	}

	void OnDrawGizmos() {
		var nodes = GetComponentsInChildren<BezierNode>();

		// for each pair of nodes, draw a Bezier segment
		if(nodes.Length > 1) {
			for(int i = 1; i < nodes.Length; i++) {
				BezierNode prevNode = nodes[i - 1];
				BezierNode curNode = nodes[i];
				DrawSpline(prevNode, curNode);
			}

			if(Loop) {
				BezierNode prevNode = nodes[nodes.Length - 1];
				BezierNode curNode = nodes[0];
				DrawSpline(prevNode, curNode);
			}
		}
	}
	#endregion

	#region Public Methods
	/// <summary>
	/// Gets the closest point on the rail spline, and output orientation of that point
	/// </summary>
	public Vector3 GetClosestPoint(Vector3 pos, out Quaternion rotation) {
		// there's not really a good analytical solution to this that I can find
		// so instead I'm just splitting the bezier into discrete segments and then for each segment computing closest point.
		_tmpSegments.Clear();

		if(_nodes.Length == 0) {
			throw new System.InvalidOperationException("Invalid spline!");
		} else if(_nodes.Length == 1) {
			rotation = _nodes[0].transform.rotation;
			return _nodes[0].transform.position;
		} else {
			for(int i = 1; i < _nodes.Length; i++) {
				BezierNode prevNode = _nodes[i - 1];
				BezierNode curNode = _nodes[i];
				ComputeSegments(prevNode, curNode, _tmpSegments, 8);
			}

			if(Loop) {
				BezierNode prevNode = _nodes[_nodes.Length - 1];
				BezierNode curNode = _nodes[0];
				ComputeSegments(prevNode, curNode, _tmpSegments, 8);
			}

			Vector3 bestPt = Vector3.zero;
			Quaternion bestRot = Quaternion.identity;
			float bestDist = Mathf.Infinity;
			foreach(var seg in _tmpSegments) {
				Vector3 pt = seg.GetClosestPoint(pos, out Quaternion rot);
				float dist = Vector3.Distance(pt, pos);

				if(dist < bestDist) {
					bestDist = dist;
					bestPt = pt;
					bestRot = rot;
				}
			}

			rotation = bestRot;
			return bestPt;
		}
	}
	#endregion

	#region Private Methods
	private void DrawSpline(BezierNode prevNode, BezierNode curNode) {
		DrawSpline(prevNode.transform.position, prevNode.transform.position + (prevNode.transform.forward * prevNode.Weight),
			curNode.transform.position - (curNode.transform.forward * curNode.Weight), curNode.transform.position,
			prevNode.transform.up, curNode.transform.up);
	}

	private void ComputeSegments(BezierNode prevNode, BezierNode curNode, List<Segment> outSegments, int detail) {
		ComputeSegments(prevNode.transform.position, prevNode.transform.position + (prevNode.transform.forward * prevNode.Weight),
			curNode.transform.position - (curNode.transform.forward * curNode.Weight), curNode.transform.position,
			prevNode.transform.up, curNode.transform.up, outSegments, detail);
	}

	private void ComputeSegments(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 u0, Vector3 u1, List<Segment> outSegments, int detail) {
		Vector3 lastPt = ComputeBezier(p0, p1, p2, p3, u0, u1, 0f, out Quaternion lastRot);

		for(int i = 1; i <= detail; i++) {
			float t = i / (float)detail;

			Vector3 b = ComputeBezier(p0, p1, p2, p3, u0, u1, t, out Quaternion rot);

			outSegments.Add(new Segment()
			{
				p0 = lastPt,
				p1 = b,
				r0 = lastRot,
				r1 = rot,
			});

			lastPt = b;
			lastRot = rot;
		}
	}

	private void DrawSpline(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 u0, Vector3 u1) {
		Vector3 lastPt = p0;

		for(int i = 1; i <= 32; i++) {
			float t = i / 32f;

			Vector3 b = ComputeBezier(p0, p1, p2, p3, u0, u1, t, out Quaternion rot);

			Gizmos.color = Color.yellow;
			Gizmos.DrawLine(lastPt, b);

			Gizmos.color = Color.red;
			Gizmos.DrawRay(b, rot * Vector3.right * 0.25f);

			Gizmos.color = Color.green;
			Gizmos.DrawRay(b, rot * Vector3.up * 0.25f);

			lastPt = b;
		}
	}

	private Vector3 ComputeBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 u0, Vector3 u1, float t, out Quaternion rot) {
		Vector3 q0 = Vector3.Lerp(p0, p1, t);
		Vector3 q1 = Vector3.Lerp(p1, p2, t);
		Vector3 q2 = Vector3.Lerp(p2, p3, t);
		Vector3 r0 = Vector3.Lerp(q0, q1, t);
		Vector3 r1 = Vector3.Lerp(q1, q2, t);
		Vector3 b = Vector3.Lerp(r0, r1, t);

		// calculate orientation
		Vector3 tangent = ComputeBezierDerivative(p0, p1, p2, p3, t).normalized;
		Vector3 u = Vector3.Slerp(u0, u1, t);
		Vector3 right = Vector3.Cross(u, tangent);
		Vector3 up = Vector3.Cross(tangent, right);
		rot = Quaternion.LookRotation(tangent, up);

		return b;
	}

	private Vector3 ComputeBezierDerivative(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t) {
		a = 3f * (b - a);
		b = 3f * (c - b);
		c = 3f * (d - c);

		return a * ((1 - t) * (1 - t)) + 2f * b * (1 - t) * t + c * (t * t);
	}
	#endregion

	#region Types
	private struct Segment {
		public Vector3 p0;
		public Quaternion r0;
		public Vector3 p1;
		public Quaternion r1;

		public Vector3 GetClosestPoint(Vector3 pt, out Quaternion rot) {
			Vector3 direction = (p1 - p0);
			float length = direction.magnitude;
			direction /= length;
			Vector3 lhs = pt - p0;
			float dotp = Vector3.Dot(lhs, direction);
			dotp = Mathf.Clamp(dotp, 0f, length);
			float t = dotp / length;

			rot = Quaternion.Slerp(r0, r1, t);
			return p0 + (direction * dotp);
		}
	}
	#endregion
}
