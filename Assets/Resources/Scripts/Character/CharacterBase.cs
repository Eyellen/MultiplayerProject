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
        public Vector3 movementVector;
        public float movementAngle;
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

        private CharacterState _currentState = CharacterState.Idle;

        protected Vector3 _velocity;
        public Vector3 Velocity { get => _velocity; }

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
        protected float CurrentVerticalSpeed { get; private set; }

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
        //private StateMsg _latestServerState;
        //private StateMsg _lastProcessedState;
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
            _timer += Time.deltaTime;

            while (_timer > _minTimeBetweenTicks)
            {
                _timer -= _minTimeBetweenTicks;
                ServerTick();
                _currentTick++;
            }
        }

        /// <summary>
        /// This method executes once every _minTimeBetweenTicks
        /// </summary>
        protected virtual void ServerTick()
        {
            if (isLocalPlayer)
            {
                UpdateVelocity();

                uint bufferIndex = _currentTick % BUFFER_SIZE;

                InputMsg inputMsg = new InputMsg();
                inputMsg.tick = _currentTick;
                inputMsg.movementVector = _velocity;
                inputMsg.movementAngle = _transform.rotation.y;
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
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (_currentState == CharacterState.Dash)
                _currentState = CharacterState.Idle;
        }

        private StateMsg ProcessMovement(InputMsg inputMsg)
        {
            //_characterController.Move(Quaternion.Euler(0, inputMsg.movementAngle, 0) * inputMsg.movementVector * _minTimeBetweenTicks);
            _characterController.Move(inputMsg.movementVector * _minTimeBetweenTicks);

            return new StateMsg
            {
                tick = _currentTick,
                position = _transform.position,
            };
        }

        #region Network Messages and CallBacks
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
            //_latestServerState = serverState;
        }
        #endregion

        [Client]
        private void UpdateVelocity()
        {
            if (!isLocalPlayer) return;

            HandleGravity();

            HandleStates();
        }

        [Client]
        private void HandleStates()
        {
            // Simple implementation of StateMachine
            switch (_currentState)
            {
                case CharacterState.Idle:
                    {
                        if (Mathf.Abs(Input.MovementVector.magnitude) >= 0.1)
                        {
                            _currentState = CharacterState.Walk;
                            break;
                        }

                        if (Input.IsDashPressed)
                        {
                            _currentState = CharacterState.Dash;
                            StartCoroutine(HandleDashCoroutine());
                            break;
                        }

                        break;
                    }

                case CharacterState.Walk:
                    {
                        if (Mathf.Abs(Input.MovementVector.magnitude) < 0.1)
                        {
                            _currentState = CharacterState.Idle;
                            SetHorizontalVelocity(Vector3.zero);
                            break;
                        }

                        if (Input.IsDashPressed)
                        {
                            _currentState = CharacterState.Dash;
                            StartCoroutine(HandleDashCoroutine());
                            break;
                        }

                        HandleHorizontalMovement(Input.MovementVector);

                        break;
                    }

                case CharacterState.Dash:
                    {
                        break;
                    }

                default:
                    {
                        break;
                    }
            }
        }

        protected void HandleGravity()
        {
            if (IsGrounded())
            {
                CurrentVerticalSpeed = 0;
                _velocity.y = 0;
                return;
            }

            CurrentVerticalSpeed -= _gravity * _minTimeBetweenTicks;
            _velocity.y += CurrentVerticalSpeed;
        }

        protected virtual void HandleHorizontalMovement(Vector3 axis)
        {
            if (axis.magnitude < 0.1) return;

            SetHorizontalVelocity(_movementSpeed * (_transform.rotation * axis));
        }

        private IEnumerator HandleDashCoroutine()
        {
            _currentState = CharacterState.Dash;

            Vector3 targetPosition = _transform.position + _transform.forward * _dashDistance;

            float traveledDistance;
            float currentSpeed = _dashStartSpeed;

            for (traveledDistance = 0; traveledDistance < _dashDistance; traveledDistance += currentSpeed * Time.deltaTime)
            {
                //  Reset velocity if dash state has been exited
                if (_currentState != CharacterState.Dash)
                {
                    SetHorizontalVelocity(Vector3.zero);
                    yield break;
                }

                if (traveledDistance > _dampAfterDistance)
                {
                    float dampingRatio = (traveledDistance - _dampAfterDistance) / (_dashDistance - _dampAfterDistance);

                    currentSpeed = Mathf.Lerp(_dashStartSpeed, _dashEndSpeed, dampingRatio);
                }

                SetHorizontalVelocity(currentSpeed * _transform.forward);

                yield return null;
            }

            //  Manually setting position.
            //      Because Time.deltaTime is not accurate.
            //      Also we skip 1 frame before we check if we traveled enough
            //          Therefore it travels for 1 extra frame.
            _transform.position = targetPosition;

            SetHorizontalVelocity(Vector3.zero);
            _currentState = CharacterState.Idle;
        }

        /// <summary>
        /// Sets _velocity.x and _velocity.z from "velocity" parameter.
        /// The _velocity.y is ignored.
        /// </summary>
        /// <param name="velocity">velocity</param>
        protected void SetHorizontalVelocity(Vector3 velocity)
        {
            _velocity.x = velocity.x;
            _velocity.z = velocity.z;
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
    }
}