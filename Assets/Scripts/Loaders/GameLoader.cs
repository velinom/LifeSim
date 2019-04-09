using UnityEngine;

public class GameLoader : MonoBehaviour {

	public GameObject GAME_MANAGER;

	// Just in case it's not instantiated yet, load it.
	// Then call the setup board function.
	void Awake () {
		if (GameManager.instance == null) {
			Instantiate(GAME_MANAGER);
		}

		GameManager.instance.initGame();
	}
}
