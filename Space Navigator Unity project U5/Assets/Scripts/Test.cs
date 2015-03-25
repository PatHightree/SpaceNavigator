using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class Test : MonoBehaviour {
	
	[DllImport ("XCodePlugin")]
	private static extern int InitDevice();
	[DllImport ("XCodePlugin")]
	private static extern int SampleDevice();
	[DllImport ("XCodePlugin")]
	private static extern int DisposeDevice();

	public int Output;

	// Use this for initialization
	void Start () {
		Debug.Log (InitDevice ());
	}
	
	// Update is called once per frame
	void Update () {
		Output = SampleDevice ();
	}

	void OnDisable() {
		Debug.Log (DisposeDevice ());
	}
}
