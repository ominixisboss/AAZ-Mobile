using UnityEngine;

namespace AAZ.HD2D
{
    /// <summary>
    /// Fixed-angle diorama camera. HD-2D framing is a long lens looking down at a shallow
    /// angle: the narrow FOV flattens perspective so sprites stay readable, while the tilt
    /// exposes enough ground plane to show off the 3D set.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("AAZ/HD-2D/Camera Rig")]
    public sealed class HD2DCameraRig : MonoBehaviour
    {
        [Header("Target")]
        public Transform target;

        [Tooltip("Offset from the target's pivot to the point the camera looks at. " +
                 "Raise it to roughly chest height so the character sits low in frame.")]
        public Vector3 targetOffset = new Vector3(0f, 1.1f, 0f);

        [Header("Framing")]
        [Tooltip("Downward tilt in degrees. 30-45 is the usual HD-2D range: shallow enough " +
                 "that sprites keep their full height, steep enough to reveal the floor.")]
        [Range(5f, 80f)] public float pitch = 38f;

        [Tooltip("Orbit angle around the target in degrees.")]
        public float yaw;

        [Tooltip("Distance from the look-at point, in metres.")]
        [Min(0.1f)] public float distance = 14f;

        [Tooltip("Vertical field of view. Keep it narrow (20-35) for the telephoto, " +
                 "miniature-diorama compression that defines the look.")]
        [Range(8f, 60f)] public float fieldOfView = 26f;

        [Header("Follow")]
        [Tooltip("Seconds for the camera to catch up to the target. 0 snaps instantly.")]
        [Min(0f)] public float followSmoothTime = 0.18f;

        [Tooltip("Seconds for a yaw change to settle. 0 snaps instantly.")]
        [Min(0f)] public float rotationSmoothTime = 0.25f;

        [Header("Pixel grid")]
        [Tooltip("Snap the look-at point to a world-space grid so sprite pixels stop " +
                 "shimmering as the camera drifts. Set to your sprites' pixels-per-unit; " +
                 "0 disables snapping.")]
        [Min(0f)] public float pixelsPerUnit;

        Camera m_Camera;
        Vector3 m_SmoothedFocus;
        Vector3 m_FocusVelocity;
        float m_SmoothedYaw;
        float m_YawVelocity;
        bool m_Initialized;

        void OnEnable()
        {
            m_Camera = GetComponent<Camera>();
            m_Initialized = false;
        }

        void OnValidate()
        {
            if (m_Camera == null)
                m_Camera = GetComponent<Camera>();

            ApplyLens();
        }

        void LateUpdate()
        {
            if (m_Camera == null)
                m_Camera = GetComponent<Camera>();

            ApplyLens();

            Vector3 desiredFocus = target != null
                ? target.position + targetOffset
                : m_SmoothedFocus;

            if (!m_Initialized)
            {
                m_SmoothedFocus = desiredFocus;
                m_SmoothedYaw = yaw;
                m_FocusVelocity = Vector3.zero;
                m_YawVelocity = 0f;
                m_Initialized = true;
            }
            else
            {
                float dt = Application.isPlaying ? Time.deltaTime : 1f;

                m_SmoothedFocus = followSmoothTime > 0f && Application.isPlaying
                    ? Vector3.SmoothDamp(m_SmoothedFocus, desiredFocus, ref m_FocusVelocity, followSmoothTime, Mathf.Infinity, dt)
                    : desiredFocus;

                m_SmoothedYaw = rotationSmoothTime > 0f && Application.isPlaying
                    ? Mathf.SmoothDampAngle(m_SmoothedYaw, yaw, ref m_YawVelocity, rotationSmoothTime, Mathf.Infinity, dt)
                    : yaw;
            }

            Vector3 focus = Snap(m_SmoothedFocus);
            Quaternion rotation = Quaternion.Euler(pitch, m_SmoothedYaw, 0f);

            transform.SetPositionAndRotation(focus - rotation * Vector3.forward * distance, rotation);
        }

        void ApplyLens()
        {
            if (m_Camera == null)
                return;

            m_Camera.orthographic = false;
            m_Camera.fieldOfView = fieldOfView;
        }

        Vector3 Snap(Vector3 worldPoint)
        {
            if (pixelsPerUnit <= 0f)
                return worldPoint;

            float step = 1f / pixelsPerUnit;
            return new Vector3(
                Mathf.Round(worldPoint.x / step) * step,
                Mathf.Round(worldPoint.y / step) * step,
                Mathf.Round(worldPoint.z / step) * step);
        }

        /// <summary>Rotates the rig by <paramref name="degrees"/>, e.g. from a rotate-view input.</summary>
        public void Orbit(float degrees) => yaw += degrees;
    }
}
