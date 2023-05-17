using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using System;

namespace KinematicCharacterController.Examples
{
    public enum CharacterState
    {
        Default,
    }

    public enum OrientationMethod
    {
        TowardsCamera,
        TowardsMovement,
        TowardsMomentum,
    }

    public struct PlayerCharacterInputs
    {
        public float MoveAxisForward;
        public float MoveAxisRight;
        public Quaternion CameraRotation;
        public bool JumpDown;
        public bool CrouchDown;
        public bool CrouchUp;
        public bool Attack;
    }

    public struct AICharacterInputs
    {
        public Vector3 MoveVector;
        public Vector3 LookVector;
    }

    public enum BonusOrientationMethod
    {
        None,
        TowardsGravity,
        TowardsGroundSlopeAndGravity,
    }

    public class ExampleCharacterController : MonoBehaviour, ICharacterController
    {
        public KinematicCharacterMotor Motor;

        [Header("Stable Movement")]
        public float MaxStableMoveSpeed = 10f;
        public float StableMovementSharpness = 15f;
        public float OrientationSharpness = 10f;
        public OrientationMethod OrientationMethod = OrientationMethod.TowardsCamera;

        [Header("Air Movement")]
        public float MaxAirMoveSpeed = 15f;
        public float AirAccelerationSpeed = 15f;
        public float Drag = 0.1f;

        [Header("Jumping")]
        public bool AllowJumpingWhenSliding = false;
        public float JumpUpSpeed = 10f;
        public float JumpScalableForwardSpeed = 10f;
        public float JumpPreGroundingGraceTime = 0f;
        public float JumpPostGroundingGraceTime = 0f;
        private float _timeSinceLastGroundJump;

        [Header("Wall Jumping")]
        public bool AllowWallJump = false;
        public float WallSlideDuration = 0f;
        [HideInInspector]
        public bool WallSliding { get => _wallSliding; }
        private bool _wallSliding = false;
        public bool WallSlideFade = false;
        private float _wallSlideTimer = 0f;
        public float WallJumpDuration = 0f;
        public float WallJumpCooldown = 0f;
        private float _wallJumpTimer = 0f;
        private Vector3 _lastWallNormal, _currentWallNormal, _pointDir;
        public float WallSlideDistance = 0f;
        public float UpwardsMovement = 0f;
        public float WallSlideStickyTime = 0f;
        private float _wallSlideUnstickTimer = 0f;
        private bool _initUnstick = false;
        public float WallSlideMovementThreshold = 0f;
        public float WallSlideCheckRadius = 0f;
        public float WallSlideDrag_mod = 0f;
        public float WallSlideJumpCooldownTimer = 0f;
        public Transform BottomWallSlidePivot;
        public Transform TopWallSlidePivot;
        int _wallSlideMask;

        [Header("Misc")]
        public List<Collider> IgnoredColliders = new List<Collider>();
        public BonusOrientationMethod BonusOrientationMethod = BonusOrientationMethod.None;
        public float BonusOrientationSharpness = 10f;
        public Vector3 Gravity = new Vector3(0, -30f, 0);
        public Transform MeshRoot;
        public Transform CameraFollowPoint;
        public float CrouchedCapsuleHeight = 1f;

        public CharacterState CurrentCharacterState { get; private set; }

        private Collider[] _probedColliders = new Collider[8];
        private RaycastHit[] _probedHits = new RaycastHit[8];
        private Vector3 _moveInputVector;
        private Vector3 _lookInputVector;
        private bool _jumpRequested = false;
        private bool _jumpConsumed = false;
        private bool _jumpedThisFrame = false;
        private float _timeSinceJumpRequested = Mathf.Infinity;
        private float _timeSinceLastAbleToJump = 0f;
        private Vector3 _internalVelocityAdd = Vector3.zero;
        private bool _shouldBeCrouching = false;
        private bool _isCrouching = false;

        [Header("Attack")]
        private bool _attackRequested;
        private bool _attackConsumed;
        private float _timeSinceAttackLastRequested;
        private float _timeSinceLastAttack;
        private float _attackTimer;
        public float AttackDuration;
        public float CanAttackTimer;
        public float AttackRegisterTimer;
        [HideInInspector] public bool Attacking;
        private SphereCollider _attackCollider;
        // if time since attack requested > register timer, reset
        // if time since attack requested < register timer and time since last attack > attack period, attack

        private Vector3 lastInnerNormal = Vector3.zero;
        private Vector3 lastOuterNormal = Vector3.zero;

        private void Awake()
        {
            // Handle initial state
            TransitionToState(CharacterState.Default);

            // Assign the characterController to the motor
            Motor.CharacterController = this;
            _wallSlideMask = ~(Motor.CollidableLayers & LayerMask.GetMask("Collectible", "Treasure", "TreasureChecker"));
            _timeSinceLastAttack = CanAttackTimer;
            _attackCollider = transform.FindDeepChild("AttackCollider").GetComponent<SphereCollider>();
        }

        /// <summary>
        /// Handles movement state transitions and enter/exit callbacks
        /// </summary>
        public void TransitionToState(CharacterState newState)
        {
            CharacterState tmpInitialState = CurrentCharacterState;
            OnStateExit(tmpInitialState, newState);
            CurrentCharacterState = newState;
            OnStateEnter(newState, tmpInitialState);
        }

        /// <summary>
        /// Event when entering a state
        /// </summary>
        public void OnStateEnter(CharacterState state, CharacterState fromState)
        {
            switch (state)
            {
                case CharacterState.Default:
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Event when exiting a state
        /// </summary>
        public void OnStateExit(CharacterState state, CharacterState toState)
        {
            switch (state)
            {
                case CharacterState.Default:
               {
                    break;
               }
            }
        }

        /// <summary>
        /// This is called every frame by ExamplePlayer in order to tell the character what its inputs are
        /// </summary>
        public void SetInputs(ref PlayerCharacterInputs inputs)
        {
            // Clamp input
            Vector3 moveInputVector = Vector3.ClampMagnitude(new Vector3(inputs.MoveAxisRight, 0f, inputs.MoveAxisForward), 1f);
            Vector3 originalMoveInputVector = moveInputVector;

            // Calculate camera direction and rotation on the character plane
            Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.forward, Motor.CharacterUp).normalized;
            if (cameraPlanarDirection.sqrMagnitude == 0f)
            {
                cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.up, Motor.CharacterUp).normalized;
            }
            Quaternion cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, Motor.CharacterUp);

            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        if (inputs.Attack && _timeSinceJumpRequested <= AttackRegisterTimer && _timeSinceLastAttack >= CanAttackTimer)
                        {
                            _attackRequested = true;
                            _timeSinceAttackLastRequested = 0f;
                        }

                        moveInputVector = (cameraPlanarRotation * moveInputVector).normalized;
                        // Move and look inputs
                        if (_wallJumpTimer > 0f)
                        {
                            _moveInputVector = _currentWallNormal * (Mathf.Clamp(_wallJumpTimer, 0, WallJumpDuration) / WallJumpDuration);
                        }
                        else if (_wallSlideTimer > 0f)
                        {
                            _moveInputVector = -_lastWallNormal;
                        }
                        else
                        {
                            _moveInputVector = moveInputVector;
                        }
                        Vector3 motorVelocity = Vector3.ProjectOnPlane(Motor.BaseVelocity, Motor.CharacterUp);
                        Vector3 castDir = (_moveInputVector.sqrMagnitude > 0f) ? _moveInputVector : motorVelocity.normalized;
                        if (_wallSliding)
                        {
                            castDir = transform.forward;
                        }
                        RaycastHit wallHit = new RaycastHit();
                        if (_timeSinceLastGroundJump > WallSlideJumpCooldownTimer &&
                                _wallJumpTimer <= WallJumpCooldown && (_wallJumpTimer > 0f || originalMoveInputVector.sqrMagnitude > 0f || motorVelocity.sqrMagnitude > 0f) &&
                                !(AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround) &&
                            Physics.CapsuleCast(BottomWallSlidePivot.position, TopWallSlidePivot.position, Motor.Capsule.radius * WallSlideCheckRadius, 
                                castDir, out wallHit, WallSlideDistance, _wallSlideMask, QueryTriggerInteraction.Ignore))
                        {
                        if (!_wallSliding)
                        {
                            _wallSliding = true;
                            _wallSlideTimer = WallSlideDuration;
                            _wallSlideUnstickTimer = 0f;
                        }
                            _lastWallNormal = Vector3.ProjectOnPlane(wallHit.normal, Motor.CharacterUp).normalized;
                        }
                        else
                        {
                            _wallSliding = false;
                            _wallSlideTimer = 0f;
                        }

                        _pointDir = moveInputVector;

                        if (_wallSlideTimer > 0f == true && moveInputVector.sqrMagnitude > 0f && Vector3.Angle(moveInputVector, -_lastWallNormal) > 90f)
                        {
                            _pointDir = moveInputVector;
                            _initUnstick = true;
                            _wallSlideUnstickTimer += Time.deltaTime;
                            if (_wallSlideUnstickTimer >= WallSlideStickyTime)
                            {
                                _wallSlideTimer = 0f;
                                _wallSliding = false;
                                _wallSlideUnstickTimer = 0f;
                            }
                        }
                        else if (_initUnstick)
                        {
                            _initUnstick = false;
                            _wallSliding = false;
                            _wallSlideTimer = 0f;
                            _wallSlideUnstickTimer = 0f;
                        }


                        switch (OrientationMethod)
                        {
                            case OrientationMethod.TowardsCamera:
                                _lookInputVector = cameraPlanarDirection;
                                break;
                            case OrientationMethod.TowardsMovement:
                                _lookInputVector = _moveInputVector; //.normalized;
                                break;
                        }

                        // Jumping input
                        if (inputs.JumpDown)
                        {
                            _timeSinceJumpRequested = 0f;
                            _jumpRequested = true;
                        }

                        // Crouching input
                        if (inputs.CrouchDown)
                        {
                            _shouldBeCrouching = true;

                            if (!_isCrouching)
                            {
                                _isCrouching = true;
                                Motor.SetCapsuleDimensions(0.5f, CrouchedCapsuleHeight, CrouchedCapsuleHeight * 0.5f);
                                MeshRoot.localScale = new Vector3(1.25f, 0.5f, 1.25f);
                            }
                        }
                        else if (inputs.CrouchUp)
                        {
                            _shouldBeCrouching = false;
                        }

                        break;
                    }
            }
        }

        /// <summary>
        /// This is called every frame by the AI script in order to tell the character what its inputs are
        /// </summary>
        public void SetInputs(ref AICharacterInputs inputs)
        {
            _moveInputVector = inputs.MoveVector;
            _lookInputVector = inputs.LookVector;
        }

        private Quaternion _tmpTransientRot;

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is called before the character begins its movement update
        /// </summary>
        public void BeforeCharacterUpdate(float deltaTime)
        {
        }

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is where you tell your character what its rotation should be right now. 
        /// This is the ONLY place where you should set the character's rotation
        /// </summary>
        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        if (_lookInputVector.sqrMagnitude > 0f && OrientationSharpness > 0f)
                        {
                            // Smoothly interpolate from current to target look direction
                            Vector3 smoothedLookInputDirection = Vector3.Slerp(Motor.CharacterForward, _lookInputVector, 1 - Mathf.Exp(-OrientationSharpness * deltaTime)).normalized;

                            // Set the current rotation (which will be used by the KinematicCharacterMotor)
                            currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, Motor.CharacterUp);
                        }

                        Vector3 currentUp = (currentRotation * Vector3.up);
                        if (BonusOrientationMethod == BonusOrientationMethod.TowardsGravity)
                        {
                            // Rotate from current up to invert gravity
                            Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, -Gravity.normalized, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
                            currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
                        }
                        else if (BonusOrientationMethod == BonusOrientationMethod.TowardsGroundSlopeAndGravity)
                        {
                            if (Motor.GroundingStatus.IsStableOnGround)
                            {
                                Vector3 initialCharacterBottomHemiCenter = Motor.TransientPosition + (currentUp * Motor.Capsule.radius);

                                Vector3 smoothedGroundNormal = Vector3.Slerp(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
                                currentRotation = Quaternion.FromToRotation(currentUp, smoothedGroundNormal) * currentRotation;

                                // Move the position to create a rotation around the bottom hemi center instead of around the pivot
                                Motor.SetTransientPosition(initialCharacterBottomHemiCenter + (currentRotation * Vector3.down * Motor.Capsule.radius));
                            }
                            else
                            {
                                Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, -Gravity.normalized, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
                                currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
                            }
                        }
                        else
                        {
                            Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, Vector3.up, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
                            currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is where you tell your character what its velocity should be right now. 
        /// This is the ONLY place where you can set the character's velocity
        /// </summary>
        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        if (_attackRequested == true)
                        {
                            _attackRequested = false;
                            _timeSinceLastAttack = 0f;
                            _attackTimer = AttackDuration;
                        }

                        _timeSinceLastAttack += deltaTime;
                        _timeSinceAttackLastRequested += deltaTime;

                        if (_attackTimer >= 0)
                        {
                            _attackTimer -= deltaTime;
                            _attackCollider.enabled = true;
                            Attacking = true;
                        }
                        else
                        {
                            _attackCollider.enabled = false;
                            Attacking = false;
                        }

                        // Ground movement
                        if (Motor.GroundingStatus.IsStableOnGround)
                        {
                            float currentVelocityMagnitude = currentVelocity.magnitude;

                            Vector3 effectiveGroundNormal = Motor.GroundingStatus.GroundNormal;

                            // Reorient velocity on slope
                            currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, effectiveGroundNormal) * currentVelocityMagnitude;

                            // Calculate target velocity
                            Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
                            Vector3 reorientedInput = Vector3.Cross(effectiveGroundNormal, inputRight).normalized * _moveInputVector.magnitude;
                            Vector3 targetMovementVelocity = reorientedInput * MaxStableMoveSpeed;

                            // Smooth movement Velocity
                            currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1f - Mathf.Exp(-StableMovementSharpness * deltaTime));
                        }
                        // Air movement
                        else
                        {
                            // Add move input
                            if (_moveInputVector.sqrMagnitude > 0f)
                            {
                                Vector3 addedVelocity = _moveInputVector * AirAccelerationSpeed * deltaTime;

                                Vector3 currentVelocityOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);

                                // Limit air velocity from inputs
                                if (currentVelocityOnInputsPlane.magnitude < MaxAirMoveSpeed)
                                {
                                    // clamp addedVel to make total vel not exceed max vel on inputs plane
                                    Vector3 newTotal = Vector3.ClampMagnitude(currentVelocityOnInputsPlane + addedVelocity, MaxAirMoveSpeed);
                                    addedVelocity = newTotal - currentVelocityOnInputsPlane;
                                }
                                else
                                {
                                    // Make sure added vel doesn't go in the direction of the already-exceeding velocity
                                    if (Vector3.Dot(currentVelocityOnInputsPlane, addedVelocity) > 0f)
                                    {
                                        addedVelocity = Vector3.ProjectOnPlane(addedVelocity, currentVelocityOnInputsPlane.normalized);
                                    }
                                }

                                // Prevent air-climbing sloped walls
                                if (Motor.GroundingStatus.FoundAnyGround)
                                {
                                    if (Vector3.Dot(currentVelocity + addedVelocity, addedVelocity) > 0f)
                                    {
                                        Vector3 perpenticularObstructionNormal = Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp).normalized;
                                        addedVelocity = Vector3.ProjectOnPlane(addedVelocity, perpenticularObstructionNormal);
                                    }
                                }

                                // Apply added velocity
                                currentVelocity += addedVelocity;
                            }

                            // Gravity
                            currentVelocity += Gravity * deltaTime;

                            // Drag
                            float wallMod = WallSlideDrag_mod * (Mathf.Clamp(_wallSlideTimer, 0, WallSlideDuration) / WallSlideDuration);
                            currentVelocity *= (1f / (1f + ((Drag + wallMod) * deltaTime)));
                        }

                        // Handle jumping
                        _jumpedThisFrame = false;
                        _timeSinceJumpRequested += deltaTime;
                        if (_jumpRequested)
                        {
                            // See if we actually are allowed to ground jump
                            if (!_jumpConsumed)
                            {
                                Vector3 jumpDirection = Motor.CharacterUp;

                                // Handle wall jump
                                if (_timeSinceLastAbleToJump <= JumpPostGroundingGraceTime && AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround)
                                {
                                    _timeSinceLastGroundJump = 0f;
                                    // Calculate jump direction before ungrounding
                                    if (Motor.GroundingStatus.FoundAnyGround && !Motor.GroundingStatus.IsStableOnGround)
                                    {
                                        jumpDirection = Motor.GroundingStatus.GroundNormal;
                                    }
                                    _jumpConsumed = true;
                                }
                                else if (AllowWallJump && _wallSlideTimer > 0f && _wallJumpTimer <= WallJumpCooldown) 
                                {
                                    _wallJumpTimer = WallJumpDuration;

                                    _pointDir = Vector3.ProjectOnPlane(_pointDir, _lastWallNormal);
                                    if (_pointDir.sqrMagnitude > 0f)
                                    {
                                        jumpDirection = (_pointDir + _lastWallNormal) / 2f;
                                    }
                                    else
                                    {
                                        jumpDirection = _lastWallNormal;
                                    }
                                    jumpDirection = jumpDirection.normalized;
                                    _currentWallNormal = jumpDirection;
                                    jumpDirection.y += UpwardsMovement;
                                    _jumpConsumed = true;
                                }

                                if (_jumpConsumed)
                                {
                                    _wallSlideTimer = 0f;
                                    // Makes the character skip ground probing/snapping on its next update. 
                                    // If this line weren't here, the character would remain snapped to the ground when trying to jump. Try commenting this line out and see.
                                    Motor.ForceUnground();

                                    // Add to the return velocity and reset jump state
                                    currentVelocity += (jumpDirection * JumpUpSpeed) - Vector3.Project(currentVelocity, Motor.CharacterUp);
                                    currentVelocity += (_moveInputVector * JumpScalableForwardSpeed);
                                    _jumpRequested = false;
                                    _jumpedThisFrame = true;
                                }
                            }
                        }

                        // Take into account additive velocity
                        if (_internalVelocityAdd.sqrMagnitude > 0f)
                        {
                            currentVelocity += _internalVelocityAdd;
                            _internalVelocityAdd = Vector3.zero;
                        }
                        break;
                    }
            }
        }

        /*
        private bool _attackRequested;
        private bool _attackConsumed;
        private float _timeSinceAttackLastRequested;
        private float _timeSinceLastAttack;
        public float AttackTimer;
        public float AttackDuration;
        public float AttackRegisterTimer;
        // if time since attack requested > register timer, reset
        // if time since attack requested < register timer and time since last attack > attack period, attack 
         */

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is called after the character has finished its movement update
        /// </summary>
        public void AfterCharacterUpdate(float deltaTime)
        {
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        // Handle jump-related values
                        {

                            _timeSinceLastGroundJump += Time.deltaTime;



                            // Handle jumping pre-ground grace period
                            if (_jumpRequested && _timeSinceJumpRequested > JumpPreGroundingGraceTime)
                            {
                                _jumpRequested = false;
                            }

                            if (_wallJumpTimer > 0f)
                            {
                                _wallJumpTimer -= Time.deltaTime;
                            }

                            if (_wallSlideTimer > 0f && WallSlideFade)
                            {
                                _wallSlideTimer -= Time.deltaTime;
                            }


                            // If we're on a ground surface, reset jumping values
                            if (!_jumpedThisFrame)
                            {
                                _jumpConsumed = false;
                            }

                            if (AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround)
                            {
                                _timeSinceLastAbleToJump = 0f;
                                _wallJumpTimer = 0f;
                                _wallSlideTimer = 0f;
                            }
                            else
                            {
                                // Keep track of time since we were last able to jump (for grace period)
                                _timeSinceLastAbleToJump += deltaTime;
                            }
                        }

                        // Handle uncrouching
                        if (_isCrouching && !_shouldBeCrouching)
                        {
                            // Do an overlap test with the character's standing height to see if there are any obstructions
                            Motor.SetCapsuleDimensions(0.5f, 2f, 1f);
                            if (Motor.CharacterOverlap(
                                Motor.TransientPosition,
                                Motor.TransientRotation,
                                _probedColliders,
                                Motor.CollidableLayers,
                                QueryTriggerInteraction.Ignore) > 0)
                            {
                                // If obstructions, just stick to crouching dimensions
                                Motor.SetCapsuleDimensions(0.5f, CrouchedCapsuleHeight, CrouchedCapsuleHeight * 0.5f);
                            }
                            else
                            {
                                // If no obstructions, uncrouch
                                MeshRoot.localScale = new Vector3(1f, 1f, 1f);
                                _isCrouching = false;
                            }
                        }
                        break;
                    }
            }
        }

        public void PostGroundingUpdate(float deltaTime)
        {
            // Handle landing and leaving ground
            if (Motor.GroundingStatus.IsStableOnGround && !Motor.LastGroundingStatus.IsStableOnGround)
            {
                OnLanded();
            }
            else if (!Motor.GroundingStatus.IsStableOnGround && Motor.LastGroundingStatus.IsStableOnGround)
            {
                OnLeaveStableGround();
            }
        }

        public bool IsColliderValidForCollisions(Collider coll)
        {
            if (IgnoredColliders.Count == 0)
            {
                return true;
            }

            if (IgnoredColliders.Contains(coll))
            {
                return false;
            }

            return true;
        }

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        public void AddVelocity(Vector3 velocity)
        {
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        _internalVelocityAdd += velocity;
                        break;
                    }
            }
        }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
        }

        protected void OnLanded()
        {
        }

        protected void OnLeaveStableGround()
        {
        }

        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
        }
    }
}