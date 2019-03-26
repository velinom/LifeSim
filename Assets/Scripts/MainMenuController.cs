using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour {

  public GameObject START_BUTTON;

  private ParticleSystem startParticles;

  void Start() {
    startParticles = START_BUTTON.GetComponent<ParticleSystem>();
  }

  // Called when the mouse enters the play button
  // Starts the particle system
  public void onStartButtonEnter() {
    startParticles.Play();
  }

  // Called when the mouse leaves the play button
  // Stops the particle system
  public void onStartButtonLeave() {
    startParticles.Stop();
  }

  // Loads the main game
  public void onStartButtonClick() {
    SceneManager.LoadScene(1);
  }
}
