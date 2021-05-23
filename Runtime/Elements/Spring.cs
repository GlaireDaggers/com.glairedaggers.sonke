using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spring : MonoBehaviour {
	#region Inspector Fields
	/// <summary>
	/// Force to apply to player
	/// </summary>
	[Tooltip("Force to apply to player")]
	[Min(0f)]
	public float bounceForce = 20f;

	/// <summary>
	/// If >0, set player's velocity to bounce force for this many seconds
	/// </summary>
	[Tooltip("If >0, set player's velocity to bounce force for this many seconds")]
	[Min(0f)]
	public float duration = 0f;

	/// <summary>
	/// If true, force player's position to be aligned with spring on bounce
	/// </summary>
	[Tooltip("If true, force player's position to be aligned with spring on bounce")]
	public bool forceAlignPosition = true;
	#endregion

	#region Unity Methods
	private void OnTriggerEnter(Collider other) {
		var phys = other.GetComponent<PlayerPhysics>();

		if(phys != null) {
			phys.Bounce(transform.up * bounceForce, duration);
		}

		if(forceAlignPosition) {
			phys.GetComponent<Rigidbody>().MovePosition(transform.position);
		}
	}

	private void OnDrawGizmosSelected() {
		if(duration > 0f) {
			// show how far the player would travel
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(transform.position + (transform.up * bounceForce * duration), 0.5f);
		}

		Gizmos.color = Color.yellow;
		Gizmos.DrawRay(transform.position, transform.up * bounceForce);
	}
	#endregion
}