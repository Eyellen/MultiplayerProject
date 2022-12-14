using System.Collections.Generic;
using UnityEngine;
using Input = GameEngine.UserInput.Input;
using Mirror;

namespace GameEngine.Core
{
    public class CharacterBase : NetworkBehaviour
    {
        #region NetworkMessage types
        public struct InputMsg
        {
            public uint tick;
            public Vector3 movementInput;
            public float relativeToAngle;
            public bool isDashPressed;
        }

        public struct StateMsg
        {
            public uint tick;
            public Vector3 position;
        }
        #endregion

        public enum CharacterState
        {
            Idle,
            Fall,
            Run,
            Dash
        }

        private struct AnimatorStates
        {
            public static string FALL_KEYWORD = "IsFalling";
            public static string RUN_KEYWORD = "IsRunning";
            public static string DASH_KEYWORD = "IsDashing";
        }

#if UNITY_EDITOR
        [SerializeField]
        private bool _isDebugging = false;
#endif

        [Header("Components")]
        protected Transform _transform;
        private CharacterController _characterController;
        
        [SerializeField]
        private Animator _animator;

        [field: SyncVar]
        public CharacterState CurrentState { get; private set; } = CharacterState.Idle;

        [Header("Movement Settings")]
        [SerializeField]
        protected float _movementSpeed = 5f;

        [Header("Dash Settings")]
        [SerializeField]
        private float _dashDistance = 7f;

        /// <summary>
        /// Can't be less than 0.1f because in this case it will not be able to reach target position
        /// </summary>
        [SerializeField]
        [Tooltip("Can't be less than 0.1.")]
        private float _dashStartSpeed = 10f;

        /// <summary>
        /// Can't be less than 0.1f because in this case it will not be able to reach target position
        /// </summary>
        [SerializeField]
        [Tooltip("Can't be less than 0.1.")]
        private float _dashEndSpeed = 0.5f;

        [SerializeField]
        [Tooltip("Can't be less than 0 and more than \"Dash Distance\".")]
        private float _dampAfterDistance = 5f;

        [Header("Gravity Settings")]
        private float _gravity = 9.8f;
        private float _currentVerticalSpeed;

        #region Server and Client
        private float _timer;
        private uint _currentTick { get; set; }

        /// <summary>
        /// Min time between server ticks.
        /// The value is 1.0f / NetworkManager.singleton.serverTickRate
        /// </summary>
        protected float _minTimeBetweenTicks { get; private set; }

        private const uint BUFFER_SIZE = 1024;
        #endregion

        #region Server Side
        private Queue<InputMsg> _inputQueue;
        #endregion

        #region Client Side
        private InputMsg[] _inputBuffer;
        private StateMsg[] _stateBuffer;
        private StateMsg _latestServerState;
        private StateMsg _lastProcessedState;
        #endregion

        #region Inputs
        private bool _isDashPressed;
        #endregion

        private int _layer;

#if UNITY_EDITOR
        private void OnValidate()
        {
            //  Clamping values to avoid errors
            _dashStartSpeed = _dashStartSpeed < 0.1f ? 0.1f : _dashStartSpeed;
            _dashEndSpeed = _dashEndSpeed < 0.1f ? 0.1f : _dashEndSpeed;

            _dampAfterDistance = _dampAfterDistance > _dashDistance ? _dashDistance : _dampAfterDistance;
            _dampAfterDistance = _dampAfterDistance < 0 ? 0 : _dampAfterDistance;
        }

        private void OnDrawGizmos()
        {
            if (_isDebugging)
            {
                float capsuleRadius = _characterController.radius;
                Vector3 topSphere = _transform.position + _characterController.center;

                float distanceToBottomSphereCenter = (_characterController.height / 2) + _characterController.skinWidth +
                    _characterController.stepOffset - capsuleRadius;

                Vector3 bottomSphere = _transform.position +
                    (_characterController.center - Vector3.up * distanceToBottomSphereCenter);

                Gizmos.color = Color.red;
                GameEngine.Debugging.Gizmos.DrawWireCapsule(topSphere, bottomSphere, capsuleRadius);
            }
        }
#endif

        protected virtual void Start()
        {
            _layer = 1 << LayerMask.NameToLayer("Player");

            _transform = GetComponent<Transform>();
            _characterController = GetComponent<CharacterController>();

            _minTimeBetweenTicks = 1f / NetworkManager.singleton.serverTickRate;
            syncInterval = _minTimeBetweenTicks;

            // No need to allocate memory for buffers if this is not local player or server
            if (!isLocalPlayer && !isServer) return;

            if (isLocalPlayer)
                _inputBuffer = new InputMsg[BUFFER_SIZE];
            else if (isServer)
                _inputQueue = new Queue<InputMsg>();
            _stateBuffer = new StateMsg[BUFFER_SIZE];
        }

        protected virtual void Update()
        {
            if (isLocalPlayer)
            {
                HandleInputs();
            }

            #region ServerTick
            _timer += Time.deltaTime;

            while (_timer > _minTimeBetweenTicks)
            {
                _timer -= _minTimeBetweenTicks;
                ServerTick();
                _currentTick++;
            }
            #endregion
        }

        /// <summary>
        /// This method executes once every _minTimeBetweenTicks
        /// </summary>
        protected virtual void ServerTick()
        {
            if (isLocalPlayer)
            {
                if (!_latestServerState.Equals(default(StateMsg)) &&
                    (_lastProcessedState.Equals(default(StateMsg)) ||
                    !_latestServerState.Equals(_lastProcessedState)))
                {
                    HandleServerReconciliation();
                }

                uint bufferIndex = _currentTick % BUFFER_SIZE;

                InputMsg inputMsg = ProcessInput();
                _inputBuffer[bufferIndex] = inputMsg;

                _stateBuffer[bufferIndex] = ProcessMovement(inputMsg);

                CmdSendInputMsg(inputMsg);
            }
            else if (isServer)
            {
                uint? bufferIndex = null;
                while (_inputQueue.Count > 1)
                {
                    InputMsg inputMsg = _inputQueue.Dequeue();

                    bufferIndex = inputMsg.tick % BUFFER_SIZE;

                    StateMsg stateMsg = ProcessMovement(inputMsg);
                    _stateBuffer[bufferIndex.Value] = stateMsg;
                }

                if (bufferIndex != null)
                {
                    TargetSendStateMsg(connectionToClient, _stateBuffer[bufferIndex.Value]);
                }
            }

            if (isLocalPlayer)
            {
                ResetInputs();
            }

            if (isServer)
            {
                RpcSendPositionToAllClients(_transform.position);
                RpcSendVerticalRotationToAllClients(_transform.eulerAngles.y);
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // Exit dash state if we hit some vertical obstacle
            if (CurrentState == CharacterState.Dash &&
                (_characterController.collisionFlags & CollisionFlags.Sides) != 0)
            {
                ExitDashState();
            }
        }

        [Client]
        private void HandleInputs()
        {
            if (Input.IsDashPressed)
                _isDashPressed = true;
        }

        [Client]
        private void ResetInputs()
        {
            _isDashPressed = default(bool);
        }

        protected virtual InputMsg ProcessInput()
        {
            return new InputMsg
            {
                tick = _currentTick,
                movementInput = Input.MovementVector,
                isDashPressed = _isDashPressed,
            };
        }

        private StateMsg ProcessMovement(InputMsg inputMsg)
        {
            HandleStates(inputMsg);

            return new StateMsg
            {
                tick = inputMsg.tick,
                position = _transform.position,
            };
        }

        #region Network Messages and Callbacks
        [Command]
        private void CmdSendInputMsg(InputMsg inputMsg)
        {
            OnClientInput(inputMsg);
        }

        [Server]
        private void OnClientInput(InputMsg inputMsg)
        {
            if (isLocalPlayer) return;

            _inputQueue.Enqueue(inputMsg);
        }

        [TargetRpc]
        private void TargetSendStateMsg(NetworkConnection connectionToClient, StateMsg stateMsg)
        {
            OnServerMovementState(stateMsg);
        }

        [Client]
        private void OnServerMovementState(StateMsg serverState)
        {
            _latestServerState = serverState;
        }
        #endregion

        private void HandleStates(InputMsg inputMsg)
        {
            // Simple implementation of StateMachine
            switch (CurrentState)
            {
                case CharacterState.Idle:
                    {
                        if (!IsGroundInStepReach())
                        {
                            CurrentState = CharacterState.Fall;
                            _animator.SetBool(AnimatorStates.FALL_KEYWORD, true);
                            break;
                        }

                        if (Mathf.Abs(inputMsg.movementInput.magnitude) >= 0.1)
                        {
                            CurrentState = CharacterState.Run;
                            _animator.SetBool(AnimatorStates.RUN_KEYWORD, true);
                            break;
                        }

                        if (inputMsg.isDashPressed)
                        {
                            CurrentState = CharacterState.Dash;
                            _animator.SetBool(AnimatorStates.DASH_KEYWORD, true);
                            break;
                        }

                        break;
                    }

                case CharacterState.Fall:
                    {
                        if (_characterController.isGrounded)
                        {
                            CurrentState = CharacterState.Idle;
                            _animator.SetBool(AnimatorStates.FALL_KEYWORD, false);
                            _currentVerticalSpeed = 0;
                            break;
                        }

                        HandleFallState();

                        break;
                    }

                case CharacterState.Run:
                    {
                        if (!IsGroundInStepReach())
                        {
                            CurrentState = CharacterState.Fall;
                            _animator.SetBool(AnimatorStates.RUN_KEYWORD, false);
                            _animator.SetBool(AnimatorStates.FALL_KEYWORD, true);
                            break;
                        }

                        if (Mathf.Abs(inputMsg.movementInput.magnitude) < 0.1)
                        {
                            CurrentState = CharacterState.Idle;
                            _animator.SetBool(AnimatorStates.RUN_KEYWORD, false);
                            break;
                        }

                        if (inputMsg.isDashPressed)
                        {
                            CurrentState = CharacterState.Dash;
                            _animator.SetBool(AnimatorStates.RUN_KEYWORD, false);
                            _animator.SetBool(AnimatorStates.DASH_KEYWORD, true);
                            break;
                        }

                        HandleWalkState(inputMsg);

                        break;
                    }

                case CharacterState.Dash:
                    {
                        if (!IsGroundInStepReach())
                        {
                            ExitDashState();
                            _animator.SetBool(AnimatorStates.DASH_KEYWORD, false);
                            _animator.SetBool(AnimatorStates.FALL_KEYWORD, true);
                            break;
                        }

                        HandleDashState();

                        break;
                    }

                default:
                    {
                        break;
                    }
            }

            // Simulating gravity pressure if not falling
            if (CurrentState != CharacterState.Fall)
                _characterController.Move(-Vector3.up * 0.1f);
        }

        private void HandleGravity()
        {
            if (IsGroundInStepReach())
            {
                _currentVerticalSpeed = 0;
                return;
            }

            _currentVerticalSpeed -= _gravity * (_minTimeBetweenTicks * _minTimeBetweenTicks);
            Move(new Vector3(0, _currentVerticalSpeed, 0));
        }

        private void HandleFallState()
        {
            _currentVerticalSpeed -= _gravity * (_minTimeBetweenTicks * _minTimeBetweenTicks);
            Move(new Vector3(0, _currentVerticalSpeed, 0));
        }

        protected virtual void HandleWalkState(InputMsg inputMsg)
        {
            Move(_movementSpeed * _minTimeBetweenTicks * (_transform.rotation * inputMsg.movementInput));
        }

        private bool _isDashing;
        private Vector3 _targetPosition;
        private float _traveledDistance;
        private float _currentDashSpeed;
        private void HandleDashState()
        {
            if (!_isDashing)
            {
                _isDashing = true;

                _targetPosition = _transform.position + _transform.forward * _dashDistance;
                _traveledDistance = 0;
                _currentDashSpeed = _dashStartSpeed;
            }

            if (_traveledDistance < _dashDistance)
            {
                // Damping the speed after _dampAfterDistance
                if (_traveledDistance > _dampAfterDistance)
                {
                    float dampingRatio = (_traveledDistance - _dampAfterDistance) / (_dashDistance - _dampAfterDistance);

                    _currentDashSpeed = Mathf.Lerp(_dashStartSpeed, _dashEndSpeed, dampingRatio);
                }

                // Moving the character by currentSpeed in local forward direction
                Move(_currentDashSpeed * _minTimeBetweenTicks * _transform.forward);
                _traveledDistance += _currentDashSpeed * _minTimeBetweenTicks;

                // Here we check if character will be further than it should be in the next ServerTick()
                // And if it will be then we move it to targetPosition and exit the state
                if (_traveledDistance + (_currentDashSpeed * _minTimeBetweenTicks) >= _dashDistance)
                {
                    Move(new Vector3(_targetPosition.x, _transform.position.y, _targetPosition.z) - _transform.position);
                    _traveledDistance = _dashDistance;
                    ExitDashState();
                }
            }
        }

        private void ExitDashState()
        {
            _isDashing = false;
            CurrentState = CharacterState.Idle;
            _animator.SetBool(AnimatorStates.DASH_KEYWORD, false);
        }

        protected void Move(Vector3 motion)
        {
            _characterController.Move(motion);
        }

        /// <summary>
        /// This method is almost the same as CharacterController.isGrounded but 
        /// it checks not if grounded but if ground is in CharacterController.stepOffset reach from 
        /// CharacterController's bottom.
        /// </summary>
        /// <returns>True if ground is in step reach, False otherwise.</returns>
        protected bool IsGroundInStepReach()
        {
            float capsuleRadius = _characterController.radius;
            Vector3 topSphere = _transform.position + _characterController.center;

            float distanceToBottomSphereCenter = (_characterController.height / 2) + _characterController.skinWidth +
                _characterController.stepOffset - capsuleRadius;

            Vector3 bottomSphere = _transform.position +
                (_characterController.center - Vector3.up * distanceToBottomSphereCenter);

            return Physics.CheckCapsule(topSphere, bottomSphere, capsuleRadius, ~_layer, QueryTriggerInteraction.Ignore);
        }

        [Client]
        private void HandleServerReconciliation()
        {
            _lastProcessedState = _latestServerState;

            uint serverStateBufferIndex = _latestServerState.tick % BUFFER_SIZE;
            float positionError = Vector3.Distance(_latestServerState.position, _stateBuffer[serverStateBufferIndex].position);

            if (positionError < 0.001f) return;

#if UNITY_EDITOR
            if (_isDebugging)
            {
                Debug.Log("Reconsiling!");
            }
#endif

            // Here instead of assigning _transform.position we should use _characterController.Move()
            //      Because _characterController overrides transform values
            //_transform.position = _latestServerState.position;
            Move(_latestServerState.position - _transform.position);

            _stateBuffer[serverStateBufferIndex] = _latestServerState;

            uint tickToProcess = _latestServerState.tick + 1;

            while (tickToProcess < _currentTick)
            {
                uint bufferIndex = tickToProcess % BUFFER_SIZE;

                StateMsg stateMsg = ProcessMovement(_inputBuffer[bufferIndex]);

                _stateBuffer[bufferIndex] = stateMsg;

                tickToProcess++;
            }
        }

        [ClientRpc]
        private void RpcSendPositionToAllClients(Vector3 position)
        {
            // Ignore this if we are local player
            //      Because it will interfere with the Client Prediction
            if (isLocalPlayer) return;

            if (!_transform) return;

            _transform.position = position;
        }

        [ClientRpc]
        private void RpcSendVerticalRotationToAllClients(float yRotation)
        {
            // Ignore this if we are local player
            //      Because it will interfere with the Client Prediction
            if (isLocalPlayer) return;

            if (!_transform) return;

            _transform.rotation = Quaternion.Euler(0, yRotation, 0);
        }
    }
}