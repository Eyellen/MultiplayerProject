using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Input = GameEngine.UserInput.Input;
using Mirror;

namespace GameEngine.Core
{
    public struct InputMsg
    {
        public uint tick;
        public Vector3 movementInput;
        public bool isDashPressed;
    }

    public struct StateMsg
    {
        public uint tick;
        public Vector3 position;
    }

    public enum CharacterState
    {
        Idle,
        Walk,
        Dash
    }

    public class CharacterBase : NetworkBehaviour
    {
        protected Transform _transform;
        private CharacterController _characterController;

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
        private Vector3 _movementInput;
        private bool _isDashPressed;
        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            //  Clamping values to avoid errors
            _dashStartSpeed = _dashStartSpeed < 0.1f ? 0.1f : _dashStartSpeed;
            _dashEndSpeed = _dashEndSpeed < 0.1f ? 0.1f : _dashEndSpeed;

            _dampAfterDistance = _dampAfterDistance > _dashDistance ? _dashDistance : _dampAfterDistance;
            _dampAfterDistance = _dampAfterDistance < 0 ? 0 : _dampAfterDistance;
        }
#endif

        public override void OnStartClient()
        {
            // Assigning timer to be in sync with server
            _timer = (float)NetworkTime.time;
        }

        protected virtual void Start()
        {
            _transform = GetComponent<Transform>();
            _characterController = GetComponent<CharacterController>();

            _minTimeBetweenTicks = 1f / NetworkManager.singleton.serverTickRate;

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
            HandleGravity();

            if (isLocalPlayer)
            {
                if (!_latestServerState.Equals(default(StateMsg)) &&
                    (_lastProcessedState.Equals(default(StateMsg)) ||
                    !_latestServerState.Equals(_lastProcessedState)))
                {
                    HandleServerReconciliation();
                }

                uint bufferIndex = _currentTick % BUFFER_SIZE;

                InputMsg inputMsg = new InputMsg();
                {
                    inputMsg.tick = _currentTick;
                    inputMsg.movementInput = _movementInput;
                    inputMsg.isDashPressed = _isDashPressed;
                }
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
            if (Mathf.Abs(Input.MovementVector.magnitude) >= 0.1)
                _movementInput = Input.MovementVector;

            if (Input.IsDashPressed)
                _isDashPressed = true;
        }

        [Client]
        private void ResetInputs()
        {
            _movementInput = default(Vector3);
            _isDashPressed = default(bool);
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
        [Command(requiresAuthority = false)]
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
                        if (Mathf.Abs(inputMsg.movementInput.magnitude) >= 0.1)
                        {
                            CurrentState = CharacterState.Walk;
                            break;
                        }

                        if (inputMsg.isDashPressed)
                        {
                            CurrentState = CharacterState.Dash;
                            break;
                        }

                        break;
                    }

                case CharacterState.Walk:
                    {
                        if (Mathf.Abs(inputMsg.movementInput.magnitude) < 0.1)
                        {
                            CurrentState = CharacterState.Idle;
                            break;
                        }

                        if (inputMsg.isDashPressed)
                        {
                            CurrentState = CharacterState.Dash;
                            break;
                        }

                        HandleWalkState(inputMsg.movementInput);

                        break;
                    }

                case CharacterState.Dash:
                    {
                        HandleDashState();
                        break;
                    }

                default:
                    {
                        break;
                    }
            }
        }

        private void HandleGravity()
        {
            if (IsGrounded())
            {
                _currentVerticalSpeed = 0;
                return;
            }

            _currentVerticalSpeed -= _gravity * (_minTimeBetweenTicks * _minTimeBetweenTicks);
            Move(new Vector3(0, _currentVerticalSpeed, 0));
        }

        protected virtual void HandleWalkState(Vector3 movementInput)
        {
            Move(_movementSpeed * _minTimeBetweenTicks * (_transform.rotation * movementInput));
        }

        // Not suitable because can't sync Coroutines with ServerTick()
        //private IEnumerator HandleDashCoroutine()
        //{
        //    CurrentState = CharacterState.Dash;
        //    uint tick = 0;

        //    Vector3 targetPosition = _transform.position + _transform.forward * _dashDistance;

        //    float traveledDistance;
        //    float currentSpeed = _dashStartSpeed;

        //    for (traveledDistance = 0; traveledDistance < _dashDistance; /*traveledDistance += currentSpeed * _minTimeBetweenTicks*/)
        //    {
        //        // Here we wait for next server tick
        //        while (tick == _currentTick)
        //        {
        //            yield return null;
        //        }

        //        tick = _currentTick;

        //        // Exit the coroutine if character is no longer in Dash state
        //        if (CurrentState != CharacterState.Dash)
        //        {
        //            Debug.Log("Exit Dash state");
        //            yield break;
        //        }


        //        // Damping the speed after _dampAfterDistance
        //        if (traveledDistance > _dampAfterDistance)
        //        {
        //            float dampingRatio = (traveledDistance - _dampAfterDistance) / (_dashDistance - _dampAfterDistance);

        //            currentSpeed = Mathf.Lerp(_dashStartSpeed, _dashEndSpeed, dampingRatio);
        //        }

        //        // Moving the character by currentSpeed in local forward direction
        //        Move(currentSpeed * _minTimeBetweenTicks * _transform.forward);
        //        traveledDistance += currentSpeed * _minTimeBetweenTicks;
        //        Debug.Log($"Traveled in this server tick: {traveledDistance - currentSpeed * _minTimeBetweenTicks}");

        //        //// Waiting for _minTimeBetweenTicks because our movement should execute in sync with ServerTick()
        //        //yield return new WaitForSeconds(_minTimeBetweenTicks);

        //        // Here we check if character will be further than it should be in the next ServerTick()
        //        // And if he will be then we move him to targetPosition and exit the loop
        //        if (traveledDistance + (currentSpeed * _minTimeBetweenTicks) > _dashDistance)
        //        {
        //            Move(targetPosition - _transform.position);
        //            traveledDistance = _dashDistance;
        //        }

        //        //yield return null;
        //    }

        //    CurrentState = CharacterState.Idle;
        //}

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
        }

        protected void Move(Vector3 motion)
        {
            _characterController.Move(motion);
        }

        protected bool IsGrounded()
        {
            Ray ray = new Ray
            {
                origin = _transform.position + (_characterController.center - Vector3.up * (_characterController.height / 2)),
                direction = -Vector3.up
            };

            return Physics.Raycast(ray, maxDistance: 0.05f);
        }

        [Client]
        private void HandleServerReconciliation()
        {
            _lastProcessedState = _latestServerState;

            uint serverStateBufferIndex = _latestServerState.tick % BUFFER_SIZE;
            float positionError = Vector3.Distance(_latestServerState.position, _stateBuffer[serverStateBufferIndex].position);

            if (positionError < 0.001f) return;

#if UNITY_EDITOR
            Debug.Log("Reconsiling!");
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