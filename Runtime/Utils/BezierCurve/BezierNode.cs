using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BezierNode : MonoBehaviour {
	public float Weight = 1f;

	void OnDrawGizmosSelected() {
		Gizmos.color = Color.blue;
		Gizmos.DrawRay(transform.position, transform.forward * Weight);
		Gizmos.DrawRay(transform.position, -transform.forward * Weight);
	}
}