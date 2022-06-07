using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TiltFive
{
    public enum GlassesMirrorMode
    {
        None,
        LeftEye,
        RightEye,
        Stereoscopic
    }

    /// <summary>
    /// GlassesSettings encapsulates all configuration data used by the Glasses'
    /// tracking runtime to compute the Head Pose and apply it to the Camera.
    /// </summary>
    [System.Serializable]
    public class GlassesSettings : TrackableSettings
    {
    #if UNITY_EDITOR
        /// <summary>
        /// Editor only configuration to disable/enable stereo-rendering.
        /// </summary>
        public bool tiltFiveXR = true;
    #endif
        /// <summary>
        /// The main camera used for rendering the Scene when the glasses are unavailable, and the gameobject used for the glasses pose.
        /// </summary>
        public Camera headPoseCamera;

        /// <summary>
        /// The near clip plane in physical space (meters), to adjust for content scale and gameboard size
        /// </summary>
        public float nearClipPlane = MIN_NEAR_CLIP_DISTANCE_IN_METERS;

        /// <summary>
        /// The far clip plane in physical space (meters), to adjust for content scale and gameboard size
        /// </summary>
        public float farClipPlane = 100f;

        public const float MIN_FOV = 35f;
        public const float MAX_FOV = 64f;
        public const float DEFAULT_FOV = 48f;

        // A default value will be returned by the client API if a custom IPD hasn't been set for
        // the glasses starting in version 1.1.0+, which means this constant won't be needed.  It
        // is kept for compatibility with older releases.
        public const float DEFAULT_IPD_UGBD = 0.059f;

        // Enforce a near clip plane that keeps objects from getting too close to the user's head.
        // TODO: Determine the threshold for discomfort (plus a small amount of margin) via usability testing.
        public const float MIN_NEAR_CLIP_DISTANCE_IN_METERS = 0.1f;

        public bool overrideFOV = false;
        public float customFOV = DEFAULT_FOV;
        public float fieldOfView => overrideFOV
            ? Mathf.Clamp(customFOV, MIN_FOV, MAX_FOV)
            : DEFAULT_FOV;

        public GlassesMirrorMode glassesMirrorMode = GlassesMirrorMode.LeftEye;

        public bool usePreviewPose = true;
        public Transform previewPose;

        public static readonly string DEFAULT_FRIENDLY_NAME = "Tilt Five Glasses";
        public string friendlyName = DEFAULT_FRIENDLY_NAME;
    }
}
