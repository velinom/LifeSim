using Models;
using UnityEngine;

public class SheepController : MonoBehaviour {

  // reference to the rigidbody component on this sheep
  // will be set in the start function
  private Rigidbody2D rBody2D;

  // The force that will be applied to this sheep each frame.
  // This force can come from several different sources and 
  // accounts for steering behaviors.
  private Vector2 force;

  // Reference to the game manager, contains map and smell info
  public GameManager GAME_MANAGER;


  // Setup this sheep by initializing fields
  void Start() {
    this.rBody2D = GetComponent<Rigidbody2D>();
  }
	
	// Update is called once per frame, used to move the camera with the controlls
	void Update () {
    calculateSteering();
    applySteering();
  }

  // Determine the force that should be applied to move the sheep on this 
  // frame
  private void calculateSteering() {
    
    // follow the smell
    this.force = getDirectionOf(SmellType.GroundFood);
  }

  // Assess the smell of the given type in the 3x3 area surounding the agent,
  // Determine the direction that the smell is coming from and return a 2D 
  // vector in that direction
  private Vector2 getDirectionOf(SmellType type) {
    Vector2 currentCell = getCurrentCell();
    Vector2 smellDirection = new Vector2(0, 0);

    // Loop over all the cells adjacent to the sheep and determine
    // the direction that the smell is strongest
    for (int xOffset = -1; xOffset < 2; xOffset++) {
      for (int yOffset = -1; yOffset < 2; yOffset++) {
        int curX = (int)currentCell.x + xOffset;
        int curY = (int)currentCell.y + yOffset;
        if (curX >= 0 && curX <= GameManager.SIZE && curY >= 0 && curY <= GameManager.SIZE) {
          double curSmellVal = GAME_MANAGER.getSmellArray()[curX, curY].getSmell(type);
          Vector2 curSmellCell = new Vector2(curX, curY);
          Vector2 curDirection = curSmellCell - currentCell;
          smellDirection = smellDirection + (curDirection * (float)curSmellVal);
        }
      }
    }
    
    return smellDirection;
  }

  // Uses the transfrom of this GameObject to determine what cell the sheep is 
  // currently in.
  private Vector2 getCurrentCell() {
    int xCell = (int)Mathf.Round(this.transform.position.x / GameManager.CELL_SIZE);
    int yCell = (int)Mathf.Round(this.transform.position.y / GameManager.CELL_SIZE);

    return new Vector2(xCell, yCell);
  }

  // Move the sheep once per frame at the force set by the stering behavior
  private void applySteering() {
    this.rBody2D.AddForce(this.force);
  }
}
