using System;
using UnityEngine;

namespace SpaceNavigatorDriver {

	[Serializable]
	public class Locks {
		public bool X, Y, Z, All;

		[SerializeField]
		private string _name;

		// Constructor
		public Locks(string name) {
			_name = name;
		}

		// Methods
		public void Write() {
			string prefix = _name + " lock ";
			PlayerPrefs.SetInt(prefix + "X", X ? 1 : 0);
			PlayerPrefs.SetInt(prefix + "Y", Y ? 1 : 0);
			PlayerPrefs.SetInt(prefix + "Z", Z ? 1 : 0);
			PlayerPrefs.SetInt(prefix + "All", All ? 1 : 0);
		}
		public void Read() {
			string prefix = _name + " lock ";
			X = PlayerPrefs.GetInt(prefix + "X", 0) == 1;
			Y = PlayerPrefs.GetInt(prefix + "Y", 0) == 1;
			Z = PlayerPrefs.GetInt(prefix + "Z", 0) == 1;
			All = PlayerPrefs.GetInt(prefix + "All", 0) == 1;
		}
	}
}