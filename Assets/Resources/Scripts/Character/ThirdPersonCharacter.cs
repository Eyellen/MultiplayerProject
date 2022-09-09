using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine.Core
{
    public class ThirdPersonCharacter : CharacterBase
    {
        private Transform _cameraTransform;

        protected override void Start()
        {
            base.Start();

            _cameraTransform = Camera.main.transform;
        }

        protected override void HandleHorizontalMovement(Vector3 axis)
        {
            if (axis.magnitude < 0.1) return;

            // Rotating character
            float targetAngle = Mathf.Atan2(axis.x, axis.z) *
                Mathf.Rad2Deg + _cameraTransform.eulerAngles.y;
            _transform.rotation = Quaternion.Euler(0, targetAngle, 0);

            // Rotating movement direction
            Vector3 moveDirection = _transform.rotation * Vector3.forward;
            SetHorizontalVelocity(_movementSpeed * moveDirection);
        }
    }
}
