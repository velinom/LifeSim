using Models;
using System.Collections.Generic;
using UnityEngine;

// Class with a bunch of utility methods that are the same / used acrossed several
// differnet agents
public abstract class BaseAgent : MonoBehaviour,
                      ISmellFollower, IWallAvoider, ICollisionAvoider, IFleer {

  // Consts from the game manager
  private float CELL_SIZE = GameManager.CELL_SIZE;

  // MOVEMENT CONSTANTS,
  // Should be passed in through unity to base classes
  public float MAX_SPEED;
  public float MAX_ACCEL;
  public float MAX_ROTATION;
  public float MAX_ANGULAR_ACC;
  // The radii for the arrive-at behavior
  public float ARRIVE_RADIUS;
  public float SLOW_RADIUS;
  public float ROTATE_ARRIVE_RAD;
  public float ROTATE_SLOW_RAD;

  // Stores the velocity and rotation of the agent (in rigidbody2D).
  protected Rigidbody2D rigidBody;

  // The cell that the agent is currently in
  protected Vector2 currentCell;

  // The action that the agent is currently taking
  // Can breifly be null while the agent "Thinks" about
  // what to do
  public Action goal;

  // The insistance object for this agent, the agent's goal is to minimize 
  // the values in this object.
  public Insistance insistance;

  // The location this agent is currently attempting to arrive at. This only
  // happens once the agent is very close to and "sees" their goal.
  // Is frequently null if the agent is wandering, sleeping, or following a smell
  public Vector2 target;

  /*
   * Composition classes for each behavior that this agent implements
   * The agent delegates these behaviors out to the composition calsses.
   * This section also includes parameters that these classes need
   */
  private ISmellFollower smellFollower;

  public float MAIN_RAY_LENGTH;
  public float  SIDE_RAY_LENGTH;
  private IWallAvoider wallAvoider;

  public float COLLISION_AVOIDANCE_RAD;
  private ICollisionAvoider collisionAvoider;

  public float FLEE_TAG_RAD;
  private IFleer fleer;

  public void initialize() {
    this.smellFollower = new SmellFollower();
    this.wallAvoider = new WallAvoider(MAIN_RAY_LENGTH, SIDE_RAY_LENGTH, transform, MAX_ACCEL);
    this.collisionAvoider = new CollisionAvoider(COLLISION_AVOIDANCE_RAD, transform, rigidBody, MAX_ACCEL);
    this.fleer = new Fleer(FLEE_TAG_RAD, transform, MAX_ACCEL);
  }

  // Uses the transfrom of this GameObject to determine what cell the sheep is 
  // currently in.
  public Vector2 getCurrentCell() {
    int xCell = (int)Mathf.Round(this.transform.position.x / CELL_SIZE);
    int yCell = (int)Mathf.Round(this.transform.position.y / CELL_SIZE);

    return new Vector2(xCell, yCell);
  }

  // Picks the best goal in the list of actions to minimize the sum of the insistances
  // squared. Accounts for the time that actions take
  public void determineGoal(List<Action> actions, Insistance insistance, string name) {
    // Loop over all available actions and determine the one that minimizes the 
    // sum of insistances sqared. Use the estimated time to complete each action
    // to discount the other actions when calculating
    float bestValue = float.MaxValue;
    Action bestAction = null;
    foreach (Action action in actions) {
      // Begin by cloning the insistance object so it isn't mutated in the calculations
      Insistance insistanceCopy = insistance.deepCopy();

      // This method also uses the estimated time to determine how much the insistances
      // will increase while this action is being carried out.
      action.takeActionAtTime(insistanceCopy, action.estTimeSeconds);
      float totalInsistance = insistanceCopy.totalInsistance(); // sum of squares
      if (totalInsistance < bestValue) {
        bestValue = totalInsistance;
        bestAction = action;
      }
    }

    Debug.Log("A " + name + " with: hunger:" + insistance.insistances[InsistanceType.Food] +
              ", thirst:" + insistance.insistances[InsistanceType.Water] +
              ", sleep:" + insistance.insistances[InsistanceType.Sleep] +
              ", joy:" + insistance.insistances[InsistanceType.Joy] +
              ", Decided to " + bestAction.name);

    this.goal = bestAction;
  }

  // ACTION METHOD: Returns the main steering vector to accomplish this action, 
  // allong with setting this.action to null and updating insistances when the 
  // action is finished
  public Vector2 seekFood(BoardManager.Food foodType, SmellType smellType) {
    Vector2 goalSteering = new Vector2(-1, -1);

    // If we've seen the food and stored its location, just arrive at the location
    // (If we don't have a target to arrive at the vector is (-1, -1))
    if (this.target.x >= 0 && this.target.y >= 0) {
      goalSteering = arriveAt(
        new Vector2(this.target.x * CELL_SIZE, this.target.y * CELL_SIZE),
        new Vector2(currentCell.x * CELL_SIZE, currentCell.y * CELL_SIZE),
        rigidBody.velocity, SLOW_RADIUS, ARRIVE_RADIUS, MAX_SPEED);
      
      // If we have arrived at the location, apply the current goal to the insistances
      if (this.target.x == this.currentCell.x && this.target.y == this.currentCell.y) {
        this.goal.apply(this.insistance);
        this.goal = null;
        this.target = new Vector2(-1, -1);
      }
    } else {
      // If we haven't seen any food yet, check if we see one
      // Check if within 3 blocks of food type (represents the agent seeing the food)
      Vector2 closeBush = getCloseFood(foodType, 3, currentCell, GameManager.instance.getFoodArray());
      if (closeBush.x >= 0 && closeBush.y >= 0) {
        this.target = closeBush;
        goalSteering = arriveAt(
          new Vector2(closeBush.x * CELL_SIZE, closeBush.y * CELL_SIZE),
          new Vector2(currentCell.x * CELL_SIZE, currentCell.y * CELL_SIZE),
          rigidBody.velocity, SLOW_RADIUS, ARRIVE_RADIUS, MAX_SPEED);
      } else {
        // If we still can't see a bush, just follow the smell
        goalSteering = this.directionOfSmell(currentCell,
          GameManager.instance.getSmellArray(), smellType) * MAX_ACCEL;
      }
    }

    return goalSteering;
  }

  // ACTION METHOD: Returns the main steering vector to accomplish this action, 
  // allong with setting this.action to null and updating insistances when the 
  // action is finished
  public Vector2 seekTile(BoardManager.TileType tileType, SmellType smellType) {
    Vector2 goalSteering = new Vector2(-1, -1);
    // If we've seen the tile and stored its location, just arrive at the tile
    // (If we don't have a target to arrive at the vector is (-1, -1))
    if (this.target.x >= 0 && this.target.y >= 0) {
      goalSteering = arriveAt(
        new Vector2(this.target.x * CELL_SIZE, this.target.y * CELL_SIZE),
        new Vector2(currentCell.x * CELL_SIZE, currentCell.y * CELL_SIZE),
        rigidBody.velocity, SLOW_RADIUS, ARRIVE_RADIUS, MAX_SPEED);
      
      // If we have arrived at the target, the agent should execute the action
      BoardManager.TileType currentCellType =
        GameManager.instance.getBoardArray()[(int)currentCell.x, (int)currentCell.y];
      if (this.target.x == this.currentCell.x && this.target.y == this.currentCell.y ||
          currentCellType == BoardManager.TileType.Water) {
        this.goal.apply(this.insistance);
        this.goal = null;
        this.target = new Vector2(-1, -1);
      }
    } else {
      // If we haven't seen tile yet, check if we see one
      // Check if within 3 blocks of the tile (represents the agent seeing the tile and going)
      Vector2 closeTile = getCloseTile(tileType, 3, currentCell, GameManager.instance.getBoardArray());
      if (closeTile.x >= 0 && closeTile.y >= 0) {
        this.target = closeTile;
        goalSteering = arriveAt(
          new Vector2(closeTile.x * CELL_SIZE, closeTile.y * CELL_SIZE),
          new Vector2(currentCell.x * CELL_SIZE, currentCell.y * CELL_SIZE),
          rigidBody.velocity, SLOW_RADIUS, ARRIVE_RADIUS, MAX_SPEED);
      } else {
        // If we still can't see a bush, just follow the smell
        goalSteering = directionOfSmell(currentCell,
          GameManager.instance.getSmellArray(), smellType) * MAX_ACCEL;
      }
    }

    return goalSteering;
  }

  // ACTION METHOD: Sleep the agent for some given time, make sure that it is not 
  // moving.
  private float sleepStartTime = -1;
  public Vector2 sleep(ParticleSystem sleepParticles, float timeToSleep) {
    // Start sleeping
    if (sleepStartTime < 0) {
      sleepStartTime = Time.time;
      sleepParticles.Play();
    }

    // If we have been sleeping for given time (in seconds)
    if (Time.time > sleepStartTime + timeToSleep) {
      this.goal.apply(this.insistance);
      this.goal = null;
      this.target = new Vector2(-1, -1);
      this.sleepStartTime = -1;
      sleepParticles.Stop();
    }

    // Make sure the agent isn't moving
    rigidBody.angularVelocity = 0;
    rigidBody.rotation = 0;
    return new Vector2(-rigidBody.velocity.x, -rigidBody.velocity.y);
  }

  // ACTION METHOD make the agent wander using a seek behavior toward a point.
  // The point that the agent is seeking is based on the agnet's current velocity
  // and is offset by some random ammount
  private float wanderStartTime = -1;
  private float wanderAngle; // The angle of the target on the circle
  public Vector2 wander() {
    // First time wander is called, setup the circle where the seek location
    // will live
    if (this.wanderStartTime < 0) {
      wanderStartTime = Time.time;
      
      // Pick an angle between 0 and 360
      wanderAngle = Random.Range(0, 360);
    }

    // Update the wnader angle and Calculate the point to seek
    // The point to seek is on a circle in front of the agent at the wander angle
    // the wander angle moves randomly every frame.
    wanderAngle += Random.Range(-15, 15);
    Vector2 inFrontOfSheep = this.transform.position + this.transform.right * 2;
    Vector2 fromCenterToEdge = new Vector2(Mathf.Cos(wanderAngle), Mathf.Sin(wanderAngle));
    Vector2 pointOnCircle = inFrontOfSheep + fromCenterToEdge;

    if (Time.time > this.wanderStartTime + 10) {
      this.goal.apply(this.insistance);
      this.goal = null;
      this.target = new Vector2(-1, -1);
      this.wanderStartTime = -1;
    }

    return arriveAt(pointOnCircle, this.transform.position, rigidBody.velocity,
                    SLOW_RADIUS, ARRIVE_RADIUS, MAX_SPEED);
  }

  // Calculate rotation, Rotation is always in the direction of the 
  // current velocity.
  public float calculateRotation() {
    float targetOrientation = Mathf.Rad2Deg * Mathf.Atan2(rigidBody.velocity.y, rigidBody.velocity.x);
    
    // The target rotation depends on the radii for "arive" and "slow"
    float curOrientation = transform.eulerAngles.z;
    float targetRotation = targetOrientation - curOrientation;
    while (targetRotation > 180) targetRotation -= 360;
    while (targetRotation < -180) targetRotation += 360;

    if (Mathf.Abs(targetRotation) < ROTATE_ARRIVE_RAD) {
      targetRotation = 0;
    } else if (Mathf.Abs(targetRotation) < ROTATE_SLOW_RAD) {
      targetRotation = MAX_ROTATION * targetRotation / ROTATE_ARRIVE_RAD;
    } else {
      targetRotation = targetRotation > 0 ? MAX_ROTATION : -MAX_ROTATION;
    }

    // If the agent is stopped, make it stop rotating
    if (rigidBody.velocity.magnitude < 0.01) {
      targetRotation = -rigidBody.angularVelocity;
    }
    float angSteering = targetRotation - rigidBody.angularVelocity;
    return angSteering;
  }

  // Mutates the given insistance by increasing all insistances based on their
  // growth rates TODO MOVE TO CLASS
  public void increaseInsistances(Insistance insistance) {
    foreach (InsistanceType type in insistance.insistanceTypes) {
      insistance.insistances[type] += insistance.growthRates[type] * Time.deltaTime;
    }
  }

  // Return the location of the first object of the given food type within
  // the given distance of the location. If none is found, return (-1, -1)
  public Vector2 getCloseFood(BoardManager.Food type, int distance,
                               Vector2 currentCell, BoardManager.Food[, ] foodArray) {
    for (int xOffset = -distance; xOffset < distance + 1; xOffset++) {
      for (int yOffset = -distance; yOffset < distance + 1; yOffset++) {
        int curX = (int)currentCell.x + xOffset;
        int curY = (int)currentCell.y + yOffset;
        if (curX >= 0 && curX < GameManager.SIZE && curY >= 0 && curY < GameManager.SIZE) {
          if (foodArray[curX, curY] == type) {
            return new Vector2(curX, curY);
          }
        }
      }
    }

    return new Vector2(-1, -1);
  }

  // Return the location of the first object of the given tile type within the given 
  // distance of the location
  public Vector2 getCloseTile(BoardManager.TileType type, int distance,
                               Vector2 currentCell, BoardManager.TileType[, ] boardArray) {
    for (int xOffset = -distance; xOffset < distance + 1; xOffset++) {
      for (int yOffset = -distance; yOffset < distance + 1; yOffset++) {
        int curX = (int)currentCell.x + xOffset;
        int curY = (int)currentCell.y + yOffset;
        if (curX >= 0 && curX < GameManager.SIZE && curY >= 0 && curY < GameManager.SIZE) {
          if (boardArray[curX, curY] == type) {
            return new Vector2(curX, curY);
          }
        }
      }
    }

    // If we didn't find any tiles of the given type, return "dummy" vector (-1, -1)
    return new Vector2(-1, -1);
  }

  // Calculates the acceleration to seek a given target with a given velocity
  public Vector2 pursue(Vector2 targetLoc, Vector2 targetVel) {
    // Determine the time to reach the target's current location
    Vector2 targetDirection = (Vector2)this.transform.position - targetLoc;
    float timeToTarget = targetDirection.magnitude / rigidBody.velocity.magnitude;

    // Project the target forward by that ammount of time
    Vector2 seekLocation = targetLoc + targetVel * timeToTarget;

    // Move at max accel toward the seek location
    return seek(seekLocation);
  }

  // Accelerate at max value in the direction of the target
  public Vector2 seek(Vector2 target) {
    Vector2 direction = target - (Vector2)this.transform.position;

    // Scale the vector in direction to max-acceleration
    direction.Normalize();
    return direction * MAX_ACCEL;
  }

  // Calculates the force needed to make the agent slowly arrive at the
  // given location and gradualy come to a stop
  public Vector2 arriveAt(Vector2 targetLoc, Vector2 currentLoc, Vector2 currentVel, 
                          float slowRad, float targetRad, float maxSpeed) {
    float distToTarget = (targetLoc - currentLoc).magnitude;

    // Three distance cases from slow-radius and target-radius
    // Compute the target velocity in each case
    float targetSpeed;
    if (distToTarget > slowRad) {
      targetSpeed = maxSpeed;
    } else if (distToTarget > targetRad) {
      targetSpeed = (distToTarget / slowRad) * maxSpeed;
    } else {
      targetSpeed = 0;
    }

    // Get the target velocity including direction
    Vector2 targetVelocity = (targetLoc - currentLoc);
    targetVelocity.Normalize();
    targetVelocity = targetVelocity * targetSpeed;

    // The target acceleration is the difference between the current velocity
    // and the target velocity
    return targetVelocity - currentVel;
  }

  /* 
   * BEHAVIOR METHODS: The methods from the Behavior Interfeces that this agent
   * implements.
   */
  public Vector2 directionOfSmell(Vector2 location, Smell[, ] smells, SmellType type) {
    return smellFollower.directionOfSmell(location, smells, type);
  }
  public Vector2 avoidWalls(List<string> wallTags) {
    return wallAvoider.avoidWalls(wallTags);
  }
  public Vector2 avoidCollisions(List<string> collisionTags) {
    return collisionAvoider.avoidCollisions(collisionTags);
  }
  public Vector2 fleePoint(Vector2 point) {
    return fleer.fleePoint(point);
  }
  public Vector2 fleeTags(List<string> fleeTags) {
    return fleer.fleeTags(fleeTags);
  }

  // Used for displaying the info about this agent when it is clicked
  void OnMouseDown() {
    HudController.instance.setInfo(this);
  }
}
