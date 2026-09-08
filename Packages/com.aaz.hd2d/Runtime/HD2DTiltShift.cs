using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace AAZ.HD2D
{
    /// <summary>
    /// Volume override driving <see cref="HD2DTiltShiftFeature"/>. Because it lives on the
    /// volume stack you can blend the diorama effect per-area: strong in a town, off in a
    /// boss arena, and it interpolates as the player walks between triggers.
    /// </summary>
    [Serializable]
    [VolumeComponentMenu("AAZ/HD-2D Tilt Shift")]
    public sealed class HD2DTiltShift : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Master strength. 0 disables the pass entirely.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

        [Header("Focus band")]
        [Tooltip("Where the sharp band sits in viewport space. 0.5,0.5 is screen centre; " +
                 "HD-2D usually sits it slightly below centre so the player stays crisp.")]
        public Vector2Parameter focusCenter = new Vector2Parameter(new Vector2(0.5f, 0.45f));

        [Tooltip("Half-height of the fully sharp band, in viewport units.")]
        public ClampedFloatParameter bandWidth = new ClampedFloatParameter(0.12f, 0f, 0.5f);

        [Tooltip("Distance from the edge of the sharp band to fully blurred.")]
        public ClampedFloatParameter falloff = new ClampedFloatParameter(0.25f, 0.01f, 1f);

        [Tooltip("Rotation of the focus band in degrees. 0 is horizontal.")]
        public ClampedFloatParameter angle = new ClampedFloatParameter(0f, -90f, 90f);

        [Tooltip("Shapes the ramp. >1 keeps more of the screen sharp and blurs late.")]
        public ClampedFloatParameter maskPower = new ClampedFloatParameter(1.5f, 0.25f, 4f);

        [Header("Asymmetry")]
        [Tooltip("Blur strength on the near (lower) side of the band.")]
        public ClampedFloatParameter nearStrength = new ClampedFloatParameter(0.7f, 0f, 1f);

        [Tooltip("Blur strength on the far (upper) side of the band. Usually higher than near.")]
        public ClampedFloatParameter farStrength = new ClampedFloatParameter(1f, 0f, 1f);

        [Header("Blur")]
        [Tooltip("Gaussian radius in texels of the half-resolution blur buffer.")]
        public ClampedFloatParameter blurRadius = new ClampedFloatParameter(1.6f, 0.25f, 4f);

        [Tooltip("Number of blur iterations. Each iteration widens the blur but costs a full pair of passes.")]
        public ClampedIntParameter iterations = new ClampedIntParameter(2, 1, 4);

        [Tooltip("Resolution divisor for the blur buffer. 2 = half res. Raise it on mobile.")]
        public ClampedIntParameter downsample = new ClampedIntParameter(2, 1, 4);

        [Header("Depth assist (optional)")]
        [Tooltip("Blends true scene depth into the mask so tall geometry near the camera " +
                 "does not stay artificially sharp. Costs a depth texture fetch.")]
        public ClampedFloatParameter depthInfluence = new ClampedFloatParameter(0f, 0f, 1f);

        [Tooltip("Distance from the camera that stays in focus, in metres.")]
        public MinFloatParameter focusDistance = new MinFloatParameter(12f, 0.1f);

        [Tooltip("Depth range around the focus distance that stays sharp, in metres.")]
        public MinFloatParameter focusRange = new MinFloatParameter(6f, 0f);

        [Tooltip("Distance over which depth ramps to fully blurred, in metres.")]
        public MinFloatParameter depthFalloff = new MinFloatParameter(20f, 0.01f);

        public bool IsActive() => intensity.value > 0f;

        // Present for URP 14/15; harmless on versions where the interface dropped it.
        public bool IsTileCompatible() => false;
    }
}
