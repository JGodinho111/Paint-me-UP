using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Handles UI Changes when called upon
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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i < imagesToShowColours.Count; i++)
        {
            imagesToShowColours[i].color = new Color32((byte)imagesToShowColours[i].color.r, (byte)imagesToShowColours[i].color.g, (byte)imagesToShowColours[i].color.b, 0);
        }

        if (endScreenUI != null)
        {
            endScreenUI.SetActive(false);
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
                imagesToShowColours[i].color = new Color32((byte)imagesToShowColours[i].color.r, (byte)imagesToShowColours[i].color.g, (byte)imagesToShowColours[i].color.b, 50);
            }
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
        }
    }

    public void showEndScreens(bool winCondition)
    {
        if (endScreenUI != null)
        {
            if (!winCondition)
            {
                Debug.LogError("Game Lost");
                endScreenUI.SetActive(true);
                endScreenUI.GetComponentInChildren<TMP_Text>().SetText("You lost at Paint me UP!");
            }
            else
            {
                Debug.LogError("Game Won");
                endScreenUI.SetActive(true);
                endScreenUI.GetComponentInChildren<TMP_Text>().SetText("You won at Paint me UP!");
            }
        }
    }
}
