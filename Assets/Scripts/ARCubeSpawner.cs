using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using System;

/// <summary>
/// Singleton Class that hanldes cube spawning on a button press if a plane is detected (trackables count is above 0)
/// Using an AR Raycast Manager spawns a cube 1.5f forward and away from the player camera and keeps a reference to it
/// So other classes can access that cube to make changes to it
/// And disables the button for other cube creation (only one is used in the game per game loop)
/// </summary>
public class ARCubeSpawner : MonoBehaviour
{
    [SerializeField]
    private ARRaycastManager raycastManager;
    [SerializeField]
    private ARPlaneManager planeManager;
    [SerializeField]
    private Camera arCamera;
    [SerializeField]
    private Button spawnButton;
    [SerializeField]
    private GameObject cubePrefab;
    private float spawnDistance = 1.5f;

    private static List<ARRaycastHit> hits = new();

    public bool cubeSpawned { get; private set; } = false;

    public static ARCubeSpawner Instance { get; private set; }

    public GameObject SpawnedCube { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        spawnButton.onClick.AddListener(SpawnCubeOnPlane);
        spawnButton.interactable = false; // Disabling cube button until plane is found
    }

    void Update()
    {
        // Enable button if at least one plane is being tracked and no cubes have been spawned
        if (!spawnButton.interactable && planeManager.trackables.count > 0 && !cubeSpawned)
        {
            spawnButton.interactable = true;
        }
    }

    void SpawnCubeOnPlane()
    {
        // Get a point in front of the camera
        Vector3 forwardPoint = arCamera.transform.position + arCamera.transform.forward * spawnDistance;

        // Raycast from screen point (convert world point to screen point)
        Vector2 screenPoint = arCamera.WorldToScreenPoint(forwardPoint);

        if (raycastManager.Raycast(screenPoint, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;

            // Spawn cube slightly above the plane to allow for visualization of bottom of the cube
            Vector3 spawnPosition = hitPose.position + Vector3.up * 0.5f;
            SpawnedCube = Instantiate(cubePrefab, spawnPosition, Quaternion.identity);

            cubeSpawned = true; // for this class not to go into the update
            spawnButton.interactable = false; // Disable after one spawn
        }
        else
        {
            Debug.LogWarning("No plane detected at spawn point.");
        }
    }
}
