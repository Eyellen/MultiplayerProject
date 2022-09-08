using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonCharacter : CharacterBase
{
    private Transform _cameraTransform;

    protected override void Start()
    {
        base.Start();

        _cameraTransform = Camera.main.transform;
    }

    protected override void HandleMovement()
    {
        _velocity = Vector3.zero;

        HandleGravity();

        if (PlayerInput.MovementVector.magnitude >= 0.1)
        {
            float targetAngle = Mathf.Atan2(PlayerInput.MovementVector.x, PlayerInput.MovementVector.z) *
                Mathf.Rad2Deg + _cameraTransform.eulerAngles.y;

            _transform.rotation = Quaternion.Euler(0, targetAngle, 0);

            Vector3 moveDirection = _transform.rotation * Vector3.forward;
            _velocity += _movementSpeed * moveDirection;
        }

        _characterController.Move(_velocity * Time.deltaTime);
    }
}
