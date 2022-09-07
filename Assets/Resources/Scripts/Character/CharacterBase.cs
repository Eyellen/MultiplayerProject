using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterBase : MonoBehaviour
{
    private Transform _transform;
    private CharacterController _characterController;

    [Header("Movement Settings")]
    [SerializeField]
    private float _movementSpeed = 5f;

    [Header("Dash Settings")]
    [SerializeField]
    private float _dashMaxSpeed = 10f;

    [SerializeField]
    private float _dashLeadTime = 1f;

    [Header("Gravity Settings")]
    private float _gravity = 9.8f;
    private float _currentVerticalSpeed;

    private void Start()
    {
        _transform = GetComponent<Transform>();
        _characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        HandleGravity();
        HandleMovement();
        HandleDash();
    }

    private void HandleGravity()
    {
        if (_characterController.isGrounded)
        {
            _currentVerticalSpeed = 0;
            return;
        }

        _currentVerticalSpeed -= _gravity * Time.deltaTime;
        _characterController.Move(new Vector3(0, _currentVerticalSpeed, 0) * Time.deltaTime);
    }

    private void HandleMovement()
    {
        if (PlayerInput.MovementVector.magnitude >= 0.1)
            _characterController.Move(_movementSpeed * Time.deltaTime * (_transform.rotation * PlayerInput.MovementVector));
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
