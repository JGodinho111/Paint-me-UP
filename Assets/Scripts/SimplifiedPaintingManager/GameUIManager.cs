using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// TODO - Put more verifications for UI update since it appears on some correct colours the cube is updating, but not the UI

// Handles UI Changes when called upon
// Only accesses PaintMeUpGameManager and UI items
public class GameUIManager : MonoBehaviour
{
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

    // Instance of manager which it calls on variables to know of state changes
    private PaintMeUpGameManager manager;

    private bool startingColoursShown = false;
    private bool currentTargetColourUpdated = false;
    private bool uISetUpComplete = false;
    private bool endScreenShown = false;

    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private AudioClip endClip;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        manager = PaintMeUpGameManager.Instance;

        for (int i = 0; i < imagesToShowColours.Count; i++)
        {
            imagesToShowColours[i].color = new Color32((byte)imagesToShowColours[i].color.r, (byte)imagesToShowColours[i].color.g, (byte)imagesToShowColours[i].color.b, 0);
        }

        if (endScreenUI != null)
        {
            endScreenUI.SetActive(false);
        }
    }
    private void Update()
    {
        // Starts newTargetColours UI and timers UI
        if (manager != null && manager.gameStarted && manager.gameSetupComplete && manager.savedColoursSet && !startingColoursShown)
        {
            startingColoursShown = true;
            ColourDisplay(manager.savedColours.Count); // Adds initial colour display UI and sets timers
        }

        // Update 180s timer & 30s timer UI every second here, to not have be called from the PaintMeUpManager
        if (manager != null && manager.gameStarted && manager.timerSet && manager.gameSetupComplete && uISetUpComplete)
        {
            setTimer(false, manager.mainTimer); // false is main timer
            setTimer(true, manager.coloursTimer); // true is colours timer
        }

        // Update newTargetColours UI once a new TargetColour is added
        if (manager != null && manager.gameStarted && manager.gameSetupComplete && manager.newTargetColourAdded && !currentTargetColourUpdated && uISetUpComplete)
        {
            currentTargetColourUpdated = true;
            UpdateImagesUI(manager.currentTargetColours, manager.savedColours, manager.completedColours);
        }

        // If endScreen has been called it is then displayed
        if(manager.gameEnded && !endScreenShown)
        {
            endScreenShown = true;
            showEndScreens();
        }

    }

    // Creates Starting Colours UI and timer text fields, and clearing the target colours and completed colours fields
    // Calls the AddNextTargetColour() to start the game colours showing
    public void ColourDisplay(int savedColoursCount)
    {
        if (savedColoursCount == 6)
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

            uISetUpComplete = true;
        }
    }

    public void setTimer(bool timer, float timerValue)
    {
        if (!timer && mainTimerText != null)
        {
            mainTimerText.SetText(((int)timerValue).ToString());
        }
        if (timer && coloursTimerText != null)
        {
            coloursTimerText.SetText(((int)timerValue).ToString());
        }
    }

    // TODO - Double check here, there might be an error
    public void UpdateImagesUI(List<Color32> currentTargetColours, List<Color32> savedColours, HashSet<Color32> completedColours)
    {
        // For UI Update
        for (int i = 0; i < currentTargetColours.Count; i++)
        {
            // If color not already saved
            if (!imagesToShowColours[i].color.CompareRGB(savedColours[i])) // Not comparing opacity as wanted
            {
                imagesToShowColours[i].color = savedColours[i];
            }
            // Lower Opacity if already completed
            if (completedColours.Contains(currentTargetColours[i]))
            {
                imagesToShowColours[i].color = new Color32((byte)imagesToShowColours[i].color.r, (byte)imagesToShowColours[i].color.g, (byte)imagesToShowColours[i].color.b, 100);
            }
        }

        currentTargetColourUpdated = false;
        manager.TargetColourAddedUIupdate();
    }

    public void showEndScreens() //(bool winCondition)
    {
        if (endScreenUI != null)
        {
            // Play end sound
            if (audioSource != null && endClip != null)
            {
                audioSource.PlayOneShot(endClip);
                Debug.LogError("Playing game end audio clip!");
            }

            if (!manager.gameEndingCondition)
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
}
