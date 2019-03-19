using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BoardManager : MonoBehaviour {

	// Size of the game board, set to public so they can be modified in Unity
	public int SIZE = 50;

	// In hundreds of px for use with unity placing.
	public float CELL_SIZE = 1.2f;

	public double MID_ELEVATION_PERCENT = 0.4;
	public double MID_EXPANSION_CHANCE = 0.5;

	// Floor tiles that are passed in through Unity
	public GameObject[] LOW_ELEVATION_TILES;
	public GameObject[] MID_ELEVATION_TILES;
	public GameObject[] HIGH_ELEVATION_TILES;

	// The three types of elevation tiles
	private enum ElevationType { Low, Medium, High }

	// Holds all the objects spawned into the board
	private Transform boardHolder;

	// 2D array of tyle-types that gets randomly generated
	private ElevationType[, ] boardArray;

  // Initializes the boardArray using random methods to set the elevation
	// type at each position on the board.
	private void setupBoard() {
		boardArray = new ElevationType[SIZE, SIZE];

		// Begin by making every tile Low elevation
		for (int x = 0; x < SIZE; x++) {
			for (int y = 0; y < SIZE; y++) {
				boardArray[x, y] = ElevationType.Low;
			}
		}

		// Now place the middle tiles, start by calculating how many to place and
		// adding three seed tiles to the queue
		int tilesToPlace = (int)(SIZE * SIZE * MID_ELEVATION_PERCENT);
		Debug.Log(tilesToPlace);
		int placedTiles = 0;
		Vector2 root1 = new Vector2(Random.Range(0, SIZE), Random.Range(0, SIZE));
		Vector2 root2 = new Vector2(Random.Range(0, SIZE), Random.Range(0, SIZE));
		Vector2 root3 = new Vector2(Random.Range(0, SIZE), Random.Range(0, SIZE));
		Queue<Vector2> openList = new Queue<Vector2>();
		openList.Enqueue(root1);
		openList.Enqueue(root2);
		openList.Enqueue(root3);

		// In a loop, place tiles until queue is empty or we have reached tilesToPlace
		while (placedTiles < tilesToPlace && openList.Count > 0) {
			// Pop off the queue and add make the tile med-elevation
			Vector2 currentTile = openList.Dequeue();
			int curX = (int)currentTile.x;
			int curY = (int)currentTile.y;
			if (boardArray[curX, curY] == ElevationType.Medium) continue;
			boardArray[curX, curY] = ElevationType.Medium;
			placedTiles++;

			// Add neighbors to Queue if they aren't mid with a percent chance
			// Adding the down neighbor
			if (curY > 0 && boardArray[curX, curY - 1] != ElevationType.Medium) {
				if (Random.value < MID_EXPANSION_CHANCE) {
					openList.Enqueue(new Vector2(curX, curY - 1));
				}
			}
			// Adding the left neighbor
			if (curX > 0 && boardArray[curX - 1, curY] != ElevationType.Medium) {
				if (Random.value < MID_EXPANSION_CHANCE) {
					openList.Enqueue(new Vector2(curX - 1, curY));
				}
			}
			// Adding the top neighbor
			if (curY < SIZE - 1 && boardArray[curX, curY + 1] != ElevationType.Medium) {
				if (Random.value < MID_EXPANSION_CHANCE) {
					openList.Enqueue(new Vector2(curX, curY + 1));
				}
			}
			// Adding the right neighbor
			if (curX < SIZE - 1 && boardArray[curX + 1, curY] != ElevationType.Medium) {
				if (Random.value < MID_EXPANSION_CHANCE) {
					openList.Enqueue(new Vector2(curX + 1, curY));
				}
			}
		}
		Debug.Log("Placed: " + placedTiles);
	}
	
  // Uses the randomly generaed boardArray to place random tiles of the correct type
	// on the board by instantiating it and putting it in the right place.
	private void createBoard() {
		boardHolder = new GameObject("Board").transform;

		for (int x = 0; x < SIZE; x++) {
			for (int y = 0; y < SIZE; y++) {
				GameObject toInstantiate = null;
				ElevationType currentType = boardArray[x, y];

				switch(currentType) {
					case ElevationType.Low: {
            toInstantiate = LOW_ELEVATION_TILES[Random.Range(0, LOW_ELEVATION_TILES.Length)];
						break;
					}
					case ElevationType.Medium: {
						toInstantiate = MID_ELEVATION_TILES[Random.Range(0, MID_ELEVATION_TILES.Length)];
						break;
					}
					case ElevationType.High: {
						toInstantiate = HIGH_ELEVATION_TILES[Random.Range(0, HIGH_ELEVATION_TILES.Length)];
						break;
					}
					default: break;
				}

				GameObject instance = Instantiate(
					toInstantiate, new Vector3(x * CELL_SIZE, y * CELL_SIZE, 0f), Quaternion.identity) as GameObject;
				instance.transform.SetParent(boardHolder);
			}
		}
	}

	// To be called by the game manager, randomly makes the board
	// and instantiates the tiles in their place
	public void createScene() {
		setupBoard();
		createBoard();
	}
}
