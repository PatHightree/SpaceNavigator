using System;
using UnityEngine;

namespace UnityEngine.InputSystem.LowLevel
{
    public class SpaceNavigatorTester : MonoBehaviour
    {
        public bool HorizonLock = true;
        
        private void Update()
        {
            Navigate();
        }

        private void Navigate()
        {
            if (SpaceNavigatorHID.current == null) return;

            transform.Translate(SpaceNavigatorHID.current.Translation.ReadValue(), Space.Self);
            if (HorizonLock)
            {
                transform.Rotate(Vector3.up, SpaceNavigatorHID.current.Rotation.ReadValue().y, Space.World);
                transform.Rotate(Vector3.right, SpaceNavigatorHID.current.Rotation.ReadValue().x, Space.Self);
            }
            else
            {
                transform.Rotate(SpaceNavigatorHID.current.Rotation.ReadValue(), Space.Self);
            }
        }
    }
}