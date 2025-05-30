using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// OLD - Before Separating into more modular classes
/// Class that manages all core game functionalities after the cube is in scene
/// It takes an image from the camera, converts it, gets 6 colors (the most common ones) and saves them
/// Sets up and updates the core game timer and the hidden 30s timer accordingly
/// Shows colors needed
/// Hanldes Taps and correct ones update the colors to tap, the UI and
/// calls on ARCubeSpawner's cube gameobject to update its face colours through CubePainter's AddColourToFace method
/// Also handles Game Loss and Win according to timer and colors selected
/// </summary>
public class PaintingManager : MonoBehaviour
{
    bool gameStarted = false;

    public static PaintingManager Instance { get; private set; }

    private ARCubeSpawner spawner; // to get reference to bool of cube being launched and direct access to the cube gameobject

    [SerializeField]
    ARCameraManager camManager; // used to get camera image to analyse initial colours and colours of player tap

    bool timerSet = false;

    // --------- For Colour Checking --------- 
    private List<Color32> savedColours = new(); // List<Color32>(); //more efficient than just Color
    private List<Color32> currentTargetColours = new(); // List<Color32>();

    private HashSet<Color32> completedColours = new(); // HashSet<Color32>();

    // --------- For Timers --------- 
    private float mainTimer = 180f;
    private float coloursTimer = 30f;
    private float resetTimer = 30f; // can be negative, if all 6 colours are already displayed

    private int coloursToChoose = 6;

    [SerializeField]
    private InputActionReference tapAction; // Reference to player tap

    private bool gameSetupComplete = false; // So inputs only start after the initial image has been processed correctly and the 6 colours assigned

    // --------- UI Fields --------- 
    [SerializeField]
    private TMP_Text mainTimerText;

    [SerializeField]
    private TMP_Text coloursTimerText;
    // UI Colour Images
    [SerializeField]
    private List<Image> imagesToShowColours = new List<Image>();

    [SerializeField]
    private GameObject endScreenUI;

    // --------------------------------------------------- Singleton Setup &  ARCubeSpawner instance reference ---------------------------------------------------

    // Instantiating Singleton
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // Getting ARCubeSpawner instance and initial setup of UI images of colours and disabling endScreen from view
    void Start()
    {
        spawner = ARCubeSpawner.Instance;

        for (int i = 0; i < imagesToShowColours.Count; i++)
        {
            imagesToShowColours[i].color = new Color32((byte)imagesToShowColours[i].color.r, (byte)imagesToShowColours[i].color.g, (byte)imagesToShowColours[i].color.b, 0);
        }

        if (endScreenUI != null)
        {
            endScreenUI.SetActive(false);
        }
    }

    // For the new Unity Input System, using a reference to an already existing Touchscreen Gesture (Tap Start Position)
    private void OnEnable()
    {
        if(tapAction != null)
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

    // --------------------------------------------------- Game Start & Press Position Analysis ---------------------------------------------------

    void Update()
    {
        // Once cube has been spawned, start game logic (accessing image and determining colours)
        if(spawner.cubeSpawned && !gameStarted)
        {
            // Enable Game Start
            StartGame();
        }

        // Start & Update 180s timer & 30s timer
        if (gameStarted && timerSet && gameSetupComplete)
        {
            mainTimer -= Time.deltaTime;
            coloursTimer -= Time.deltaTime;
            Debug.Log("Game Timer is " + mainTimer.ToString());
            Debug.Log("Colour Timer is " + coloursTimer.ToString());
            if(mainTimerText != null)
            {
                mainTimerText.SetText(((int)mainTimer).ToString());
            }
            if (coloursTimerText != null)
            {
                coloursTimerText.SetText(((int)coloursTimer).ToString());
            }

        }

        // if all 6 colors correctly chosen, it stops both timers and shows a Win Screen (pop-up win canvas with text and a go again button)
        // If not it calls the 30s timer coroutine (it Resets directly inside of that)
        if (gameStarted && timerSet && gameSetupComplete)
        {
            // Loss Condition
            if (mainTimer <= 0f)
            {
                CallOnEndScreen(false);
                return;
            }

            // 30s Timer Reset if timer reaches 0
            if (coloursTimer <= 0f && currentTargetColours.Count < savedColours.Count)
            {
                AddNextTargetColour();
                coloursTimer = resetTimer;
            }

            // Win Condition
            if (completedColours.Count == savedColours.Count)
            {
                CallOnEndScreen(true);
                return;
            }
        }
    }

    // Called on each player screen tap once game is ready
    void OnTap(InputAction.CallbackContext ctx)
    {
        if (gameStarted && timerSet && gameSetupComplete)
        {
            Vector2 pressPos = Touchscreen.current.primaryTouch.startPosition.ReadValue();

            Debug.Log("Touch Detected at " + pressPos);
            CheckColourAtTouch(pressPos);
        }
    }

    // Gets the screen camera image at the time of the press
    private void CheckColourAtTouch(Vector2 pressPos)
    {
        if (camManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            StartCoroutine(ProcessTouchAtCurrentFrame(image, pressPos));
            image.Dispose();
        }
        else
        {
            Debug.LogWarning("Could not acquire current CPU image for player tap.");
        }
    }

    // Checks position at tap position, if wrong color (from any of the active ones) nothing happens,
    // if correct then it calls on CorrectColorSelected
    // Is a Coroutine in case of multiple fast player taps
    private IEnumerator ProcessTouchAtCurrentFrame(XRCpuImage image, Vector2 pressPos)
    {
        // Creating XRCpuImage ConversionParams
        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, image.width, image.height),
            outputDimensions = new Vector2Int(image.width, image.height),
            outputFormat = TextureFormat.RGBA32,
            transformation = XRCpuImage.Transformation.MirrorY // adjusting for camera mirroring
        };

        // Converting image with conversion params and texture data
        var rawTextureData = new NativeArray<byte>(image.GetConvertedDataSize(conversionParams), Allocator.Temp);
        image.Convert(conversionParams, rawTextureData);

        Texture2D tempTexture = new Texture2D(image.width, image.height, TextureFormat.RGBA32, false);
        tempTexture.LoadRawTextureData(rawTextureData);
        tempTexture.Apply();
        rawTextureData.Dispose();

        // NOTE: Instead of rawTextureData I could also use a buffer

        // Map screen touch to texture coordinates
        Vector2 normalized = new Vector2(pressPos.x / Screen.width, pressPos.y / Screen.height);
        int x = Mathf.Clamp((int)(normalized.x * tempTexture.width), 0, tempTexture.width - 1);
        int y = Mathf.Clamp((int)(normalized.y * tempTexture.height), 0, tempTexture.height - 1);

        Color32 tappedColor = tempTexture.GetPixel(x, y);
        // Simplifying the colour so it doesn't need to be an exact pixel perfect match
        Color32 simplifiedTapped = new Color32((byte)(tappedColor.r / 32 * 32), (byte)(tappedColor.g / 32 * 32), (byte)(tappedColor.b / 32 * 32), 255);

        Debug.Log("Simplified Tapped color: " + simplifiedTapped.ToString());
        Debug.Log("Actual Tapped color: " + tappedColor.ToString());

        // Discard if it is not a target colour of has already been tapped before
        if (!currentTargetColours.Contains(simplifiedTapped) || completedColours.Contains(simplifiedTapped))
        {
            Handheld.Vibrate();
            Debug.LogError("Vibrating phone");
            yield return new WaitForSeconds(1f);
            yield return null;
        }
        else
        {
            // If here it is a valid needed color
            CorrectColorSelected(simplifiedTapped);
            yield return null;
        }
    }

    // Sets the current color as completed, then calls
    // ARCubeSpawner's cube gameobject to update its face colours through CubePainter's AddColourToFace method
    private void CorrectColorSelected(Color32 colour)
    {
        Debug.Log("Correct color tapped: " + colour.ToString());
        // Add to completed colours
        completedColours.Add(colour);

        // To add the colour to a cube face
        if(spawner.SpawnedCube != null)
        {
            spawner.SpawnedCube.GetComponent<CubePainter>().AddColourToFace((completedColours.Count - 1), colour);
        }
        Debug.Log("Completed Colours: " + string.Join(", ", completedColours));
        // Add new target colour and reset timer
        AddNextTargetColour();
    }

    // -- Setting Up Target Colour (used on both initial setup and after each correct colour is selected --

    // Add new target colour and reset timer, plus update colors shown UI
    private void AddNextTargetColour()
    {
        if (currentTargetColours.Count == savedColours.Count)
            return;

        var newTargetColour = savedColours[currentTargetColours.Count];
        currentTargetColours.Add(newTargetColour);

        coloursTimer = resetTimer;

        Debug.Log("New Target Colour Added: " + string.Join(", ", newTargetColour));
        Debug.Log("Current Target Colours: " + string.Join(", ", currentTargetColours));
        Debug.Log("Current Completed Colours: " + string.Join(", ", completedColours));

        // For UI Update
        for (int i = 0; i < currentTargetColours.Count; i++)
        {
            // If color not already saved
            if(!imagesToShowColours[i].color.CompareRGB(savedColours[i])) // Not comparing opacity as wanted
            {
                imagesToShowColours[i].color = savedColours[i];
            }
            // Lower Opacity if already completed
            if(completedColours.Contains(currentTargetColours[i]))
            {
                imagesToShowColours[i].color = new Color32((byte)imagesToShowColours[i].color.r, (byte)imagesToShowColours[i].color.g, (byte)imagesToShowColours[i].color.b, 50);
            }
        }

        // If it is the first target colour and it is setup then the game can begin
        if (currentTargetColours.Count == 1)
        {
            StartCoroutine(Delay()); // Small delay to avoid problems if player is spamming the button
            
        }

    }

    // this is only here to stop the tap action from being detected immediately once the game is set up
    // - on very rare ocasions it could lead to an auto complete of a colour
    private IEnumerator Delay()
    {
        // Wait for 1 second
        yield return new WaitForSeconds(1f);
        gameSetupComplete = true; // from now taps are handled
        yield return null;
    }

    // --------------------------------------------------- Game Start after cube generated ---------------------------------------------------

    // Start Game Logic
    void StartGame()
    {
        gameStarted = true;
        Debug.Log("Game Started");
        GetCameraImage();
    }

    // --------------------------------------------------- Image Analysis Start ---------------------------------------------------

    // Gets Camera Image, if it exists, it is processed in ProcessCameraImage
    private void GetCameraImage()
    {
        if(camManager.TryAcquireLatestCpuImage(out XRCpuImage realWorldImage))
        {
            StartCoroutine(ProcessCameraImage(realWorldImage));
            realWorldImage.Dispose(); //no need to be stored after used
        }
        else
        {
            Debug.LogError("Could not acquire current CPU image to start the game.");
        }

        // Not using AR Foundation - gets full screenshot (including the cube, so not used)
        //Texture2D screenShot = ScreenCapture.CaptureScreenshotAsTexture();
        //AnalyseTextureColors(screenShot);
    }

    // XRCpu is in YUV. Converts to texture
    // Same logic as ProcessTouchAtCurrentFrame, but without analysing a specific pixel after,
    // Rather calling on AnalyseTextureColors to get all pixel colors
    private IEnumerator ProcessCameraImage(XRCpuImage image)
    {
        Texture2D imageTexture = null; // = (Texture2D) image;

        // Creating XRCpuImage ConversionParams
        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, image.width, image.height),
            outputDimensions = new Vector2Int(image.width, image.height),
            outputFormat = TextureFormat.RGBA32,
            transformation = XRCpuImage.Transformation.MirrorY
        };

        // Converting image with conversion params and texture data
        var rawTextureData = new NativeArray<byte>(image.GetConvertedDataSize(conversionParams), Allocator.Temp);
        image.Convert(conversionParams, rawTextureData);

        imageTexture = new Texture2D(image.width, image.height, TextureFormat.RGBA32, false);
        imageTexture.LoadRawTextureData(rawTextureData);
        imageTexture.Apply();
        rawTextureData.Dispose();

        // NOTE: Instead of rawTextureData I could also use a buffer

        // Get Starting Image Texture Colours
        AnalyseTextureColors(imageTexture);

        yield return null;
    }

    // Analyses colours in the image and chooses 6 of them (6 most prominent ones) and saves them in a list
    private void AnalyseTextureColors(Texture2D imageTexture)
    {
        Dictionary<Color32, int> colorCount = new Dictionary<Color32, int>(); // to order each color by the ammount of times if appears
        Color32[] pixels = imageTexture.GetPixels32();

        // Analysing the pixels for their color and adding them to the colorCount dictionary in terms of frequency of appearances
        foreach(var pixel in pixels)
        {
            // Simplifying the colour so it doesn't need to be an exact pixel perfect match
            Color32 simplified = new Color32((byte)(pixel.r / 32 * 32), (byte)(pixel.g / 32 * 32), (byte)(pixel.b / 32 * 32), 255);

            if (colorCount.ContainsKey(simplified))
                colorCount[simplified]++;
            else
                colorCount[simplified] = 1;
        }

        // NOTE: Since I'm using the most common colours situations like this may happen:
        // e.g. in a living room with many browns I'll get many browns
        savedColours = colorCount.OrderByDescending(kvp => kvp.Value) // order by most frequent color - CAN BE CHANGE if we want any detected colour
                                .Take(coloursToChoose) // 6 most frequent colors
                                .Select(kvp => kvp.Key) // by Color32
                                .ToList();

        Debug.Log("6 Colours to find: " + string.Join(", ", savedColours));

        // Adds initial colour display UI and sets timers
        ColourDisplay();
    }

    // Creates Starting Colours UI and timer text fields, and clearing the target colours and completed colours fields
    // Calls the AddNextTargetColour() to start the game colours showing
    private void ColourDisplay()
    {
        if (savedColours.Count == coloursToChoose)
        {
            // Clear UI
            for (int i = 0; i < imagesToShowColours.Count; i++)
            {
                imagesToShowColours[i].color = new Color32(255, 255, 255, 0);
            }
            if (mainTimerText != null)
            {
                mainTimerText.SetText("");
            }
            if (coloursTimerText != null)
            {
                coloursTimerText.SetText("");
            }

            // Set Timers
            mainTimer = 180f;
            coloursTimer = resetTimer;
            timerSet = true;
            // Clear target and completed lists
            currentTargetColours.Clear();
            completedColours.Clear();

            // Set up first target colour
            AddNextTargetColour();
        }
    }

    // --------------------------------------------------- Displays End Screen ---------------------------------------------------

    // Displays win or loss screen
    private void CallOnEndScreen(bool winCondition)
    {
        // Resets game fields
        gameStarted = false;
        gameSetupComplete = false;
        mainTimer = 180f;
        coloursTimer = resetTimer;
        savedColours.Clear();
        currentTargetColours.Clear();
        completedColours.Clear();

        if (spawner != null)
            Destroy(spawner.SpawnedCube);

        if (endScreenUI != null)
        {
            if (!winCondition)
            {
                Debug.Log("Game Lost");
                endScreenUI.SetActive(true);
                endScreenUI.GetComponentInChildren<TMP_Text>().SetText("You lost at Paint me UP!");
            }
            else
            {
                Debug.Log("Game Won");
                endScreenUI.SetActive(true);
                endScreenUI.GetComponentInChildren<TMP_Text>().SetText("You won at Paint me UP!");
            }
        }
    }

    // ------------------------------------------------------------------------------------------------------
}
