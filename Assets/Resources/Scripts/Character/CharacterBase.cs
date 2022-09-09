using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Input = GameEngine.UserInput.Input;

namespace GameEngine.Core
{
    public enum CharacterState
    {
        Idle,
        Walk,
        Dash
    }

    public class CharacterBase : MonoBehaviour
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
        protected float _dashMaxSpeed = 10f;

        [SerializeField]
        protected float _dashLeadTime = 1f;

        [Header("Gravity Settings")]
        private float _gravity = 9.8f;
        protected float CurrentVerticalSpeed { get; private set; }

        protected virtual void Start()
        {
            _transform = GetComponent<Transform>();
            _characterController = GetComponent<CharacterController>();
        }

        protected virtual void Update()
        {
            UpdateMovement();
        }

        protected virtual void FixedUpdate()
        {
            HandleGravity();
        }

        private void UpdateMovement()
        {
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
            if (_characterController.isGrounded)
            {
                CurrentVerticalSpeed = 0;
                return;
            }

            CurrentVerticalSpeed -= _gravity * Time.deltaTime;
            _velocity.y = CurrentVerticalSpeed;
        }

        protected virtual void HandleHorizontalMovement(Vector3 axis)
        {
            if (axis.magnitude < 0.1) return;

            SetHorizontalVelocity(_movementSpeed * (_transform.rotation * axis));
        }

        private IEnumerator HandleDashCoroutine()
        {
            _currentState = CharacterState.Dash;

            float dashCurrentTime = 0;
            float dashCurrentSpeed = _dashMaxSpeed;

            while (dashCurrentTime < _dashLeadTime)
            {
                SetHorizontalVelocity(dashCurrentSpeed / _dashLeadTime * _transform.forward);

                dashCurrentTime += Time.deltaTime;

                yield return null;
            }

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
    }
}