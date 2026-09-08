using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AAZ.HD2D.Editor
{
    /// <summary>
    /// One-stop setup for the HD-2D layer: wires the renderer feature into the active URP
    /// renderer, authors a starting volume profile, and converts existing SpriteRenderers
    /// over to lit billboards.
    /// </summary>
    public sealed class HD2DSetupWindow : EditorWindow
    {
        const string k_SettingsFolder = "Assets/Settings";
        const string k_MaterialFolder = "Assets/Settings/HD2D Materials";
        const string k_ShaderName = "AAZ/HD2D/Sprite Lit";

        Vector2 m_Scroll;

        [MenuItem("Window/AAZ/HD-2D Setup")]
        public static void Open()
        {
            var window = GetWindow<HD2DSetupWindow>();
            window.titleContent = new GUIContent("HD-2D Setup");
            window.minSize = new Vector2(380f, 320f);
        }

        void OnGUI()
        {
            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);

            EditorGUILayout.LabelField("1. Render pipeline", EditorStyles.boldLabel);
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

            if (urpAsset == null)
            {
                EditorGUILayout.HelpBox(
                    "No Universal Render Pipeline asset is active. Assign one under " +
                    "Project Settings > Graphics before running this setup.",
                    MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox($"Active URP asset: {urpAsset.name}", MessageType.None);

                if (GUILayout.Button("Add tilt-shift renderer feature"))
                    AddRendererFeature(urpAsset);

                if (GUILayout.Button("Apply HD-2D quality defaults"))
                    ApplyQualityDefaults(urpAsset);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("2. Post processing", EditorStyles.boldLabel);
            if (GUILayout.Button("Create HD-2D volume profile"))
                CreateVolumeProfile();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("3. Sprites", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Select GameObjects in the scene (or a whole hierarchy root) and convert their " +
                "SpriteRenderers. Sprite swaps driven by an Animator keep working: the shader " +
                "reads _MainTex, which is what SpriteRenderer binds.",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(Selection.gameObjects.Length == 0))
            {
                if (GUILayout.Button($"Convert SpriteRenderers in selection ({Selection.gameObjects.Length} objects)"))
                    ConvertSelection();
            }

            EditorGUILayout.EndScrollView();
        }

        // ------------------------------------------------------------------

        static void AddRendererFeature(UniversalRenderPipelineAsset urpAsset)
        {
            var so = new SerializedObject(urpAsset);
            SerializedProperty list = so.FindProperty("m_RendererDataList");

            if (list == null || list.arraySize == 0)
            {
                Debug.LogError("[HD-2D] Could not read the URP asset's renderer list.");
                return;
            }

            int added = 0;
            for (int i = 0; i < list.arraySize; i++)
            {
                var data = list.GetArrayElementAtIndex(i).objectReferenceValue as ScriptableRendererData;
                if (data == null)
                    continue;

                if (data.rendererFeatures.Exists(f => f is HD2DTiltShiftFeature))
                    continue;

                var feature = CreateInstance<HD2DTiltShiftFeature>();
                feature.name = "HD-2D Tilt Shift";

                Undo.RegisterCreatedObjectUndo(feature, "Add HD-2D Tilt Shift");
                data.rendererFeatures.Add(feature);

                AssetDatabase.AddObjectToAsset(feature, data);
                EditorUtility.SetDirty(data);
                added++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(added > 0
                ? $"[HD-2D] Added the tilt-shift feature to {added} renderer(s)."
                : "[HD-2D] Every renderer already has the tilt-shift feature.");
        }

        static void ApplyQualityDefaults(UniversalRenderPipelineAsset urpAsset)
        {
            Undo.RecordObject(urpAsset, "HD-2D quality defaults");

            // Sprites need the depth texture (tilt-shift depth assist, SSAO) and the opaque
            // texture stays off because nothing in this look reads it.
            urpAsset.supportsCameraDepthTexture = true;
            urpAsset.supportsCameraOpaqueTexture = false;

            // HDR is non-negotiable: bloom on emissive lanterns and windows is a large part
            // of the HD-2D signature, and it needs values above 1 to bloom from.
            urpAsset.supportsHDR = true;

            EditorUtility.SetDirty(urpAsset);
            AssetDatabase.SaveAssets();

            Debug.Log("[HD-2D] Applied quality defaults (depth texture on, HDR on, opaque texture off).");
        }

        static void CreateVolumeProfile()
        {
            EnsureFolder(k_SettingsFolder);

            string path = AssetDatabase.GenerateUniqueAssetPath($"{k_SettingsFolder}/HD2D Volume Profile.asset");
            var profile = CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            var tiltShift = profile.Add<HD2DTiltShift>(true);
            tiltShift.intensity.Override(0.85f);
            tiltShift.focusCenter.Override(new Vector2(0.5f, 0.45f));
            tiltShift.bandWidth.Override(0.12f);
            tiltShift.falloff.Override(0.25f);
            tiltShift.nearStrength.Override(0.65f);
            tiltShift.farStrength.Override(1f);

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(0.75f);
            bloom.scatter.Override(0.7f);

            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);

            var colorAdjustments = profile.Add<ColorAdjustments>(true);
            colorAdjustments.postExposure.Override(0.15f);
            colorAdjustments.contrast.Override(8f);
            colorAdjustments.saturation.Override(10f);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.28f);
            vignette.smoothness.Override(0.4f);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = profile;
            EditorGUIUtility.PingObject(profile);
            Debug.Log($"[HD-2D] Created volume profile at {path}. Assign it to a global Volume in your scene.");
        }

        // ------------------------------------------------------------------

        static void ConvertSelection()
        {
            Shader shader = Shader.Find(k_ShaderName);
            if (shader == null)
            {
                Debug.LogError($"[HD-2D] Shader '{k_ShaderName}' not found.");
                return;
            }

            EnsureFolder(k_SettingsFolder);
            EnsureFolder(k_MaterialFolder);

            var renderers = new List<SpriteRenderer>();
            foreach (GameObject go in Selection.gameObjects)
                renderers.AddRange(go.GetComponentsInChildren<SpriteRenderer>(true));

            if (renderers.Count == 0)
            {
                Debug.LogWarning("[HD-2D] No SpriteRenderers found in the selection.");
                return;
            }

            var materialCache = new Dictionary<Texture, Material>();
            int converted = 0;

            foreach (SpriteRenderer sr in renderers)
            {
                Texture texture = sr.sprite != null ? sr.sprite.texture : null;
                if (texture == null)
                    continue;

                if (!materialCache.TryGetValue(texture, out Material material))
                {
                    material = GetOrCreateMaterial(shader, texture);
                    materialCache[texture] = material;
                }

                Undo.RecordObject(sr, "Convert to HD-2D sprite");
                sr.sharedMaterial = material;
                sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                sr.receiveShadows = true;
                EditorUtility.SetDirty(sr);
                converted++;

                FixTextureImport(texture);
            }

            Debug.Log($"[HD-2D] Converted {converted} SpriteRenderer(s) using {materialCache.Count} material(s).");
        }

        static Material GetOrCreateMaterial(Shader shader, Texture texture)
        {
            string safeName = MakeSafeName(texture.name);
            string path = $"{k_MaterialFolder}/HD2D_{safeName}.mat";

            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.shader = shader;
                return existing;
            }

            var material = new Material(shader) { name = $"HD2D_{safeName}" };
            material.SetTexture("_MainTex", texture);

            // Yaw-only billboarding by default: it keeps vertical pixel columns vertical,
            // which matters far more for pixel art than facing the camera exactly.
            material.SetFloat("_Billboard", 1f);
            material.EnableKeyword("_BILLBOARD_YAXIS");
            material.DisableKeyword("_BILLBOARD_NONE");
            material.DisableKeyword("_BILLBOARD_FULL");

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static void FixTextureImport(Texture texture)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(path))
                return;

            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                return;

            bool dirty = false;

            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                dirty = true;
            }

            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                dirty = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                dirty = true;
            }

            if (dirty)
                importer.SaveAndReimport();
        }

        static string MakeSafeName(string raw)
        {
            var chars = raw.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '_' && chars[i] != '-')
                    chars[i] = '_';
            }
            return new string(chars);
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            int slash = folder.LastIndexOf('/');
            string parent = folder.Substring(0, slash);
            string leaf = folder.Substring(slash + 1);

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
