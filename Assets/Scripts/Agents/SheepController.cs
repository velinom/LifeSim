using Models;
using UnityEngine;

public class SheepController : MonoBehaviour {

  // Reference to the game manager, contains map and smell info
  // as well as all constants needed from the manager
  public GameManager GAME_MANAGER;
  private float CELL_SIZE = GameManager.CELL_SIZE;

  // The max speed / accel (Force) for this sheep
  private float MAX_SPEED = 5;
  private float MAX_ACCEL = 10;

  // The radii for the arrive-at behavior
  private float ARRIVE_RADIUS = 0.2f;
  private float SLOW_RADIUS = 3f;

  // The force that will be applied to this sheep each frame.
  // This force can come from several different sources and 
  // accounts for steering behaviors.
  private Vector2 steering;

  // Updated at the start of each frame, the current cell the 
  // sheep is in.
  private Vector2 currentCell;

  // The current velocity of the sheep
  private Vector2 velocity;

  // Setup this sheep by initializing fields
  void Start() {
    velocity = new Vector2(0, 0);
  }
	
	// Update is called once per frame used to calculate the steering 
  // for the given frame
	void Update () {
    this.currentCell = getCurrentCell();
    calculateSteering();

    applySteering();
  }


  // Determine the force that should be applied to move the sheep on this 
  // frame
  private void calculateSteering() {
    // If within 3 blocks of smell, arrive at smell
    Vector2 closeBush = getClose(BoardManager.Food.Bush, 3);
    if (closeBush.x >= 0 && closeBush.y >= 0) {
      Debug.Log("Sheep found bush at: (" + closeBush.x + ", " + closeBush.y + ")");
      this.steering = arriveAt(
        new Vector2(closeBush.x * CELL_SIZE, closeBush.y * CELL_SIZE));
    } else {
      // Otherwise, follow the smell until within 3 blocks
      this.steering = getDirectionOf(SmellType.GroundFood) * MAX_ACCEL;
    }
  }

  // Assess the smell of the given type in the 3x3 area surounding the agent,
  // Determine the direction that the smell is coming from and return a 2D 
  // vector in that direction
  private Vector2 getDirectionOf(SmellType type) {
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
    
    smellDirection.Normalize();
    return smellDirection;
  }

  // Return the location of the first object of the given type within
  // the given distance of the player. If none is found, return (-1, -1)
  private Vector2 getClose(BoardManager.Food type, int distance) {
    for (int xOffset = -distance; xOffset < distance + 1; xOffset++) {
      for (int yOffset = -distance; yOffset < distance + 1; yOffset++) {
        int curX = (int)currentCell.x + xOffset;
        int curY = (int)currentCell.y + yOffset;
        if (curX >= 0 && curX <= GameManager.SIZE && curY >= 0 && curY <= GameManager.SIZE) {
          if (GAME_MANAGER.getFoodArray()[curX, curY] == type) {
            return new Vector2(curX, curY);
          }
        }
      }
    }

    return new Vector2(-1, -1);
  }

  // Calculates the force needed to make the sheep slowly arrive at the
  // given location and gradualy come to a stop
  private Vector2 arriveAt(Vector2 targetLoc) {
    Vector2 location2d = new Vector2(transform.position.x, transform.position.y);
    float distToTarget = (targetLoc - location2d).magnitude;

    // Three distance cases from slow-radius and target-radius
    // Compute the target velocity in each case
    float targetSpeed;
    if (distToTarget > SLOW_RADIUS) {
      targetSpeed = MAX_SPEED;
    } else if (distToTarget > ARRIVE_RADIUS) {
      targetSpeed = (distToTarget / SLOW_RADIUS) * MAX_SPEED;
    } else {
      targetSpeed = 0;
    }

    // Get the target velocity including direction
    Vector2 targetVelocity = (targetLoc - location2d);
    targetVelocity.Normalize();
    targetVelocity = targetVelocity * targetSpeed;

    // The target acceleration is the difference between the current velocity
    // and the target velocity
    return targetVelocity - this.velocity;
  }

  // Uses the transfrom of this GameObject to determine what cell the sheep is 
  // currently in.
  private Vector2 getCurrentCell() {
    int xCell = (int)Mathf.Round(this.transform.position.x / CELL_SIZE);
    int yCell = (int)Mathf.Round(this.transform.position.y / CELL_SIZE);

    return new Vector2(xCell, yCell);
  }

  // Preform an update on the sheep based on the linear acceleration.
  // Then move the sheep based on the new velocity
  private void applySteering() {
    // Begin by clamping the acceleration
    if (steering.magnitude > MAX_ACCEL) {
      this.steering.Normalize();
      this.steering *= MAX_ACCEL;
    }

    // Update the velocity using the acceleration
    this.velocity = this.velocity + this.steering;

    // Clip the velocity if it is too high
    if (this.velocity.magnitude > MAX_SPEED) {
      this.velocity.Normalize();
      this.velocity *= MAX_SPEED;
    }

    // Move the sheep
    this.transform.Translate(this.velocity * Time.deltaTime);
  }
}
