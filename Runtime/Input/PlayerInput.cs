using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Class responsible for providing input to a PlayerPhysics component
/// </summary>
[RequireComponent(typeof(PlayerPhysics))]
public class PlayerInput : MonoBehaviour {
	#region Private Constants
	/// <summary> Angle threshold used to check if forward and up basis vectors are nearly parallel </summary>
	private const float BASIS_PARALLEL_THRESHOLD = 45f;
	#endregion

	#region Private Fields
	private PlayerPhysics _physics;
	private Vector2 _moveAxes;
	private Dictionary<ActionSet, List<PlayerActionBase>> _actions = new Dictionary<ActionSet, List<PlayerActionBase>>();
	private bool _execAction;
	private PlayerActionBase _enteredAction;
	#endregion

	#region Unity Methods
	private void Awake() {
		_physics = GetComponent<PlayerPhysics>();

		// gather actions
		var actions = GetComponents<PlayerActionBase>();
		foreach(var action in actions) {
			if(!_actions.ContainsKey(action.action)) {
				_actions[action.action] = new List<PlayerActionBase>();
			}

			_actions[action.action].Add(action);
		}
	}

	private void Update() {
		Vector3 input = new Vector3(_moveAxes.x, 0f, _moveAxes.y);

		// calculate a frame of reference to make input relative to
		Vector3 basisFwd = Camera.main.transform.forward;
		Vector3 basisUp = transform.up;

		// if up & fwd are nearly parallel, then use fallback fwd vector
		if(Mathf.Abs(Vector3.Dot(basisFwd, basisUp)) >= Mathf.Cos(BASIS_PARALLEL_THRESHOLD * Mathf.Deg2Rad)) {
			basisFwd = Vector3.up;
		}

		// create plane of reference using these vectors, transform input into world space
		Quaternion inputBasis = Quaternion.LookRotation(basisFwd, basisUp);
		input = inputBasis * input;

		// transform into local space and provide to controller
		input = transform.InverseTransformDirection(input);
		_physics.InputMoveAxes = new Vector2(input.x, input.z);
	}
	#endregion

	#region Public Methods
	public void InputMove(InputAction.CallbackContext context) {
		_moveAxes = context.ReadValue<Vector2>();
	}

	public void InputAction0(InputAction.CallbackContext context) {
		HandleAction(context, ActionSet.Action0);
	}

	public void InputAction1(InputAction.CallbackContext context) {
		HandleAction(context, ActionSet.Action1);
	}

	public void InputAction2(InputAction.CallbackContext context) {
		HandleAction(context, ActionSet.Action2);
	}

	public void InputAction3(InputAction.CallbackContext context) {
		HandleAction(context, ActionSet.Action3);
	}
	#endregion

	#region Private Methods
	private void HandleAction(InputAction.CallbackContext context, ActionSet set) {
		if(context.phase != InputActionPhase.Performed) return;
		if(_execAction) return;

		var phase = context.ReadValue<float>() != 0f ? ActionPhase.Enter : ActionPhase.Exit;

		// if we entered a hold+release type action, force that action to trigger on release
		if(phase == ActionPhase.Exit && _enteredAction != null) {
			StartCoroutine(ExecuteAction(_enteredAction.Execute(phase)));
			_enteredAction = null;
		} else {
			// otherwise if any actions mapped to this set are currently valid, perform that action
			var action = GetValidAction(set, phase);
			if(action != null) {
				StartCoroutine(ExecuteAction(action.Execute(phase)));

				if(action.holdRelease && phase == ActionPhase.Enter) {
					_enteredAction = action;
				}
			}
		}
	}
	private IEnumerator ExecuteAction(IEnumerator action) {
		_execAction = true;
		yield return StartCoroutine(action);
		_execAction = false;
	}

	private PlayerActionBase GetValidAction(ActionSet set, ActionPhase phase) {
		if(!_actions.ContainsKey(set)) {
			return null;
		}

		var pool = _actions[set];
		foreach(var action in pool) {
			if(action.IsValid(phase)) {
				return action;
			}
		}

		return null;
	}
	#endregion
}