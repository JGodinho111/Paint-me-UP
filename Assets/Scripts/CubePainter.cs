using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;


/// <summary>
/// Initializes the cube mesh and sets start colour at white
/// Then when calling AddColourToFace from PaintingManager it adds the color to each face
/// Right now uses ProbuilderMesh with a Probuilder 6 Standard Vertex Color Shader
/// </summary>
// OLD
// In order for the faces to be painted correctly according to vertices I need each face to have independent vertices,
// So here I create a custom cube mesh at start (right as its being instantiated in ARCubeSpawner
// Then in the PaintFaceMethod, called by the PaintingManager it colours a given face, given an index (0 to 5) and a colour
//OLD - [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[RequireComponent(typeof(ProBuilderMesh))]
public class CubePainter : MonoBehaviour
{
    //private Mesh cubeMesh;
    //private Color[] colours;

    private ProBuilderMesh cubeMesh;
    private Color[] colours;

    [SerializeField]
    private ParticleSystem correctSelectionAnimation;

    /*private void Awake()
    {
        GenerateMeshData();
        colours = new Color[cubeMesh.vertices.Length];
        for (int i = 0; i < colours.Length; i++)
        {
            colours[i] = Color.white;
            
        }
        cubeMesh.colors = colours;
    }*/

    private bool initialColourSet = false;

    private void Start()
    {
        // Assigning CubeMesh
        cubeMesh = GetComponent<ProBuilderMesh>();

        if(cubeMesh == null)
        {
            Debug.LogError("Cube Mesh not found");
            return;
        }

        // Checking faces
        var faces = cubeMesh.faces; // saving in a temporary variable since it is better for performance, but mostly insignificant
        Debug.Log("Total faces is " + faces.Count + " of what should be 6 faces");

        // Setting up colours for each vertex
        colours = new Color[cubeMesh.vertexCount];

        /*foreach(var face in faces)
        {
            foreach(int i in face.distinctIndexes)
            {
                colours[i] = Color.black;
            }
        }*/

        // Painting the cube white
        for(int i = 0; i < faces.Count; i++)
        {
            AddColourToFace(i, Color.white);
        }
    }

    // Given a face index, gets unique indexes for the given face (corresponding to the index)
    // Which relate to the vertices and updates the colour for each of those
    // Then reapplies the colours for the cubeMesh (ProBuilder Mesh)
    public void AddColourToFace(int faceIndex, Color colourToAdd) // Was having issues with Color32, switched to Color
    {
        var faces = cubeMesh.faces; // saving in a temporary variable since it is better for performance, but mostly insignificant

        // Changing the colour on the vertexes corresponding to the face Index face vertexes
        foreach (int i in cubeMesh.faces[faceIndex].distinctIndexes)
        {
            colours[i] = colourToAdd;
        }

        cubeMesh.colors = colours;
        cubeMesh.ToMesh();
        cubeMesh.Refresh();

        // Show UI Particle Effect
        if (correctSelectionAnimation != null && initialColourSet)
        {
            ParticleSystem ps = Instantiate(correctSelectionAnimation, transform.position, Quaternion.identity);

            ps.Play();
            Debug.Log("Playing animation!");
            Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
        }
        initialColourSet = true;

        Debug.Log("Cube face " + faceIndex + " painted with rgb Color " + colourToAdd);
        Debug.Log("rgb Color " + colourToAdd + " corresponds to Color32 " + (Color32)colourToAdd);

        // NOTE - OLD METHOD - Using the created cube mesh, rahter than a probuilder mesh
        // Each face has 4 unique vertices, from 0 to 3 for the first, followed by 4 to 7, etc
        /*int[] faceVertexes = new int[] { faceIndex * 4, faceIndex * 4 + 1, faceIndex * 4 + 2, faceIndex * 4 + 3 };

        if (faceIndex < 0 || faceIndex > 5)
        {
            Debug.LogError("Invalid cube face index, must be from 0 to 5.");
            return;
        }

        foreach (int i in faceVertexes)
        {
            colours[i] = colourToAdd;
        }

        cubeMesh.colors = colours;*/
    }

    // OLD METHOD - Something Regarding this method I am generating either UVs or normals incorrectly
    // After consideration the error may have actually come from the material shader I was using
    /*private void GenerateMeshData()
    {
        cubeMesh = new Mesh(); // Old Way - Used the standard unity mesh, did not work -> this.gameObject.GetComponent<MeshFilter>().mesh;
        cubeMesh.name = "Colorable Cube Mesh";

        //Lenght is 24: 4 vertices per cube face, 6 faces, all independent
        Vector3[] verticesGenerated = {
            
            // Front (+Z)
            new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3( 0.5f, -0.5f, 0.5f),
            new Vector3( 0.5f,  0.5f, 0.5f),
            new Vector3(-0.5f,  0.5f, 0.5f),

            // Back (-Z)
            new Vector3( 0.5f, -0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(-0.5f,  0.5f, -0.5f),
            new Vector3( 0.5f,  0.5f, -0.5f),

            // Left (-X)
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f,  0.5f),
            new Vector3(-0.5f,  0.5f,  0.5f),
            new Vector3(-0.5f,  0.5f, -0.5f),

            // Right (+X)
            new Vector3( 0.5f, -0.5f,  0.5f),
            new Vector3( 0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f,  0.5f, -0.5f),
            new Vector3( 0.5f,  0.5f,  0.5f),

            // Top (+Y)
            new Vector3(-0.5f,  0.5f,  0.5f),
            new Vector3( 0.5f,  0.5f,  0.5f),
            new Vector3( 0.5f,  0.5f, -0.5f),
            new Vector3(-0.5f,  0.5f, -0.5f),

            // Bottom (-Y)
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f, -0.5f,  0.5f),
            new Vector3(-0.5f, -0.5f,  0.5f),
        };

        //Length is 36: 2 triangles per face, 3 vertexes each, for each of the 6 faces
        //each int is the vertex index corresponding to each of its vertexes
        int[] trianglesGenerated = {
            // Front (+Z)
            0, 1, 2, 0, 2, 3,
            // Back (-Z)
            4, 5, 6, 4, 6, 7,
            // Left (-X)
            8, 9, 10, 8,10,11,
            // Right (+X)
            12,13,14, 12,14,15,
            // Top (+Y)
            16,17,18, 16,18,19,
            // Bottom (-Y)
            20,21,22, 20,22,23  
        };

        cubeMesh.vertices = verticesGenerated;
        cubeMesh.triangles = trianglesGenerated;
        this.gameObject.GetComponent<MeshFilter>().mesh = cubeMesh;
        //cubeMesh.RecalculateNormals();

    }*/
}
