# Leap Tilt Five Testing
 Unity project of [Ultraleap hand tracking](https://www.ultraleap.com/tracking/) interactions with [Tilt Five AR glasses](https://www.tiltfive.com/)

![Bracket mounted to TiltFive glasses](imgs/leap-tiltfive-header.jpg)

## Introduction
 This repo contains example Unity scenes exploring HMD and Desktop-mode hand tracking with the Tilt Five AR system. 'HMD' and 'Desktop' modes refer to the expected Ultraleap tracking device orientation: mounted to the TiltFive glasses or positioned next to the gameboard facing upward. Note how the interactions in each example depend on the device orientation. The chess game is not suited for Desktop-mode because often the user must reach past the Ultraleap sensor's field of view. Moreover, consider if your app requires physical interaction or simple heuristics can provide better control to your users. The 'Chess Game' example demonstrates grid-snapping and object highlighting to provide clear feedback in the absence of haptics when interacting 1-to-1 with virtual world. The 'Hand Cursor' example demonstrates two methods of projecting hand position onto the gameboard, great for objects out of reach. The 'Turntable' example demonstrates how to rotate an object smoothly, with momentum, and with either or both hands. Finally, the '3D User Interface' scene demonstrates physically simulated buttons and sliders on a panel hovering above the Tilt Five board. The variety of demos are intended to demonstrate that there are several ways to solve a design problem. Whatever the implementation, fast and accurate hand tracking paired with AR glasses is a magical experience.

## Requirements:
 - Tilt Five Driver 1.1.0+
 - Unity 2019.4 LTS or later
 - Ultraleap Tracking Gemini 5.6+
 - (Tested on Windows with [Ultraleap Unity plugin 5.11.0](https://github.com/ultraleap/UnityPlugin/releases/) )

## Basic Scene Setup:
 - If hands are not showing, open the Ultraleap Control Panel to check if image is inverted
 - Ultraleap tracking uses the scale of 1 world unit = 1 real world meter. Set the 'Content Scale' in Tilt Five Manager' gameobject to 1 meter for reference
 - Tracking space alignment can be accomplished by either moving the 'Leap Rig' gameobject for Desktop examples, or adjusting the 'Device Origin' in LeapXRServiceProvider for HMD modes (i.e. the chess example)
 - Instead of trying to align Tilt Five and Leap space and depending on 1-to-1 physically simulated interactions, sometimes mapping hand data to a heuristic can improve interaction consistency and user experience. See the chess example use proximity and pinch amount, and how the turntable example keeps track of fingertips within a volume instead of trying to simulate contact forces. 

## 3D Printed bracket (optional)
 For the HMD-mode tracked example, the Ultraleap device is expected to be mounted to the glasses. The STL files can be found in ["Assets > CAD Files"](/Assets/CAD%20Files) The mount prints with a consumer FDM printer with the following settings:
 - 0.2mm layer height
 - 3 perimeters
 - 10% infill
 - No supports
 - Skirt outlines or raft recommended

![print orientation](imgs/tiltfive_mount2_printOrientation3.PNG)
![print orientation](imgs/print_post.jpg)

 The design includes sacrificial geometry instead of support material that needs to be removed (colored in orange in photo above). The mount adheres to the glasses using mounting tape, and the Ultraleap device is fastened using threadforming plastic screws and 3D printed bushings. See the BOM and 'SIR170_screw_adapter.stl' in ["Assets > CAD Files"](/Assets/CAD%20Files) for more info.

![CAD design](imgs/tiltfive_CAD_Assm.PNG)

## Examples

 Using Ultraleap hand tracking mounted to Tilt Five glasses virtual boardgames are literally in reach. Note that each example is designed around the device orientation. Some interactions are better suited for certain device orientations. For example, the chess game interaction would not work well in Desktop-mode because the hand often exceeds the field of view of camera when reaching over the board. As an alternative, the 'Hand Cursor' scene demonstrates 2 methods of impelementing a virtual cursor to interact with objects out of reach. Furthermore, when direction interaction is required, 'Extendo Hands' example shows how to project the virtual hands past the Tilt Five board.

### Chess Game (HMD mode)
 The user can grab and move pieces as they expect them to be in the real world. The interaction logic can be found on the 'Chess Game' gameobject under 'Tilt Five Game Board.' Note the use of proximity and pinch to determine the grab state of each gamepiece. This method is more predictable for the user than the sometimes explosive use of physics and colliders, as well as being forgiving in device alignment. The hand tracking 'Leap Service Provider' is on the 'Tilt Five Camera' gameobject. Note that alignment between hand and image can be adjusted by changing 'SIR170 Location' gameobject relative to 'Tilt Five Camera' gameobject.
![TiltFive hand interaction board game](imgs/hmd_interaction.gif)

### Extendo Hands (Desktop)
 Although virtual images can extend down into the Tilt Five board, our hands cannot. However, this example demonstrates using a post process to project the Leap hands as a function of arm extension. In addition, it demonstrates the use of Interaction Engine as an extensible way to interact naturally with virtual objects.
![TiltFive hand reaching example](imgs/extendohands.gif)

### Hand Cursor (Desktop)
 The example scene demonstrates two methods of controlling a virtual cursor with your hands. Sometimes it's not practical to interact 1-to-1 with virtual objects and selecting from a distance is preferable. The script 'Hand 2D Projection' on the 'Leap Rig' maps two cursors to hand movements. The first is a direct mapping between hand position and cursor position. The second cursor is determined from a raycast starting from a virtual shoulder through users' index finger knuckle. This enables a more natural directionality to the cursor, and the cursor position becomes invariant to pinch gesture. Note that this example demonstrates a different world scaling ( Content Scale: 1 world unit = 5cm) in order to highlight the advantage of virtual cursors in the not world scale context.
![TiltFive hand interaction board game](imgs/handcursor.gif)

### Turntable (Desktop)
 The Leap Turntable example uses the change in fingertip positions to calculate and apply a rotational velocity that dampens over time. This demonstrates the advantages of using 'interaction heuristics' rather than physically replicating interactions of colliders touching. Precise definition of any finger tip within the turntable volume will calculate velocity. It would be less reliable and require more code to replicate the physical interaction of the friction of a finger applied to a turntable. The interaction zone where fingertips affect rotation can be customized using the table height and radius fields. The table can even have a tapered edge using 'Edge Angle' field to minimize the user inadvertently spinning the object.
![TiltFive hand interaction board game](imgs/turntable.gif)

### 3D User Interface (Desktop)
 This scene physically simulates buttons and sliders for user to try. The virtual UI panel floats at an angle above the board, and requires a good alignment between the Tilt Five and Leap’s space to feel real. Simply moving the Desktop-mode tracking device can better align your hands. The buttons simulate a mass-spring-damper system. Note the different field of views between Ultraleap tracking devices and where they are located. Design your UI accordingly.

![TiltFive hand interaction UI panel](imgs/3dui.gif)

 