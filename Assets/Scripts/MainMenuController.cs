using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour {

  // Reference to the start button to controll the particle system
  public GameObject START_BUTTON;
  private ParticleSystem startParticles;

  // Reference to the input field so the value can be set in GameManager
  public InputField INPUT;

  // Load a refference to the start button particle system
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

  // Called when the user changes the value in the seed box
  public void onSeedFieldChanged() {
    int num = int.Parse(INPUT.text);

    if (num == 0 || INPUT.text.Length == 0) {
      GameManager.instance.setSeed(-1);
      GameManager.instance.setUseSeed(false);
    } else {
      GameManager.instance.setSeed(num);
      GameManager.instance.setUseSeed(true);
    }
  }
}
