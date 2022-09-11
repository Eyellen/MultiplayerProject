using System.Collections;
using UnityEngine;
using Input = GameEngine.UserInput.Input;
using Mirror;

namespace GameEngine.Core
{
    public enum CharacterState
    {
        Idle,
        Walk,
        Dash
    }

    public class CharacterBase : NetworkBehaviour
    {
        protected Transform _transform;
        protected CharacterController _characterController;

        private CharacterState _currentState = CharacterState.Idle;

        protected Vector3 _velocity;
        public Vector3 Velocity { get => _velocity; }

        [Header("Movement Settings")]
        [SerializeField]
        protected float _movementSpeed = 5f;

        [Header("Dash Settings")]
        [SerializeField]
        protected float _dashDistance = 7f;

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

        protected virtual void Start()
        {
            _transform = GetComponent<Transform>();
            _characterController = GetComponent<CharacterController>();
        }

        protected virtual void Update()
        {
            UpdateMovement();
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (_currentState == CharacterState.Dash)
                _currentState = CharacterState.Idle;
        }

        private void UpdateMovement()
        {
            HandleGravity();

            HandleStates();

            _characterController.Move(_velocity * Time.deltaTime);
        }

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

            CurrentVerticalSpeed -= _gravity * Time.deltaTime;
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