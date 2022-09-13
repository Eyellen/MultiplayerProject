using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine.Core
{
    public class ThirdPersonCharacter : CharacterBase
    {
        private Transform _cameraTransform;

        [Header("Third Person Character Settings")]
        [SerializeField]
        private float _turnSmoothness = 15f;

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

            _transform.rotation = Quaternion.Lerp(_transform.rotation,
                Quaternion.Euler(0, targetAngle, 0), _turnSmoothness * _minTimeBetweenTicks);

            // Rotating movement direction
            Vector3 moveDirection = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;
            SetHorizontalVelocity(_movementSpeed * moveDirection);
        }
    }
}
