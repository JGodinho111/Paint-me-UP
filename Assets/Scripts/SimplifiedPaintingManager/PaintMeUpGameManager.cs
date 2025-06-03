using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

// Singleton that handles Core Game Logic, timers and is called by other classes
public class PaintMeUpGameManager : MonoBehaviour
{
    public static PaintMeUpGameManager Instance { get; private set; }

    private ARCubeSpawner spawner; // to get reference to bool of cube being launched and direct access to the cube gameobject

    [SerializeField]
    ARCameraManager camManager; // used to get camera image to analyse initial colours and colours of player tap

    public bool gameStarted { get; private set; } = false;

    // --------- For Colour Checking --------- 
    // NOTE: These can't be HashSets since I access their indexes at least once
    public List<Color32> savedColours { get; private set; } = new(); // List<Color32>(); //more efficient than just Color
    public List<Color32> currentTargetColours { get; private set; } = new(); // List<Color32>();

    public HashSet<Color32> completedColours { get; private set; } = new(); // HashSet<Color32>();

    // --------- For Timers --------- 
    public float mainTimer { get; private set; } = 180f;
    public float coloursTimer { get; private set; } = 30f;
    private float resetTimer = 30f; // can be negative, if all 6 colours are already displayed
    public bool timerSet { get; private set; } = false;

    public bool gameSetupComplete { get; private set; } = false; // So inputs only start after the initial image has been processed correctly and the 6 colours assigned

    public bool gameEnded { get; private set; } = false; // For UiManager to call end screens
    public bool gameEndingCondition { get; private set; } = false; // For UiManager to call correct end screen
    public bool newTargetColourAdded { get; private set; } = false; // For UiManager to update side colours when new one is added
    public bool savedColoursSet { get; private set; } = false; // For UiManager to add locally the colours to display at the start


    //GameUIManager uiManager;
    //ColourScanner colourScanner;

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

        //uiManager = FindFirstObjectByType<GameUIManager>();
        //colourScanner = FindFirstObjectByType<ColourScanner>();
    }

    void Update()
    {
        // Once cube has been spawned, start game logic (accessing image and determining colours)
        if (spawner.cubeSpawned && !gameStarted)
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
            //uiManager.setTimer(false, mainTimer); // false is main timer
            //uiManager.setTimer(true, coloursTimer); // true is colours timer
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

    // --------------------------------------------------- Game Start after cube generated ---------------------------------------------------

    // Start Game Logic
    void StartGame()
    {
        gameStarted = true;
        Debug.Log("Game Started");
        //colourScanner.GetCameraImage();
    }

    // Called By ColourScanner once initial image colours have been retrieved
    public void SetSavedColours(List<Color32> newColours)
    {
        savedColours = newColours;
        Debug.Log("6 Colours to find: " + string.Join(", ", savedColours));

        savedColoursSet = true; // For ui to set up
        //uiManager.ColourDisplay(savedColours.Count); // Adds initial colour display UI and sets timers

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

    // Called By ColourScanner once a new colour has been completed
    public void AddCompletedColours(Color32 newColour)
    {
        completedColours.Add(newColour);
    }

    // Called only by UI manger once new colour is added to ui
    public void TargetColourAddedUIupdate() 
    {
        newTargetColourAdded = false;
    }

    // -- Setting Up Target Colour (used on both initial setup and after each correct colour is selected --

    // Add new target colour and reset timer, plus update colors shown UI
    // Called By ColourScanner and by the class itself
    public void AddNextTargetColour()
    {
        if (currentTargetColours.Count == savedColours.Count)
        {
            newTargetColourAdded = true;
            return;
        }

        var newTargetColour = savedColours[currentTargetColours.Count];
        currentTargetColours.Add(newTargetColour);

        coloursTimer = resetTimer;

        Debug.Log("New Target Colour Added: " + string.Join(", ", newTargetColour));
        Debug.Log("Current Target Colours: " + string.Join(", ", currentTargetColours));
        Debug.Log("Current Completed Colours: " + string.Join(", ", completedColours));

        // If it is the first target colour and it is setup then the game can begin
        if (currentTargetColours.Count == 1)
        {
            StartCoroutine(Delay()); // Small delay to avoid problems if player is spamming the button
        }

        //uiManager.UpdateImagesUI(currentTargetColours, savedColours, completedColours);
        newTargetColourAdded = true;

        // note: Just to check if this solves the issue of UI updates (even if I shouldn't technically call it if I'm a singleton)
        //FindFirstObjectByType<GameUIManager>().UpdateImagesUI(currentTargetColours, savedColours, completedColours);
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

        gameEndingCondition = winCondition;
        gameEnded = true; // for uiManager to call 
        //uiManager.showEndScreens(winCondition);
    }

    // ------------------------------------------------------------------------------------------------------
}
