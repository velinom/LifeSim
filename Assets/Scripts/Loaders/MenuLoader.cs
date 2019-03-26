using UnityEngine;

// Class to load the game manager singleton as soon as the game starts
public class MenuLoader : MonoBehaviour {

	// GameManare object to load, passed through Unity
	public GameObject GAME_MANAGER;

	// Load the singleton instance of the GameManager that will be used
	// throughout the game. 
	void Awake () {
		if (GameManager.instance == null) {
			Instantiate(GAME_MANAGER);
		}
	}
}