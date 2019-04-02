using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {

	// Passed in through Unity, controll the movement of Camere with the arrow keys
	public float MIN_X;
	public float MIN_Y;
	public float MAX_X;
	public float MAX_Y;
	public float MOVE_SPEED;

	// Passed in through Unity, controll the zoom of the camera
	public float MAX_SIZE;
	public float MIN_SIZE;
	public float ZOOM_SPEED;
	
	// Update is called once per frame, used to move the camera with the controlls
	void Update () {
		float scaledSpeed = MOVE_SPEED * Camera.main.orthographicSize;
		// Handle moving the camera up/down/left/right
		if (Input.GetKey(KeyCode.RightArrow)) {
			if (transform.position.x + scaledSpeed * Time.deltaTime < MAX_X) {
				transform.Translate(new Vector3(scaledSpeed * Time.deltaTime, 0, 0));
			} else {
				transform.Translate(new Vector3(MAX_X - transform.position.x, 0, 0));
			}
		}
		if (Input.GetKey(KeyCode.LeftArrow)) {
			if (transform.position.x - scaledSpeed * Time.deltaTime > MIN_X) {
				transform.Translate(new Vector3(-scaledSpeed * Time.deltaTime, 0, 0));
			} else {
				transform.Translate(new Vector3(MIN_X - transform.position.x, 0, 0));
			}
		}
		if (Input.GetKey(KeyCode.UpArrow)) {
			if (transform.position.y + scaledSpeed * Time.deltaTime < MAX_Y) {
				transform.Translate(new Vector3(0, scaledSpeed * Time.deltaTime, 0));
			} else {
				transform.Translate(new Vector3(0, MAX_Y - transform.position.y, 0));
			}
		}
		if (Input.GetKey(KeyCode.DownArrow)) {
			if (transform.position.y - scaledSpeed * Time.deltaTime > MIN_Y) {
				transform.Translate(new Vector3(0, -scaledSpeed * Time.deltaTime, 0));
			} else {
				transform.Translate(new Vector3(0, MIN_Y - transform.position.y, 0));
			}
		}

		// Controll the zoom of the camera with the u and d keys
		if (Input.GetKey(KeyCode.U)) {
			Camera.main.orthographicSize += ZOOM_SPEED * Time.deltaTime;
		}
		if (Input.GetKey(KeyCode.D)) {
			Camera.main.orthographicSize -= ZOOM_SPEED * Time.deltaTime;
		}
		Camera.main.orthographicSize += Input.GetAxis("Mouse ScrollWheel") * ZOOM_SPEED;
		Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, MIN_SIZE, MAX_SIZE);

		// Check if the game has been quit
		if (Input.GetKey(KeyCode.Escape)) {
			Application.Quit();
		}
	}
}
