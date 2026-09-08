using UnityEngine;
using UnityEngine.Rendering;

namespace AAZ.HD2D
{
    /// <summary>
    /// Publishes the rendering camera's position and up vector as global shader values.
    /// <para>
    /// The billboard shader cannot use <c>_WorldSpaceCameraPos</c>: URP rebinds it to the
    /// light's virtual camera while rendering shadow maps, so sprites would rotate to face
    /// the light and cast a shadow that does not match what the player sees. Publishing the
    /// real camera separately keeps the main pass and the shadow pass in agreement.
    /// </para>
    /// </summary>
    public static class HD2DCameraDirector
    {
        static readonly int s_CameraPositionId = Shader.PropertyToID("_HD2DCameraPosition");
        static readonly int s_CameraUpId = Shader.PropertyToID("_HD2DCameraUp");

        static bool s_Hooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init() => Hook();

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void InitEditor() => Hook();
#endif

        static void Hook()
        {
            if (s_Hooked)
                return;

            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            s_Hooked = true;
        }

        static void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera == null)
                return;

            Transform t = camera.transform;
            Shader.SetGlobalVector(s_CameraPositionId, t.position);
            Shader.SetGlobalVector(s_CameraUpId, t.up);
        }
    }
}
