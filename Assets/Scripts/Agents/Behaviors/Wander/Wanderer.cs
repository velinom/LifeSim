using UnityEngine;

public class Wanderer : IWanderer {

  // The angle from the center of the circle to the point the agent
  // is seeking on the circle. The angle moves randomly each frame
  private float wanderAngle;

  // The seeker that this wanderer will use to seek the point on the circle
  private ISeeker seeker;

  // The transform of the seeker
  public Transform transform;

  public Wanderer(float maxAccel, Transform trans) {
    this.wanderAngle = Random.Range(0, 360);
    this.transform = trans;

    this.seeker = new Seeker(maxAccel, trans);
  }

  // Return the steering vector for the agent to wander, seeks a point on 
  // a circle projected forward from the agent, the point moves arround the circle
  // randomly.
  public Vector2 wander() {

    // Update the wnader angle and Calculate the point to seek
    // The point to seek is on a circle in front of the agent at the wander angle
    // the wander angle moves randomly every frame.
    wanderAngle += Random.Range(-20, 20);
    Vector2 inFrontOfSheep = transform.position + transform.right * 2;
    Vector2 fromCenterToEdge = new Vector2(Mathf.Cos(wanderAngle), Mathf.Sin(wanderAngle));
    Vector2 pointOnCircle = inFrontOfSheep + fromCenterToEdge;

    return seeker.seek(pointOnCircle);
  }
}