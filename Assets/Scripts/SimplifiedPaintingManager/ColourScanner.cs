using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// TODO - Improvements: Combine ProcessCameraImage() ProcessTouchAtCurrentFrame() where both take a bool, XRCpuImage and a Vector2
// - If bool is false it is starting image (ignores Vector2) and calls AnalyseTextureColors()
// - If bool is true it is the current frame touch that matters (uses both XRCpuImage and a Vector2)
//   and only takes the current pixel colour to call CorrectColorSelected() if colour is a needed one

// For initial image processing
// - Gets camera image, processes it and gets the 6 most frequent colours, the calls on PaintMeUpGameManager SetSavedColours() to save them
// For player taps, gets called on a successful tap camera image from PlayerTapHandler
// - Processes it and calls on PaintMeUpGameManager AddCompletedColours() and AddNextTargetColour() to save the completed colour and add a new target One
public class ColourScanner : MonoBehaviour
{
    private PaintMeUpGameManager manager;

    private ARCubeSpawner spawner;

    [SerializeField]
    ARCameraManager camManager; // used to get camera image to analyse initial colours and colours of player tap

    private bool isGettingCameraImage = false;

    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private AudioClip incorrectColorClip;

    [SerializeField]
    private AudioClip successColorClip;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        manager = PaintMeUpGameManager.Instance;
        spawner = ARCubeSpawner.Instance;
    }

    private void Update()
    {
        // So that once the game starts it gets the camera image
        if(manager != null && manager.gameStarted && !isGettingCameraImage)
        {
            isGettingCameraImage = true;
            GetCameraImage();
        }
    }

    // ------ If game started, get camera image and get its colours --------------

    public void GetCameraImage()
    {
        if (camManager.TryAcquireLatestCpuImage(out XRCpuImage realWorldImage))
        {
            StartCoroutine(ProcessCameraImage(realWorldImage));
            realWorldImage.Dispose(); //no need to be stored after used
        }
        else
        {
            Debug.LogError("Could not acquire current CPU image to start the game.");
        }
    }

    // Converts XRCpu to texture
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
        foreach (var pixel in pixels)
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
        var savedColours = colorCount.OrderByDescending(kvp => kvp.Value) // order by most frequent color - CAN BE CHANGE if we want any detected colour
                                .Take(6) // 6 most frequent colors
                                .Select(kvp => kvp.Key) // by Color32
                                .ToList();

        manager.SetSavedColours(savedColours);
    }

    // ------ Called from Player Tap Handler --------------

    // Checks position at tap position, if wrong color (from any of the active ones) nothing happens,
    // if correct then it calls on CorrectColorSelected
    // Is a Coroutine in case of multiple fast player taps
    public IEnumerator ProcessTouchAtCurrentFrame(XRCpuImage image, Vector2 pressPos)
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
        if (!manager.currentTargetColours.Contains(simplifiedTapped) || manager.completedColours.Contains(simplifiedTapped))
        {
            // Play Incorrect Colour Sound
            if (audioSource != null && incorrectColorClip != null)
            {
                audioSource.PlayOneShot(incorrectColorClip);
                Debug.Log("Playing incorrect color selected audio clip!");
            }

            // Vibrates phone on incorrect tap for 1 second - Only works on Android, and may not work on all versions
            Handheld.Vibrate();
            Debug.Log("Vibrating phone");
            yield return null;
        }
        else
        {
            // If here it is a valid needed color
            // Play Completed Sound
            if (audioSource != null && successColorClip != null)
            {
                audioSource.PlayOneShot(successColorClip);
                Debug.Log("Playing success audio clip!");
            }

            CorrectColorSelected(simplifiedTapped);
            yield return null;
        }
    }

    // Sets the current color as completed, then calls
    // ARCubeSpawner's cube gameobject to update its face colours through CubePainter's AddColourToFace method
    private void CorrectColorSelected(Color32 colour)
    {
        Debug.Log("Correct color tapped: " + colour.ToString());

        Debug.Log("Complete Colours before additon are : " + manager.completedColours.Count);

        // Add to completed colours
        manager.AddCompletedColours(colour);
        //HashSet<Color32> newCompletedColours = new();
        //newCompletedColours = manager.completedColours;
        //newCompletedColours.Add(colour);


        Debug.Log("Complete Colours after additon are : " + manager.completedColours.Count);
        // To add the colour to a cube face
        if (spawner.SpawnedCube != null)
        {
            spawner.SpawnedCube.GetComponent<CubePainter>().AddColourToFace((manager.completedColours.Count - 1), colour);
        }
        Debug.Log("Completed Colours: " + string.Join(", ", manager.completedColours));

        // Add new target colour and reset timer
        manager.AddNextTargetColour();
    }

    
}
