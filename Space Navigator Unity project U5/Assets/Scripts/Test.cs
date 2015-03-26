using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class Test : MonoBehaviour {
	
	[DllImport ("XCodePlugin")]
	private static extern int InitDevice();
	[DllImport ("XCodePlugin")]
	private static extern void SampleDevice(ref int x, ref int y, ref int z, ref int rx, ref int ry, ref int rz);
	[DllImport ("XCodePlugin")]
	private static extern int DisposeDevice();

	public Vector3 Trans;
	public Vector3 Rot;

	// Use this for initialization
	void Start () {
		Debug.Log (InitDevice ());
	}
	
	// Update is called once per frame
	void Update () {
		int x, y, z, rx, ry, rz;
		x = y = z = rx = ry = rz = 0;
		SampleDevice (ref x, ref y, ref z, ref rx, ref ry, ref rz);
		Trans = new Vector3 (x, y, z);
		Rot = new Vector3 (rx, ry, rz);
	}

	void OnDisable() {
		Debug.Log (DisposeDevice ());
	}
}
