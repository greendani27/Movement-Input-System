using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private PlayerInput player;

    [SerializeField]
    private GameObject button;

    [SerializeField]
    private GameObject menu;

    [SerializeField]
    private InputActionReference pauseAction;

    bool isPaused = false;

    private void OnEnable()
    {
        pauseAction.action.Enable();
        pauseAction.action.performed += onPausePerformed;
    }

    private void OnDisable()
    {
        pauseAction.action.performed -= onPausePerformed;
        pauseAction.action.Disable();
    }

    private void onPausePerformed(InputAction.CallbackContext context) {
        isPaused = !isPaused;

        if (isPaused)
        {
            StartCoroutine(PauseRoutine());
        }
        else {
            StartCoroutine(ResumeRoutine());
        }
    }


    private IEnumerator PauseRoutine() {
        
        menu.SetActive(true);
        Time.timeScale = 0;
        EventSystem.current.SetSelectedGameObject(button);

        yield return null;
    }

    private IEnumerator ResumeRoutine() {
        menu.SetActive(false);
        Time.timeScale = 1f;
        yield return null;
    }
}
