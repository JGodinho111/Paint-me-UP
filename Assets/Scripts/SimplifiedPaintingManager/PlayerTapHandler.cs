using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// Handles Player Taps and Processes the info, which auto updates PaintMeUpGameManager
public class PlayerTapHandler : MonoBehaviour
{
    [SerializeField]
    private InputActionReference tapAction; // Reference to player tap

    private PaintMeUpGameManager manager;

    [SerializeField]
    ARCameraManager camManager; // used to get camera image to analyse initial colours and colours of player tap

    ColourScanner colourScanner;

    // For the new Unity Input System, using a reference to an already existing Touchscreen Gesture (Tap Start Position)
    private void OnEnable()
    {
        if (tapAction != null)
        {
            if (tapAction != null)
            {
                tapAction.action.Enable();
                tapAction.action.performed += OnTap;
            }
        }
    }
    private void OnDisable()
    {
        if (tapAction != null)
        {
            if (tapAction != null)
            {
                tapAction.action.performed -= OnTap;
                tapAction.action.Disable();
            }
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        manager = PaintMeUpGameManager.Instance;
        colourScanner = FindFirstObjectByType<ColourScanner>();
    }

    // Called on each player screen tap once game is ready
    void OnTap(InputAction.CallbackContext ctx)
    {
        if(manager != null)
        {
            if (manager.gameStarted && manager.timerSet && manager.gameSetupComplete)
            {
                Vector2 pressPos = Touchscreen.current.primaryTouch.startPosition.ReadValue();

                Debug.LogError("Touch Detected at " + pressPos);
                CheckColourAtTouch(pressPos);
            }
        }
        
    }

    // Gets the screen camera image at the time of the press
    private void CheckColourAtTouch(Vector2 pressPos)
    {
        if (camManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            StartCoroutine(colourScanner.ProcessTouchAtCurrentFrame(image, pressPos));
            image.Dispose();
        }
        else
        {
            Debug.LogWarning("Could not acquire current CPU image.");
        }
    }
}
