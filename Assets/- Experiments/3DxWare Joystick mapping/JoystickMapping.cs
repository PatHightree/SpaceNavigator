using UnityEngine;

public class JoystickMapping : MonoBehaviour {
	public Vector3 InitialInput;
	public Vector3 JoystickInput;
	void Awake() {
		InitialInput = new Vector3(Input.GetAxis("Joy 0 X"), Input.GetAxis("Joy 0 Y"), Input.GetAxis("Joy 0 Z"));
	}

	void Start() {
	}

	void Update() {
		JoystickInput = new Vector3(Input.GetAxis("Joy 0 X"), Input.GetAxis("Joy 0 Y"), Input.GetAxis("Joy 0 Z"));
		//JoystickInput -= InitialInput;
		//Debug.Log(JoystickInput);
		JoystickInput.x = 0;
		JoystickInput.z = 0;
		transform.Translate(JoystickInput, Space.Self);
	


		//int i = 0;
		//while (i < 4) {
		//    if (Mathf.Abs(Input.GetAxis("Joy " + i + " X")) > 0.2F || Mathf.Abs(Input.GetAxis("Joy " + i + " Y")) > 0.2F)
		//        Debug.Log(Input.GetJoystickNames()[i] + " is moved");
		//    i++;
		//}
	}
}
