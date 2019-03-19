using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BoardManager : MonoBehaviour {

	// Size of the game board, set to public so they can be modified in Unity
	public int SIZE = 50;

	// Floor tiles that are passed in through Unity
	public GameObject[] LOW_ELEVATION_TILES;
	public GameObject[] MID_ELEVATION_TILES;
	public GameObject[] HIGH_ELEVATION_TILES;

	// Holds all the objects spawned into the board
	private Transform boardHolder;

	// 2D array of tyle-types that gets randomly generated

  // Instantiates the tiles by picking a random one from the three types of
	// tile lists. Tiles are all spawned at the desired location
	private void createBoard() {
		boardHolder = new GameObject("Board").transform;

		for (int x = 0; x < SIZE; x++) {
			for (int y = 0; y < SIZE; y++) {
				GameObject toInstantiate = LOW_ELEVATION_TILES[Random.Range(0, LOW_ELEVATION_TILES.Length)];
				GameObject instance = Instantiate(
					toInstantiate, new Vector3(x, y, 0f), Quaternion.identity) as GameObject;
				instance.transform.SetParent(boardHolder);
			}
		}
	}

	public void createScene() {
		createBoard();
	}
}
