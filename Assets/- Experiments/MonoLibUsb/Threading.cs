using System.Threading;
using UnityEngine;

public class Threading : MonoBehaviour {

	private Thread _workerThread;
	private Vector3 _pos;
	private object _posLock;

	public void Awake() {
		_pos = transform.position;
		_posLock = new object();

		_workerThread = new Thread(Increment);
		_workerThread.Start();
	}

	public void Update() {
		lock (_posLock) {
			transform.position = _pos;
		}
	}

	public void OnDisable() {
		_workerThread.Abort();
	}

	void Increment() {
		while (true) {
			lock (_posLock) {
				_pos = _pos + Vector3.one;
				Debug.Log(_pos);
			}

			Thread.Sleep(1000);
		}
	}
}
