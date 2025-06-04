# Paint-me-up
Android AR Game Prototype

- Game Loop: Move around to detect the ground, press the button to create a cube, and start the game. You have 180 seconds to find the colours shown in the UI in the real world. Once any correct colour is selected or 30 seconds passes a new one is shown. Every correct color found is painted on one cube face!
    - Colours are simplified so the colour match doesn't have to be exact.

- This is a Game App created in unity using AR Foundations, ProBuilder and Unity 6 XR Interaction Toolkit for input detection.

- Playable version download: https://jgodinho.itch.io/paint-me-up-mobile-game 
- Showcase Video: https://youtu.be/_MK79-4ivwc

- When downloading the project from GitHub, after opening the project in the Unity Editor, switch build profile to Android, then go to Assets -> Scenes -> GameScene_MoreModular for used game scene.
    
- Script Structure:
    - ARCubeSpawner.cs is responsible for making sure a plane has been detected and if so enabling the cube placement button. If pressed it then creates a probuilder cube in the scene
    - CubePainter.cs updates the color an a cube face to a new given color by updating the vertex colors of cube face of a probuilder cube mesh 
    - GameReset.cs simply reloads the scene on a button pressed (called onClick on the end screen)
    - PaintingManager.cs was the originally created class to handle the core game logic, handling player taps, getting the camera image, analysing the colors and then updating the UI accordingly. It was then later split into four classes.
        - PaintMeUpGameManager.cs - handles the core game logic once a cube has been spawned, including timers and such.
        - PlayerTapHandler.cs handles player taps once the game has begun and calls ColourScanner.cs when a touch is done.
        - ColourScanner.cs handles the conversion from a XRCpuImage into a texture and then into pixels to check for colors either of the whole image to get the 6 most frequent simplified colors (at the game setup) or at the press position of a touch (during the game).
        - GameUIManager.cs handles UI updates and calling on the end screen by listing to the PaintMeUpGameManager.cs.

- Note: Sounds, shaking and particle effects were added after initial build.

- External Sound Assets Used:
    - https://www.youtube.com/watch?v=iHxrseIqpNk
    - https://www.youtube.com/watch?v=dt2kfez86P0
    - https://www.youtube.com/watch?v=QqvLHzWaF_s
