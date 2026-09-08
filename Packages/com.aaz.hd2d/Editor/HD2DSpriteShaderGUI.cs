using UnityEditor;
using UnityEngine;

namespace AAZ.HD2D.Editor
{
    /// <summary>
    /// Material inspector for <c>AAZ/HD2D/Sprite Lit</c>. Draws the normal property list and
    /// adds the checks that actually bite in practice: pixel art wrecked by bilinear
    /// filtering or block compression.
    /// </summary>
    public sealed class HD2DSpriteShaderGUI : ShaderGUI
    {
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            base.OnGUI(materialEditor, properties);

            var material = materialEditor.target as Material;
            if (material == null)
                return;

            EditorGUILayout.Space();
            WarnAboutTextureImport(material);
        }

        static void WarnAboutTextureImport(Material material)
        {
            Texture tex = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
            if (tex == null)
                return;

            string path = AssetDatabase.GetAssetPath(tex);
            if (string.IsNullOrEmpty(path))
                return;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            bool pointFiltering = importer.filterMode == FilterMode.Point;
            bool uncompressed = importer.textureCompression == TextureImporterCompression.Uncompressed;

            if (pointFiltering && uncompressed)
                return;

            EditorGUILayout.HelpBox(
                "This sprite is not imported for pixel art. Bilinear filtering blurs the pixel " +
                "grid and block compression smears flat colour fields - both are very visible " +
                "once a sprite is magnified by a narrow-FOV HD-2D camera.",
                MessageType.Warning);

            if (!GUILayout.Button("Fix import settings (Point filter, uncompressed, no mips)"))
                return;

            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
    }
}
