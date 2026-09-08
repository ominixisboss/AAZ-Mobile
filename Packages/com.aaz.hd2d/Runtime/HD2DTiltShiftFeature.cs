using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
#endif

namespace AAZ.HD2D
{
    /// <summary>
    /// Renderer feature that applies the HD-2D tilt-shift depth of field. Add it to the
    /// Universal Renderer Data asset; drive it with an <see cref="HD2DTiltShift"/> volume override.
    /// </summary>
    public sealed class HD2DTiltShiftFeature : ScriptableRendererFeature
    {
        [SerializeField]
        [Tooltip("Runs before URP's post stack so bloom picks up the softened highlights, " +
                 "which is what gives HD-2D its dreamy glow.")]
        RenderPassEvent m_Event = RenderPassEvent.BeforeRenderingPostProcessing;

        [SerializeField, HideInInspector]
        Shader m_Shader;

        Material m_Material;
        TiltShiftPass m_Pass;

        public override void Create()
        {
            if (m_Shader == null)
                m_Shader = Shader.Find("Hidden/AAZ/HD2D/TiltShift");

            if (m_Shader == null)
                return;

            if (m_Material == null)
                m_Material = CoreUtils.CreateEngineMaterial(m_Shader);

            m_Pass = new TiltShiftPass(m_Material) { renderPassEvent = m_Event };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (m_Pass == null || m_Material == null)
                return;

            var cameraData = renderingData.cameraData;
            if (cameraData.cameraType == CameraType.Preview || cameraData.cameraType == CameraType.Reflection)
                return;
            if (!cameraData.postProcessEnabled)
                return;

            var settings = VolumeManager.instance.stack.GetComponent<HD2DTiltShift>();
            if (settings == null || !settings.IsActive())
                return;

            m_Pass.Setup(settings);
            m_Pass.renderPassEvent = m_Event;

            if (settings.depthInfluence.value > 0f)
                m_Pass.ConfigureInput(ScriptableRenderPassInput.Depth);

            renderer.EnqueuePass(m_Pass);
        }

        protected override void Dispose(bool disposing)
        {
            m_Pass?.Dispose();
            m_Pass = null;
            CoreUtils.Destroy(m_Material);
            m_Material = null;
        }

        // ------------------------------------------------------------------

        sealed class TiltShiftPass : ScriptableRenderPass
        {
            static readonly int s_BlurTexId = Shader.PropertyToID("_HD2DBlurTex");
            static readonly int s_BandId = Shader.PropertyToID("_TiltShiftBand");
            static readonly int s_AxisId = Shader.PropertyToID("_TiltShiftAxis");
            static readonly int s_ParamsId = Shader.PropertyToID("_TiltShiftParams");
            static readonly int s_DepthId = Shader.PropertyToID("_TiltShiftDepth");

            const int k_PassCopy = 0;
            const int k_PassBlurH = 1;
            const int k_PassBlurV = 2;
            const int k_PassComposite = 3;

            readonly Material m_Material;
            readonly ProfilingSampler m_Sampler = new ProfilingSampler("HD-2D Tilt Shift");

            HD2DTiltShift m_Settings;
            int m_Downsample = 2;
            int m_Iterations = 2;

            RTHandle m_Sharp;
            RTHandle m_BlurA;
            RTHandle m_BlurB;

            public TiltShiftPass(Material material)
            {
                m_Material = material;
                profilingSampler = m_Sampler;
            }

            public void Setup(HD2DTiltShift settings)
            {
                m_Settings = settings;
                m_Downsample = Mathf.Max(1, settings.downsample.value);
                m_Iterations = Mathf.Max(1, settings.iterations.value);
            }

            /// <summary>Pushes the volume values onto the blit material.</summary>
            void ApplyMaterialParameters()
            {
                var s = m_Settings;
                float rad = Mathf.Deg2Rad * s.angle.value;

                m_Material.SetVector(s_BandId, new Vector4(
                    s.focusCenter.value.x,
                    s.focusCenter.value.y,
                    s.bandWidth.value,
                    s.falloff.value));

                m_Material.SetVector(s_AxisId, new Vector4(
                    Mathf.Cos(rad),
                    Mathf.Sin(rad),
                    s.nearStrength.value,
                    s.farStrength.value));

                m_Material.SetVector(s_ParamsId, new Vector4(
                    s.blurRadius.value,
                    s.intensity.value,
                    s.maskPower.value,
                    s.depthInfluence.value));

                m_Material.SetVector(s_DepthId, new Vector4(
                    s.focusDistance.value,
                    s.focusRange.value,
                    s.depthFalloff.value,
                    0f));
            }

            static RenderTextureDescriptor GetDescriptor(RenderTextureDescriptor src, int divisor)
            {
                var desc = src;
                desc.depthBufferBits = 0;
                desc.msaaSamples = 1;
                desc.useMipMap = false;
                desc.autoGenerateMips = false;
                desc.width = Mathf.Max(1, src.width / divisor);
                desc.height = Mathf.Max(1, src.height / divisor);
                return desc;
            }

            public void Dispose()
            {
                m_Sharp?.Release();
                m_BlurA?.Release();
                m_BlurB?.Release();
                m_Sharp = m_BlurA = m_BlurB = null;
            }

#if UNITY_6000_0_OR_NEWER
            // ---------------- Render Graph path (Unity 6 default) ----------------

            class CompositeData
            {
                public Material material;
                public TextureHandle sharp;
                public TextureHandle blur;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (m_Settings == null || m_Material == null)
                    return;

                var resourceData = frameData.Get<UniversalResourceData>();
                if (resourceData.isActiveTargetBackBuffer)
                    return;

                ApplyMaterialParameters();

                var cameraData = frameData.Get<UniversalCameraData>();
                TextureHandle cameraColor = resourceData.activeColorTexture;

                RenderTextureDescriptor fullDesc = GetDescriptor(cameraData.cameraTargetDescriptor, 1);
                RenderTextureDescriptor smallDesc = GetDescriptor(cameraData.cameraTargetDescriptor, m_Downsample);

                TextureHandle sharp = UniversalRenderer.CreateRenderGraphTexture(
                    renderGraph, fullDesc, "_HD2DSharp", false, FilterMode.Bilinear, TextureWrapMode.Clamp);
                TextureHandle blurA = UniversalRenderer.CreateRenderGraphTexture(
                    renderGraph, smallDesc, "_HD2DBlurA", false, FilterMode.Bilinear, TextureWrapMode.Clamp);
                TextureHandle blurB = UniversalRenderer.CreateRenderGraphTexture(
                    renderGraph, smallDesc, "_HD2DBlurB", false, FilterMode.Bilinear, TextureWrapMode.Clamp);

                // Keep an unblurred full-res copy: we cannot read and write camera colour at once.
                renderGraph.AddBlitPass(
                    new RenderGraphUtils.BlitMaterialParameters(cameraColor, sharp, m_Material, k_PassCopy),
                    "HD2D TiltShift Copy");

                renderGraph.AddBlitPass(
                    new RenderGraphUtils.BlitMaterialParameters(sharp, blurA, m_Material, k_PassCopy),
                    "HD2D TiltShift Downsample");

                for (int i = 0; i < m_Iterations; i++)
                {
                    renderGraph.AddBlitPass(
                        new RenderGraphUtils.BlitMaterialParameters(blurA, blurB, m_Material, k_PassBlurH),
                        "HD2D TiltShift Blur H");
                    renderGraph.AddBlitPass(
                        new RenderGraphUtils.BlitMaterialParameters(blurB, blurA, m_Material, k_PassBlurV),
                        "HD2D TiltShift Blur V");
                }

                using (var builder = renderGraph.AddRasterRenderPass<CompositeData>(
                           "HD2D TiltShift Composite", out var passData, m_Sampler))
                {
                    passData.material = m_Material;
                    passData.sharp = sharp;
                    passData.blur = blurA;

                    builder.UseTexture(sharp);
                    builder.UseTexture(blurA);
                    builder.SetRenderAttachment(cameraColor, 0);

                    builder.SetRenderFunc((CompositeData data, RasterGraphContext ctx) =>
                    {
                        ctx.cmd.SetGlobalTexture(s_BlurTexId, data.blur);
                        Blitter.BlitTexture(ctx.cmd, data.sharp, new Vector4(1f, 1f, 0f, 0f),
                                            data.material, k_PassComposite);
                    });
                }
            }
#endif

            // ---------------- Compatibility path (URP 14-16, and Unity 6 with Render Graph disabled) ----------------

#pragma warning disable 618
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                var srcDesc = renderingData.cameraData.cameraTargetDescriptor;

                AllocIfNeeded(ref m_Sharp, GetDescriptor(srcDesc, 1), FilterMode.Bilinear, "_HD2DSharp");
                AllocIfNeeded(ref m_BlurA, GetDescriptor(srcDesc, m_Downsample), FilterMode.Bilinear, "_HD2DBlurA");
                AllocIfNeeded(ref m_BlurB, GetDescriptor(srcDesc, m_Downsample), FilterMode.Bilinear, "_HD2DBlurB");
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (m_Settings == null || m_Material == null)
                    return;

                var cameraColor = renderingData.cameraData.renderer.cameraColorTargetHandle;
                if (cameraColor == null || m_Sharp == null)
                    return;

                ApplyMaterialParameters();

                CommandBuffer cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, m_Sampler))
                {
                    Blitter.BlitCameraTexture(cmd, cameraColor, m_Sharp, m_Material, k_PassCopy);
                    Blitter.BlitCameraTexture(cmd, m_Sharp, m_BlurA, m_Material, k_PassCopy);

                    for (int i = 0; i < m_Iterations; i++)
                    {
                        Blitter.BlitCameraTexture(cmd, m_BlurA, m_BlurB, m_Material, k_PassBlurH);
                        Blitter.BlitCameraTexture(cmd, m_BlurB, m_BlurA, m_Material, k_PassBlurV);
                    }

                    cmd.SetGlobalTexture(s_BlurTexId, m_BlurA);
                    Blitter.BlitCameraTexture(cmd, m_Sharp, cameraColor, m_Material, k_PassComposite);
                }

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }
#pragma warning restore 618

            static void AllocIfNeeded(ref RTHandle handle, in RenderTextureDescriptor desc,
                                      FilterMode filterMode, string name)
            {
#if UNITY_6000_0_OR_NEWER
                RenderingUtils.ReAllocateHandleIfNeeded(ref handle, desc, filterMode, TextureWrapMode.Clamp, name: name);
#else
                RenderingUtils.ReAllocateIfNeeded(ref handle, desc, filterMode, TextureWrapMode.Clamp, name: name);
#endif
            }
        }
    }
}
