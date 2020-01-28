using UnityEngine;

namespace SpaceNavigatorDriver {

	public static class QuaternionExtensions {
		public static Quaternion Inverse(this Quaternion q) {
			return Quaternion.Inverse(q);
		}
		public static Quaternion QuaternionFromMatrix(Matrix4x4 m) {
			return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
		}
		public static Quaternion RotateInSpecifiedCoordinateSystem(Quaternion rotation, Transform referenceCoordSys) {
			return referenceCoordSys.rotation * rotation * referenceCoordSys.rotation.Inverse();
		}

		// Math by Minahito: http://sunday-lab.blogspot.nl/2008/04/get-pitch-yaw-roll-from-quaternion.html
		/// <summary>
		/// The pitch angle of the quaternion in radians.
		/// </summary>
		/// <param name="q">The quaternion.</param>
		/// <returns></returns>
		public static float Pitch(this Quaternion q) {
			return Mathf.Atan2(2 * (q.y * q.z + q.w * q.x), q.w * q.w - q.x * q.x - q.y * q.y + q.z * q.z);
		}
		/// <summary>
		/// The yaw angle of the quaternion in radians.
		/// </summary>
		/// <param name="q">The quaternion.</param>
		/// <returns></returns>
		public static float Yaw(this Quaternion q) {
			return Mathf.Asin(-2 * (q.x * q.z - q.w * q.y));
		}
		/// <summary>
		/// The roll angle of the quaternion in radians.
		/// </summary>
		/// <param name="q">The quaternion.</param>
		/// <returns></returns>
		public static float Roll(this Quaternion q) {
			return Mathf.Atan2(2 * (q.x * q.y + q.w * q.z), q.w * q.w + q.x * q.x - q.y * q.y - q.z * q.z);
		}
	}
}