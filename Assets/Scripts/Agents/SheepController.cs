using Models;
using UnityEngine;

public class SheepController : MonoBehaviour {

  // Reference to the game manager, contains map and smell info
  // as well as all constants needed from the manager
  public GameManager GAME_MANAGER;
  private float CELL_SIZE = GameManager.CELL_SIZE;

  // The max speed / accel (Force) for this sheep
  private float MAX_SPEED = 2;
  private float MAX_ACCEL = 10;
  private float MAX_ROTATION = 179;
  private float MAX_ANGULAR_ACC = 30;

  // The radii for the arrive-at behavior
  private float ARRIVE_RADIUS = 0.5f;
  private float SLOW_RADIUS = 3f;
  private float ROTATE_ARRIVE_RAD = 15;
  private float ROTATE_SLOW_RAD = 45;

  // Parameters for wall avoidence
  private float RAY_LENGTH = 2;

  // The force that will be applied to this sheep each frame.
  // This force can come from several different sources and 
  // accounts for steering behaviors.
  private Vector2 steering;
  private float angularSteering;

  // Updated at the start of each frame, the current cell the 
  // sheep is in.
  private Vector2 currentCell;

  // The current velocity and rotation of the sheep
  private Vector2 velocity;
  private float rotation;

  // Insistance
  

  // Setup this sheep by initializing fields
  void Start() {
    velocity = new Vector2(0, 0);
    rotation = 0;
  }
	
	// Update is called once per frame used to calculate the steering 
  // for the given frame
	void Update () {
    // Update the current cell so that it is known the whole update
    this.currentCell = getCurrentCell();

    // Calculate the steering, this includes the high level goal steering as
    // well as lower level steering to avoid trees / rocks etc.
    calculateSteering();

    // Calculate the rotation which is always towards the sheeps current 
    // velocity
    calculateRotation();

    // Apply the steering to actually move the sheep, both linear and 
    // rotational steering are applied here.
    applySteering();
  }

  // Determine the force that should be applied to move the sheep on this 
  // frame
  private void calculateSteering() {
    // The steering toward the sheeps current goal, to be set below.
    Vector2 mainGoalSteering = new Vector2(-1, -1);

    // If within 3 blocks of smell, arrive at smell
    Vector2 closeBush = getCloseFood(BoardManager.Food.Bush, 3);
    if (closeBush.x >= 0 && closeBush.y >= 0) {
      mainGoalSteering = arriveAt(
        new Vector2(closeBush.x * CELL_SIZE, closeBush.y * CELL_SIZE));
    } else {
      // Otherwise, follow the smell until within 3 blocks
      mainGoalSteering = getDirectionOf(SmellType.GroundFood) * MAX_ACCEL;
    }

    // Now that main steering has been calculated, get lower-level steerings 
    
    // Wall Avoidence:
    // Cast a ray in front of the sheep to determine if there is a wall there
    Vector2 avoidWallsSteering = calculateWallAvoidence();


    // The total steering is a weighted sum of the components
    this.steering = mainGoalSteering * 0.3f + avoidWallsSteering * 0.7f;
  }

  // Calculate rotation, Rotatoin is always in the direction of the 
  // current velocity.
  private void calculateRotation() {
    float targetOrientation = Mathf.Rad2Deg * Mathf.Atan2(velocity.y, velocity.x);
    
    // The target rotation depends on the radii for "arive" and "slow"
    float curOrientation = transform.eulerAngles.z;
    if (curOrientation > 180) curOrientation -= 360;
    float targetRotation = targetOrientation - curOrientation;
    if (Mathf.Abs(targetRotation) < ROTATE_ARRIVE_RAD) {
      targetRotation = 0;
    } else if (Mathf.Abs(targetRotation) < SLOW_RADIUS) {
      targetRotation = MAX_ROTATION * targetRotation / ROTATE_ARRIVE_RAD;
    } else {
      targetRotation = targetRotation > 0 ? MAX_ROTATION : -MAX_ROTATION;
    }

    // Clamp target rotation for better behavior
    if (targetRotation > 180) targetRotation -= 180;
    if (targetRotation < -180) targetRotation += 360;

    // If the sheep is stopped, make it stop rotating
    if (velocity.magnitude < 0.05) {
      targetRotation = -this.rotation;
    }
    float angSteering = targetRotation - this.rotation;
    if (angSteering > 180) angSteering -= 360;
    if (angSteering < -180) angSteering += 360;
    this.angularSteering = angSteering;
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
        if (curX >= 0 && curX < GameManager.SIZE && curY >= 0 && curY < GameManager.SIZE) {
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
  private Vector2 getCloseFood(BoardManager.Food type, int distance) {
    for (int xOffset = -distance; xOffset < distance + 1; xOffset++) {
      for (int yOffset = -distance; yOffset < distance + 1; yOffset++) {
        int curX = (int)currentCell.x + xOffset;
        int curY = (int)currentCell.y + yOffset;
        if (curX >= 0 && curX < GameManager.SIZE && curY >= 0 && curY < GameManager.SIZE) {
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

  // Return a steering vector to awoid any walls. Need to avoi High elevation and water.
  // Done by using short "whisker" ray-casts and moving to a spot normal to the wall
  // if the whiskers hit something.
  private Vector2 calculateWallAvoidence() {
    // Preform the whisker ray-cast
    RaycastHit2D hit = Physics2D.Raycast(
      this.transform.position, this.transform.right, RAY_LENGTH);

    Debug.DrawRay(this.transform.position, this.transform.right, Color.black);

    // If we hit a wall with a whisker
    if (hit.collider != null) {
      if (hit.transform.tag == "Water" || hit.transform.tag == "HighElevation") {
        // Get a point normal to the wall at the point the colider hit.
        Vector2 normal = hit.normal.normalized;
        Debug.DrawRay(hit.point, hit.normal, Color.black);
        Vector2 seekPoint = hit.point + hit.normal * 1.5f;

        

        return arriveAt(seekPoint);
      }
    }

    return new Vector2(0, 0);
  }

  // Uses the transfrom of this GameObject to determine what cell the sheep is 
  // currently in.
  private Vector2 getCurrentCell() {
    int xCell = (int)Mathf.Round(this.transform.position.x / CELL_SIZE);
    int yCell = (int)Mathf.Round(this.transform.position.y / CELL_SIZE);

    return new Vector2(xCell, yCell);
  }

  // Preform an update on the sheep based on the linear acceleration and rotation.
  // Then move the sheep based on the new velocity and orientatoin.
  private void applySteering() {
    // Begin by clamping the linear / angular acceleration
    if (steering.magnitude > MAX_ACCEL) {
      this.steering.Normalize();
      this.steering *= MAX_ACCEL;
    }
    if (Mathf.Abs(angularSteering) > MAX_ANGULAR_ACC) {
      angularSteering = angularSteering > 0 ? MAX_ANGULAR_ACC : -MAX_ANGULAR_ACC;
    }

    // Update the velocities using the accelerations
    this.velocity += this.steering;
    this.rotation += this.angularSteering;

    // Clip the velocity/rotation if they are too high
    if (this.velocity.magnitude > MAX_SPEED) {
      this.velocity.Normalize();
      this.velocity *= MAX_SPEED;
    }
    if (Mathf.Abs(this.rotation) > MAX_ROTATION) {
      this.rotation = this.rotation > 0 ? MAX_ROTATION : - MAX_ROTATION;
    }
    if (this.rotation > 180) this.rotation -= 360;
    if (this.rotation < -180) this.rotation += 360;

    // Now apply the steering to the sheep
    this.transform.Translate(this.velocity * Time.deltaTime, Space.World);
    this.transform.Rotate(0, 0, this.rotation * Time.deltaTime);
  }
}
