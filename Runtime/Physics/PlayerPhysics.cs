using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Class which implements SA/SA2-style physics behavior
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerPhysics : MonoBehaviour {
	#region Constants
	/// <summary> extra distance margin past feet to use in ground check raycasts </summary>
	private const float GROUNDCHECK_DIST = 0.25f;

	/// <summary> Distance threshold to stay stuck to ground </summary>
	private const float GROUND_STICK_DIST_THRESHOLD = 0.25f;

	/// <summary> Angle threshold to stay stuck to ground </summary>
	private const float GROUND_STICK_ANGLE_THRESHOLD = 20f;

	/// <summary> Keep using previous ground normal after detaching from ground for this many seconds </summary>
	private const float AIR_ALIGN_DELAY = 0.1f;

	/// <summary> Angle used to detect what is a slope and what isn't </summary>
	private const float SLOPE_ANGLE_THRESHOLD = 45f;

	/// <summary> Angle used to detect what is a ceiling and what isn't </summary>
	private const float CEILING_ANGLE_THRESHOLD = 95f;

	/// <summary> If player is airborne, they can still re-stick to slopes within this threshold </summary>
	private const float SLOPE_AIR_REATTACH_THRESHOLD = 60f;

	/// <summary> Speed threshold to consider as "standing still" for purposes of turning towards accel dir </summary>
	private const float MOVE_MIN_SPEED = 0.1f;

	/// <summary> Angle threshold between actual velocity and input direction to brake instead of turning </summary>
	private const float BRAKE_ANGLE_THRESHOLD = 135f;

	/// <summary> When detaching from a surface, prevent character from re-attaching for this amount of time </summary>
	private const float FORCE_DETACH_HACK_TIMEOUT = 0.1f;
	#endregion

	#region Properties
	/// <summary> Virtual input move axes (x=horizontal axis, y=vertical axis), in player's local space </summary>
	public Vector2 InputMoveAxes { get; set; }

	/// <summary> Gets whether the player is on ground or not </summary>
	public bool isGrounded => _isGrounded;

	/// <summary> Gets whether the player is rolling or not </summary>
	public bool isRolling => _rolling;

	/// <summary>
	/// Gets the direction the player is facing
	/// </summary>
	public Vector3 facingDirection => (_visualsRot * _targetFacing) * Vector3.forward;

	/// <summary>
	/// Gets whether or not something has locked the physics state
	/// </summary>
	public bool locked => _locked;
	#endregion

	#region Events
	/// <summary> Event handler for when player begins jumping </summary>
	public UnityEvent onJump = new UnityEvent();

	/// <summary> Event handler for when Bounce is called </summary>
	public UnityEvent onBounce = new UnityEvent();

	/// <summary> Event handler for when player begins braking </summary>
	public UnityEvent onBrakeBegin = new UnityEvent();

	/// <summary> Event handler for when player stops braking </summary>
	public UnityEvent onBrakeEnd = new UnityEvent();
	#endregion

	#region Inspector Fields
	/// <summary> Collision layers to check against for ground </summary>
	[Header("Physics & Collision")]
	[Tooltip("Collision layers to check against for ground")]
	public LayerMask groundCollisionLayers = ~0;

	/// <summary> Gravity to apply while airborne </summary>
	[Tooltip("Gravity to apply while airborne")]
	public Vector3 gravity = new Vector3(0f, -10f, 0f);

	/// <summary> Maximum falling speed to clamp to </summary>
	[Tooltip("Maximum falling speed to clamp to")]
	[Min(0f)]
	public float maxFallSpeed = 100f;

	/// <summary> How much force to apply when jumping </summary>
	[Header("Behavior")]
	[Tooltip("How much force to apply when jumping")]
	[Min(0f)]
	public float jumpForce = 10f;

	/// <summary> Upper bound to clamp vertical velocity to when jump button is released in midair </summary>
	[Tooltip("Upper bound to clamp vertical velocity to when jump button is released in midair")]
	public float jumpCut = 2f;

	/// <summary> How fast to accelerate up to max speed </summary>
	[Tooltip("How fast to accelerate up to max speed")]
	[Min(0f)]
	public float acceleration = 5f;

	/// <summary> Maximum speed character can move at </summary>
	[Tooltip("Maximum speed character can move at")]
	[Min(0f)]
	public float maxSpeed = 100f;

	/// <summary> Maximum speed while rolling </summary>
	[Tooltip("Maximum speed while rolling")]
	[Min(0f)]
	public float maxRollingSpeed = 100f;

	/// <summary> Friction coefficient when no input is applied </summary>
	[Tooltip("Friction coefficient when no input is applied")]
	[Range(0f, 1f)]
	public float groundFriction = 0.1f;

	/// <summary> Friction coefficient to apply while rolling </summary>
	[Tooltip("Friction coefficient to apply while rolling")]
	[Range(0f, 1f)]
	public float rollingFriction = 0.05f;

	/// <summary> Friction coefficient when airborne </summary>
	[Tooltip("Friction coefficient when airborne")]
	[Range(0f, 1f)]
	public float airDrag = 0.05f;

	/// <summary> Friction coefficient when applying input in opposite direction of momentum </summary>
	[Tooltip("Friction coefficient when applying input in opposite direction of momentum")]
	[Range(0f, 1f)]
	public float brakeForce = 0.1f;

	/// <summary> Allowed rotation rate of the character from min to max speed </summary>
	[Tooltip("Allowed rotation rate of the character from min to max speed")]
	public AnimationCurve rotationRateOverSpeed = AnimationCurve.Linear(0f, 360f, 1f, 90f);

	/// <summary> Multiplier applied to rotation rate while airborne </summary>
	[Tooltip("Multiplier applied to rotation rate while airborne")]
	[Min(0f)]
	public float rotationRateAirMultiplier = 0.5f;

	/// <summary> Amount of vertical momentum which is converted into forward momentum upon landing </summary>
	[Tooltip("Amount of vertical momentum which is converted into forward momentum upon landing")]
	[Min(0f)]
	public float landingSpeedConversion = 1f;

	/// <summary> Minimum speed required to stick to slopes </summary>
	[Tooltip("Minimum speed required to stick to slopes")]
	[Min(0f)]
	public float slopeSpeedThreshold = 1f;

	/// <summary> Minimum speed to maintain rolling </summary>
	[Tooltip("Minimum speed to maintain rolling")]
	[Min(0f)]
	public float unrollSpeedThreshold = 1f;

	/// <summary> Amount to push player downhill when they come to a stop </summary>
	[Tooltip("Amount to push player downhill when they come to a stop")]
	[Min(0f)]
	public float slidePushForce = 1f;

	/// <summary> Speed boost to apply while moving downhill </summary>
	[Tooltip("Speed boost to apply while moving downhill")]
	[Min(0f)]
	public float downhillSpeedBoost = 1f;

	/// <summary> Root transform containing visual mesh </summary>
	[Header("Visuals & Appearance")]
	[Tooltip("Root transform containing visual mesh")]
	public Transform visualsRoot;

	/// <summary> Speed to smoothly rotate visuals root towards real rotation while grounded </summary>
	[Tooltip("Speed to smoothly rotate visuals root towards real rotation while grounded")]
	public float rotationSmoothingSpeed = 360f;

	/// <summary> Speed to smoothly rotate visuals root towards real rotation while airborne </summary>
	[Tooltip("Speed to smoothly rotate visuals root towards real rotation while airborne")]
	public float rotationSmoothingSpeedAir = 180f;
	#endregion

	#region Private Fields
	private Transform _tr;
	private Rigidbody _rb;
	private Vector3 _groundNormal;
	private Vector3 _airGroundNormal;
	private float _airTime;
	private bool _wasAirborne;
	private bool _isGrounded;
	private bool _isJumping;
	private bool _jumpBtnState;
	private bool _forceDetach;
	private float _forceDetachTimeout;
	private Quaternion _visualsRot;
	private Quaternion _targetFacing;
	private bool _wasBraking;
	private Vector3 _bounceVel;
	private float _bounceTime;
	private bool _locked;
	private bool _rolling;
	#endregion

	#region Unity Methods
	private void Awake() {
		_tr = GetComponent<Transform>();
		_rb = GetComponent<Rigidbody>();
		_groundNormal = Vector3.up;
		_airGroundNormal = _groundNormal;

		_rb.useGravity = false;
		_rb.constraints |= RigidbodyConstraints.FreezeRotation;

		_targetFacing = Quaternion.identity;
		_visualsRot = visualsRoot?.rotation ?? Quaternion.identity;
	}

	private void FixedUpdate() {
		PhysicsTick();
	}

	private void LateUpdate() {
		if(visualsRoot != null) {
			float rotSpeed = _isGrounded ? rotationSmoothingSpeed : rotationSmoothingSpeedAir;
			_visualsRot = Quaternion.RotateTowards(_visualsRot, transform.rotation, rotSpeed * Time.deltaTime);
			visualsRoot.rotation = _visualsRot * _targetFacing;
		}
	}
	#endregion

	#region Public Methods
	/// <summary>
	/// Bounce player with the given velocity for the given number of seconds
	/// </summary>
	/// <param name="force">Force to apply to the player</param>
	/// <param name="duration">If >0, disable input and force player's velocity to given bounce force for this many seconds</param>
	public void Bounce(Vector3 force, float duration = 0f) {
		onBounce.Invoke();

		_rb.velocity = force;
		Detach();
		_isJumping = true;
		_isGrounded = false;
		_rolling = false;

		if(duration > 0f) {
			_bounceVel = force;
			_bounceTime = duration;
		}
	}

	/// <summary>
	/// Try to enter rolling
	/// </summary>
	public void Roll() {
		if(_isGrounded) {
			_rolling = true;
		}
	}

	/// <summary>
	/// Increase current speed by given amount
	/// </summary>
	public void Boost(float amount) {
		float target = _rolling ? maxRollingSpeed : maxSpeed;
		float speedInDir = Vector3.Dot(_rb.velocity, facingDirection);
		float diff = target - speedInDir;
		diff = Mathf.Clamp(diff, 0f, amount);
		_rb.velocity += facingDirection * diff;
	}

	/// <summary>
	/// Stop the script from controlling the physics state (used when external code wants to take over)
	/// </summary>
	public void LockState() {
		_locked = true;
	}

	/// <summary>
	/// Resume letting the script control the physics state (used when external code controlling physics state is finished)
	/// </summary>
	public void UnlockState() {
		_locked = false;
	}

	/// <summary>
	/// Force player to be aligned with new up vector. Make sure LockState() is called first.
	/// </summary>
	/// <param name="upVector"></param>
	public void ForceUpAlignment(Vector3 upVector) {
		if(!_locked) {
			Debug.LogError("Please call LockState() first before trying to modify physics state");
			return;
		}

		_groundNormal = upVector;
	}

	/// <summary>
	/// Force player grounded state to given value. Make sure LockState() is called first
	/// </summary>
	/// <param name="grounded"></param>
	public void ForceGrounded(bool grounded) {
		if(!_locked) {
			Debug.LogError("Please call LockState() first before trying to modify physics state");
			return;
		}

		_isGrounded = grounded;
		_wasAirborne = !grounded;
	}

	/// <summary>
	/// Force player rolling state to given value. Make sure LockState() is called first
	/// </summary>
	/// <param name="rolling"></param>
	public void ForceRolling(bool rolling) {
		if(!_locked) {
			Debug.LogError("Please call LockState() first before trying to modify physics state");
			return;
		}

		_rolling = rolling;
	}

	/// <summary>
	/// Rotate facing direction towards input axes
	/// </summary>
	public void RotateTowardsInput(float rotationSpeed) {
		if(!_locked) {
			Debug.LogError("Please call LockState() first before trying to modify physics state");
			return;
		}

		if(InputMoveAxes.sqrMagnitude > Mathf.Epsilon) {
			Quaternion rot = Quaternion.LookRotation(new Vector3(InputMoveAxes.x, 0f, InputMoveAxes.y), Vector3.up);
			_targetFacing = Quaternion.RotateTowards(_targetFacing, rot, rotationSpeed * Time.deltaTime);
		}
	}

	/// <summary>
	/// Rotate facing direction towards given world-space direction
	/// </summary>
	public void RotateTowardsDirection(Vector3 target, float rotationSpeed) {
		if(!_locked) {
			Debug.LogError("Please call LockState() first before trying to modify physics state");
			return;
		}

		target = _tr.InverseTransformDirection(target);
		target.y = 0f;

		if(target.sqrMagnitude > Mathf.Epsilon) {
			Quaternion rot = Quaternion.LookRotation(target, Vector3.up);
			_targetFacing = Quaternion.RotateTowards(_targetFacing, rot, rotationSpeed * Time.deltaTime);
		}
	}

	/// <summary>
	/// Sets the current state of the jump button
	/// </summary>
	/// <param name="state">True if pressed, false if released</param>
	public void SetJumpPressed(bool state) {
		if(state && !_jumpBtnState) {
			if(_isGrounded) {
				// apply jumping force away from current ground normal and detach
				_isJumping = true;
				_rb.velocity += _groundNormal * jumpForce;
				_isGrounded = false;
				_rolling = false;
				Detach();
				onJump.Invoke();
			}

			_jumpBtnState = true;
		} else if(!state && _jumpBtnState) {
			if(!_isGrounded && _isJumping) {
				if(_rb.velocity.y > jumpCut) {
					_rb.velocity = new Vector3(_rb.velocity.x, jumpCut, _rb.velocity.z);
				}
			}

			_isJumping = false;
			_jumpBtnState = false;
		}
	}
	#endregion

	#region Private Methods
	private void PhysicsTick() {
		if(_bounceTime > 0f) {
			_bounceTime -= Time.deltaTime;
			_rb.velocity = _bounceVel;
			_groundNormal = Vector3.up;
			_airTime += Time.deltaTime;
			_wasAirborne = true;
			_isGrounded = false;
			RealignUp(Vector3.up);
		} else if(_locked) {
			_wasBraking = false;
			RealignUp(_groundNormal);
		} else {
			Debug.DrawRay(_tr.position + _tr.up, _rb.velocity.normalized * (1f + GROUNDCHECK_DIST));

			if(Physics.Raycast(_tr.position + _tr.up, -_tr.up, out var hitInfo, 1f + GROUNDCHECK_DIST, groundCollisionLayers, QueryTriggerInteraction.Ignore) &&
	   			!_forceDetach) {
				_groundNormal = hitInfo.normal;
				RealignUp(_groundNormal);
				_airGroundNormal = _groundNormal;
				_airTime = 0f;
				_isGrounded = true;

				HandleGroundPhysics();
			} else if(_wasAirborne && Physics.Raycast(_tr.position + _tr.up, facingDirection, out hitInfo, 1f + GROUNDCHECK_DIST, groundCollisionLayers, QueryTriggerInteraction.Ignore) &&
	   			!_forceDetach && Vector3.Angle(hitInfo.normal, Vector3.up) <= SLOPE_AIR_REATTACH_THRESHOLD) {
				_groundNormal = hitInfo.normal;
				RealignUp(_groundNormal);
				_rb.MovePosition(hitInfo.point);
				_isGrounded = true;
			} else {
				_rolling = false;
				_groundNormal = Vector3.up;
				_airTime += Time.deltaTime;
				_wasAirborne = true;
				_isGrounded = false;

				if(_airTime >= AIR_ALIGN_DELAY) {
					RealignUp(Vector3.up);
				} else {
					RealignUp(_airGroundNormal);
				}

				HandleAirPhysics();
			}
		}

		Vector3 localVel = _tr.InverseTransformDirection(_rb.velocity);
		localVel.y = 0f;

		// update target facing to direction we're moving in
		if(localVel.sqrMagnitude > 0.1f && !_locked) {
			_targetFacing = Quaternion.LookRotation(localVel, Vector3.up);
		}

		if(_forceDetach) {
			_forceDetachTimeout -= Time.deltaTime;
			if(_forceDetachTimeout <= 0f) {
				_forceDetach = false;
			}
		}
	}

	private void Detach() {
		_forceDetach = true;
		_forceDetachTimeout = FORCE_DETACH_HACK_TIMEOUT;
	}

	private void HandleGroundPhysics() {
		GroundControl();

		float groundSlope = Mathf.Acos(_groundNormal.y) * Mathf.Rad2Deg;

		// if we've just landed on the ground, convert velocity perpendicular to ground plane into a speed boost
		if(_wasAirborne) {
			Vector3 addSpeed = Mathf.Abs(Vector3.Dot(_rb.velocity, _groundNormal)) * Vector3.ProjectOnPlane(_rb.velocity, _groundNormal).normalized * landingSpeedConversion;
			_rb.velocity += addSpeed;
			_wasAirborne = false;
		}

		Vector3 planeVel = Vector3.ProjectOnPlane(_rb.velocity, _groundNormal);

		// unroll if we go below rolling speed threshold
		if(_rolling && planeVel.sqrMagnitude < (unrollSpeedThreshold * unrollSpeedThreshold)) {
			_rolling = false;
		}

		if(groundSlope >= CEILING_ANGLE_THRESHOLD) {
			// detach if speed falls below a certain threshold
			if(planeVel.sqrMagnitude < (slopeSpeedThreshold * slopeSpeedThreshold)) {
				_rb.velocity += _groundNormal * 3f;
				Detach();
			}
		} else if(groundSlope >= SLOPE_ANGLE_THRESHOLD) {
			// if we're moving downhill, apply a downhill speed boost
			if(_rb.velocity.y < 0f) {
				float d = _rb.velocity.normalized.y;
				_rb.velocity += Vector3.ProjectOnPlane(Vector3.up, _groundNormal) * d * downhillSpeedBoost;
			}

			_rb.velocity += Vector3.ProjectOnPlane(Vector3.down, _groundNormal) * slidePushForce * Time.deltaTime;
		}
	}

	private void HandleAirPhysics() {
		AirControl();
		_rb.velocity += gravity * Time.deltaTime;

		if(_rb.velocity.y < -maxFallSpeed) {
			_rb.velocity = new Vector3(_rb.velocity.x, -maxFallSpeed, _rb.velocity.z);
		}
	}

	private void GroundControl() {
		// get velocity in local space and store lateral component
		Vector3 velocityLS = transform.InverseTransformDirection(_rb.velocity);
		Vector3 velocityXZ = new Vector3(velocityLS.x, 0f, velocityLS.z);

		// turn axes into local-space direction
		Vector3 inputLS = new Vector3(InputMoveAxes.x, 0f, InputMoveAxes.y);

		bool isBraking = false;

		if(inputLS.sqrMagnitude > Mathf.Epsilon) {
			// split input into direction+magnitude
			float inputMagnitude = inputLS.magnitude;
			Vector3 inputDir = inputLS / inputMagnitude;

			// if the player is standing still, allow them to immediately begin accelerating in the desired direction
			if(velocityXZ.sqrMagnitude <= MOVE_MIN_SPEED) {
				velocityXZ += acceleration * inputLS * Time.deltaTime;
			} else {
				// if desired input direction is opposite velocity and we aren't rolling, then brake instead
				if(Vector3.Angle(velocityXZ, inputDir) >= BRAKE_ANGLE_THRESHOLD && !_rolling) {
					velocityXZ *= 1f - brakeForce;
					isBraking = true;
				} else {
					// otherwise, we rotate the current velocity direction to face the input direction over time
					// while also increasing speed up to maximum

					// split velocity into direction+magnitude
					float speed = velocityXZ.magnitude;
					Vector3 velocityXZDir = velocityXZ / speed;

					// the faster the player is moving, the slower this rotation rate to make player feel less "twitchy" at high speeds
					float rotationRate = rotationRateOverSpeed.Evaluate(velocityXZ.magnitude / maxSpeed);

					velocityXZDir = Vector3.RotateTowards(velocityXZDir, inputDir,
						rotationRate * Mathf.Deg2Rad * Time.deltaTime, 1f);

					// accelerate speed up to max speed if we're not rolling
					if(!_rolling) {
						speed = Mathf.MoveTowards(speed, maxSpeed * inputMagnitude, acceleration * Time.deltaTime);
					}

					velocityXZ = velocityXZDir * speed;
				}
			}
		} else {
			// apply deceleration unless slope is too steep and player is going downhill
			if(Vector3.Angle(_groundNormal, Vector3.up) < SLOPE_ANGLE_THRESHOLD || _rb.velocity.y > 0f) {
				velocityXZ *= 1f - (_rolling ? rollingFriction : groundFriction);
			}
		}

		if(isBraking && !_wasBraking) {
			_wasBraking = true;
			onBrakeBegin.Invoke();
		} else if(_wasBraking && !isBraking) {
			_wasBraking = false;
			onBrakeEnd.Invoke();
		}

		// transform back into world space, perform ground sticking, and apply
		Vector3 velocityWS = transform.rotation * new Vector3(velocityXZ.x, velocityLS.y, velocityXZ.z);
		velocityWS = StickToGround(velocityWS);
		_rb.velocity = velocityWS;
	}

	private void AirControl() {
		// get velocity in local space and store lateral component
		Vector3 velocityLS = transform.InverseTransformDirection(_rb.velocity);
		Vector3 velocityXZ = new Vector3(velocityLS.x, 0f, velocityLS.z);

		// turn axes into local-space direction
		Vector3 inputLS = new Vector3(InputMoveAxes.x, 0f, InputMoveAxes.y);

		if(inputLS.sqrMagnitude > Mathf.Epsilon) {
			// split input into direction+magnitude
			float inputMagnitude = inputLS.magnitude;
			Vector3 inputDir = inputLS / inputMagnitude;

			// if the player is standing still, allow them to immediately begin accelerating in the desired direction
			if(velocityXZ.sqrMagnitude <= MOVE_MIN_SPEED) {
				velocityXZ += acceleration * inputLS * Time.deltaTime;
			} else {
				// otherwise, we rotate the current velocity direction to face the input direction over time
				// while also increasing speed up to maximum

				// split velocity into direction+magnitude
				float speed = velocityXZ.magnitude;
				Vector3 velocityXZDir = velocityXZ / speed;

				// the faster the player is moving, the slower this rotation rate to make player feel less "twitchy" at high speeds
				float rotationRate = rotationRateOverSpeed.Evaluate(velocityXZ.magnitude / maxSpeed) * rotationRateAirMultiplier;

				velocityXZDir = Vector3.RotateTowards(velocityXZDir, inputDir,
					rotationRate * Mathf.Deg2Rad * Time.deltaTime, 1f);

				// accelerate speed up to max speed
				speed = Mathf.MoveTowards(speed, maxSpeed * inputMagnitude, acceleration * Time.deltaTime);

				velocityXZ = velocityXZDir * speed;
			}
		} else {
			// apply deceleration
			velocityXZ *= 1f - airDrag;
		}

		// transform back into world space and apply
		Vector3 velocityWS = transform.rotation * new Vector3(velocityXZ.x, velocityLS.y, velocityXZ.z);
		_rb.velocity = velocityWS;
	}

	private Vector3 StickToGround(Vector3 velocity) {
		if(_rb.SweepTest(-_tr.up, out var hitInfo, GROUND_STICK_DIST_THRESHOLD, QueryTriggerInteraction.Ignore) &&
			Mathf.Acos(Vector3.Dot(hitInfo.normal, _tr.up)) <= GROUND_STICK_ANGLE_THRESHOLD * Mathf.Deg2Rad) {
			_rb.MovePosition(_rb.position - (_tr.up * Mathf.Max(0f, hitInfo.distance - 0.05f)));
		}
		return Vector3.ProjectOnPlane(velocity, _groundNormal);
	}

	private void RealignUp(Vector3 up) {
		Quaternion newRot = Quaternion.FromToRotation(_rb.rotation * Vector3.up, up) * _rb.rotation;
		_rb.MoveRotation(newRot);
	}
	#endregion
}
