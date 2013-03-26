using System;
using UnityEngine;

class SpaceNavigatorNoDevice : SpaceNavigator {
	// For development without SpaceNavigator.
	private Vector3 _fakeRotationInput;
	private Vector3 _fakeTranslationInput;
	private const float FakeInputThreshold = 0.1f;

	// Public API
	public override Vector3 GetTranslation() {
		float sensitivity = Application.isPlaying ? PlayTransSens : TransSens;
		return new Vector3(
			LockTranslationX || LockTranslationAll ? 0 : _fakeTranslationInput.x * sensitivity,
			LockTranslationY || LockTranslationAll ? 0 : _fakeTranslationInput.y * sensitivity,
			LockTranslationZ || LockTranslationAll ? 0 : _fakeTranslationInput.z * sensitivity);
	}
	public override Quaternion GetRotation() {
		float sensitivity = Application.isPlaying ? PlayRotSens : RotSens;
		return Quaternion.Euler(
			LockRotationX || LockRotationAll ? 0 : _fakeRotationInput.x * sensitivity,
			LockRotationY || LockRotationAll ? 0 : _fakeRotationInput.y * sensitivity,
			LockRotationZ || LockRotationAll ? 0 : _fakeRotationInput.z * sensitivity);
	}

	#region - Singleton -
	public static SpaceNavigatorNoDevice SubInstance {
		get { return _subInstance ?? (_subInstance = new SpaceNavigatorNoDevice()); }
	}
	private static SpaceNavigatorNoDevice _subInstance;
	#endregion - Singleton -

	#region - IDisposable -
	public override void Dispose() { }
	#endregion - IDisposable -

	public override void OnGUI() {
		base.OnGUI();

		GUILayout.Space(10);
		GUILayout.BeginHorizontal();
		GUILayout.Label(String.Format("Fake rotation x {0:0.00000}", _fakeRotationInput.x));
		_fakeRotationInput.x = GUILayout.HorizontalSlider(_fakeRotationInput.x, -1, 1);
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label(String.Format("Fake rotation y {0:0.00000}", _fakeRotationInput.y));
		_fakeRotationInput.y = GUILayout.HorizontalSlider(_fakeRotationInput.y, -1, 1);
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label(String.Format("Fake rotation z {0:0.00000}", _fakeRotationInput.z));
		_fakeRotationInput.z = GUILayout.HorizontalSlider(_fakeRotationInput.z, -1, 1);
		GUILayout.EndHorizontal();

		GUILayout.Space(5);

		GUILayout.BeginHorizontal();
		GUILayout.Label(String.Format("Fake translation x {0:0.00000}", _fakeTranslationInput.x));
		_fakeTranslationInput.x = GUILayout.HorizontalSlider(_fakeTranslationInput.x, -0.05f, 0.05f);
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label(String.Format("Fake translation y {0:0.00000}", _fakeTranslationInput.y));
		_fakeTranslationInput.y = GUILayout.HorizontalSlider(_fakeTranslationInput.y, -0.05f, 0.05f);
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		GUILayout.Label(String.Format("Fake translation z {0:0.00000}", _fakeTranslationInput.z));
		_fakeTranslationInput.z = GUILayout.HorizontalSlider(_fakeTranslationInput.z, -0.05f, 0.05f);
		GUILayout.EndHorizontal();

		if (GUILayout.Button("Stop")) {
			_fakeRotationInput = Vector2.zero;
			_fakeTranslationInput = Vector3.zero;
		}

		if (Mathf.Abs(_fakeRotationInput.x) < FakeInputThreshold)
			_fakeRotationInput.x = 0;
		if (Mathf.Abs(_fakeRotationInput.y) < FakeInputThreshold)
			_fakeRotationInput.y = 0;
	}
}