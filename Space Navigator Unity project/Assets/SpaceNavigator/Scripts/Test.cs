using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class Test : MonoBehaviour {

	[DllImport ("XCodePlugin")]
	private static extern void InitPlugin(bool isDevice);

	// Use this for initialization
	void Start () {
		InitPlugin (false);
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
