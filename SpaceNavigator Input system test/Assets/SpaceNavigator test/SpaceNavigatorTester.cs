using System;
using UnityEngine;

namespace UnityEngine.InputSystem.LowLevel
{
    public class SpaceNavigatorTester : MonoBehaviour
    {
        private void Update()
        {
            transform.Translate(SpaceNavigatorHID.current.Translation.ReadValue(), Space.Self);
            transform.Rotate(transform.right, SpaceNavigatorHID.current.Rotation.ReadValue().x, Space.Self);
            transform.Rotate(Vector3.up, SpaceNavigatorHID.current.Rotation.ReadValue().y, Space.World);
            transform.Rotate(transform.forward, SpaceNavigatorHID.current.Rotation.ReadValue().z, Space.Self);
        }
    }
}