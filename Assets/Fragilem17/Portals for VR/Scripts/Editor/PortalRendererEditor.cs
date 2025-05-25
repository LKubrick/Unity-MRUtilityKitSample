#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Fragilem17.MirrorsAndPortals
{
    [CustomEditor(typeof(PortalRenderer))]
    public class PortalRendererEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            PortalRenderer portalRenderer = (PortalRenderer)target;

#if UNITY_2022_3_OR_NEWER && !UNITY_6000_0_OR_NEWER
            if (portalRenderer.UseSubmitRenderRequest)
            {
                EditorGUILayout.HelpBox("The Portal texture is now created using SubmitRenderRequest instead of RenderSingleCamera, enabling UI elements to be correctly reflected. However, this approach may trigger a runtime error indicating that recursive rendering is not supported. This error can be safely ignored and does not occur in more recent Unity versions.", MessageType.Warning);
            }
#endif

#if UNITY_6000_0_OR_NEWER

            bool renderGraphEnabled = RenderGraphEnabled();

            if (renderGraphEnabled && PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows)[0].ToString().Contains("Direct3D"))
            {
                EditorGUILayout.HelpBox("When RenderGraph is enabled (compatibility mode off) and the Windows Graphics API is set to Direct3D, the scene view fails to render portals and generates repeated console errors. To preview portals correctly in the scene, switch the Windows Graphics API to Vulkan or OpenGL.", MessageType.Warning);
            }

            if (renderGraphEnabled && PlayerSettings.GetGraphicsAPIs(BuildTarget.Android)[0].ToString().Contains("Vulkan"))
            {
                MessageType msgType = MessageType.Warning;
                if (portalRenderer.FlipSecondRecursion)
				{
                    msgType = MessageType.Info;
				}
                EditorGUILayout.HelpBox("When using RenderGraph with compatibility mode disabled and the Android Graphics API set to Vulkan, the second reflection within a portal (portal-in-portal effect) is flipped along the y-axis in the build. To resolve this issue, enable the 'Flip Second Recursion' option. This will show the second recursion flipped in the editor but it will be corrected in the headset.", msgType);
            }
#endif

            // Draw the default inspector properties
            DrawDefaultInspector();
        }
        private bool RenderGraphEnabled()
        {
#if UNITY_6000_0_OR_NEWER
            var renderGraphSettings = GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>();
            return !renderGraphSettings.enableRenderCompatibilityMode;
#else
            return false;
#endif
        }
    }
}
#endif