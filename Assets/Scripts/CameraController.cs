using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {

	// Passed in through Unity, controll the movement of Camere with the arrow keys
	public float MIN_X;
	public float MIN_Y;
	public float MAX_X;
	public float MAX_Y;
	public float MOVE_SPEED;
	
	// Update is called once per frame, used to move the camera with the controlls
	void Update () {
		if (Input.GetKey(KeyCode.RightArrow)) {
			if (transform.position.x + MOVE_SPEED * Time.deltaTime < MAX_X) {
				transform.Translate(new Vector3(MOVE_SPEED * Time.deltaTime, 0, 0));
			}
		}
		if (Input.GetKey(KeyCode.LeftArrow)) {
			if (transform.position.x - MOVE_SPEED * Time.deltaTime > MIN_X) {
				transform.Translate(new Vector3(-MOVE_SPEED * Time.deltaTime, 0, 0));
			}
		}
		if (Input.GetKey(KeyCode.UpArrow)) {
			if (transform.position.y + MOVE_SPEED * Time.deltaTime < MAX_Y) {
				transform.Translate(new Vector3(0, MOVE_SPEED * Time.deltaTime, 0));
			}
		}
		if (Input.GetKey(KeyCode.DownArrow)) {
			if (transform.position.y + MOVE_SPEED * Time.deltaTime > MIN_Y) {
				transform.Translate(new Vector3(0, -MOVE_SPEED * Time.deltaTime, 0));
			}
		}
	}
}
