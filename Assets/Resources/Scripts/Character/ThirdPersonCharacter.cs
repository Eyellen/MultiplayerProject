using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine.Core
{
    public class ThirdPersonCharacter : CharacterBase
    {
        [Header("Third Person Character Settings")]
        [SerializeField]
        private Transform _cameraTransform;

        [SerializeField]
        private float _turnSmoothness = 15f;

        protected override void Start()
        {
            base.Start();
        }

        protected override InputMsg ProcessInput()
        {
            InputMsg inputMsg = base.ProcessInput();
            inputMsg.relativeToAngle = _cameraTransform.eulerAngles.y;

            return inputMsg;
        }

        protected override void HandleWalkState(InputMsg inputMsg)
        {
            // Finding input angle
            float targetAngle = Mathf.Atan2(inputMsg.movementInput.x, inputMsg.movementInput.z) *
                Mathf.Rad2Deg + inputMsg.relativeToAngle;

            // Smoothly rotation character
            _transform.rotation = Quaternion.Lerp(_transform.rotation,
                Quaternion.Euler(0, targetAngle, 0), _turnSmoothness * _minTimeBetweenTicks);

            // Rotating movement direction
            Vector3 moveDirection = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;
            Move(_movementSpeed * _minTimeBetweenTicks * moveDirection);
        }
    }
}
