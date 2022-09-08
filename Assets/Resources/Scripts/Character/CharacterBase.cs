using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterBase : MonoBehaviour
{
    protected Transform _transform;
    protected CharacterController _characterController;

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
        HandleDash();
    }

    private void UpdateMovement()
    {
        _velocity = Vector3.zero;

        Move(PlayerInput.MovementVector);
        HandleGravity();

        _characterController.Move(_velocity * Time.deltaTime);
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

    protected virtual void Move(Vector3 axis)
    {
        if (axis.magnitude < 0.1) return;

        _velocity = _movementSpeed * (_transform.rotation * axis);

        // Reassign vertical speed
        _velocity.y = CurrentVerticalSpeed;
    }

    private void HandleDash()
    {
        if (PlayerInput.IsDashPressed)
            StartCoroutine(HandleDashCoroutine());
    }

    private IEnumerator HandleDashCoroutine()
    {
        float dashCurrentTime = 0;
        float dashCurrentSpeed = _dashMaxSpeed;

        while (dashCurrentTime < _dashLeadTime)
        {
            _characterController.Move(dashCurrentSpeed * (Time.deltaTime / _dashLeadTime) * _transform.forward);

            dashCurrentTime += Time.deltaTime;

            yield return null;
        }
    }
}
