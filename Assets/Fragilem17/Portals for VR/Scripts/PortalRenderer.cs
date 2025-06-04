using UnityEngine;
using System.Collections.Generic;
using UnityEngine.XR;
using RenderPipeline = UnityEngine.Rendering.RenderPipelineManager;
using System;
using UnityEngine.Rendering.Universal;
using UnityEngine.Events;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Fragilem17.MirrorsAndPortals
{
    [ExecuteInEditMode]
    public class PortalRenderer : MonoBehaviour
    {
        public static List<PortalRenderer> portalRendererInstances;

        [Tooltip("The source material, disable and re-enable this component if you make changes to the material")]
        public List<Portal> Portals;

        [Tooltip("How many times can this surface reflect back onto the recursiveSurface.\nFrom 1 till the texturememory runs out.")]
        [MinAttribute(1)]
        public int recursions = 2;

        
        [Space(10)]
        [Header("Quality Settings")]

        [Tooltip("Uncheck useScreenScaleFactor to enter your own values.\nSquare values like 1024x1024 seem to render faster.")]
        public Vector2 textureSize = Vector2.one * 512;

        public bool useScreenScaleFactor = false;

        [Range(0.01f, 1f)]
        public float screenScaleFactor = 0.5f;

        [Space(5)]

        [Tooltip("Default and ARGB32 are generally good\nDefault HDR and ARGBHalf get rid of banding but some visual artifacts in recursions. ARGB64 is too heavy with no advantages to use it.")]
        public RenderTextureFormat _renderTextureFormat = RenderTextureFormat.Default;

        public AA antiAliasing = AA.Low;

        public bool disablePixelLights = true;


        [Tooltip("In VR this should probably always be 0")]
        [MinAttribute(0)]
        public int framesNeededToUpdate = 0;


        [Space(10)] // 10 pixels of spacing here.

        [Header("Other")]


        [Tooltip("Occlusion culling matrix is calculated in 2 ways depending on how far away from a portal you are, can speed up things and should be working now. If things start flickering, turn it off.")]
        public bool UseOcclusionCulling = true;

        [Tooltip("The layer mask of the reflection camera")]
        public LayerMask RenderTheseLayers = -1;


        [Tooltip("Off is fast")]
        public CameraOverrideOption OpaqueTextureMode = CameraOverrideOption.Off;
        [Tooltip("Off is fast")]
        public CameraOverrideOption DepthTextureMode = CameraOverrideOption.Off;

        public bool RenderShadows = false;
        [Tooltip("As most PP is on top of your entire screen, it makes no sense to render PP in the reflection, and then again on the whole screen, so keep it off unless you know why you're turning it on.")]
        public bool RenderPostProcessing = false;


        [Space(10)]
        [Header("Events")]
        public UnityEvent onStartRendering;
        public UnityEvent onFinishedRendering;


        private List<PooledPortalTexture> _pooledTextures = new List<PooledPortalTexture>();

        private static Dictionary<Camera, Camera> _reflectionCameras = new Dictionary<Camera, Camera>();
        private static Dictionary<Camera, UniversalAdditionalCameraData> _UacPerCameras = new Dictionary<Camera, UniversalAdditionalCameraData>();
        private static Dictionary<Camera, Skybox> _SkyboxPerCameras = new Dictionary<Camera, Skybox>();

        private static InputDevice _centerEye;
        private static float _IPD = 0;
        private static Vector3 _leftEyePosition;
        private static Vector3 _rightEyePosition;

        private static Camera _reflectionCamera;

        private int _frameCounter = 0;

        private RenderTextureFormat _oldRenderTextureFormat = RenderTextureFormat.DefaultHDR;
        private AA _oldAntiAliasing = AA.Low;
        private int _oldTextureSize = 0;
        private bool _oldUseScreenScaleFactor = true;
        private float _oldScreenScaleFactor = 0.5f;

        private bool _isMultipass = true;
        private bool _UacAllowXRRendering = true;

        private List<CameraPortalMatrices> cameraMatricesInOrder = new List<CameraPortalMatrices>();

        private static PortalRenderer _master;
        private UniversalAdditionalCameraData _uacRenderCam;
        private UniversalAdditionalCameraData _uacReflectionCam;

        private Skybox _skyboxRenderCam;
        private Skybox _skyboxReflectionCam;


        [Space(10)]
        [Header("Beta Features (read tooltip!)")]

        [Tooltip("When checked, the reflection will stop rendering but the materials will still update their position and blending")]
        public bool disableRenderingWhileStillUpdatingMaterials = false;

#if UNITY_2022_3_OR_NEWER
        [Tooltip("When checked, the reflection will be calculated using 'SubmitRenderRequest' instead of 'RenderSingleCamera', this allows UI to be correctly reflected at the expense of a runtime error that appears complaining about recursive rendering not supported, this error can be safely ignored.")]
        public bool UseSubmitRenderRequest = true;
#endif

        [Tooltip("When using Vulkan on Quest the second recusion would be flipped due to a bug on Unity's side, if you encounter this, check the box to flip it back.")]
        public bool FlipSecondRecursion = false;

        private Portal _p;
        private bool _frustumIntersectsWithPortal = false;
        private Vector3[] _frustumCornersLeft = new Vector3[4];
        private Vector3[] _frustumCornersRight = new Vector3[4];

        private Mesh _clippingPlaneMeshLeft;
        private GameObject _clippingPlaneLeft;
        private GameObject _clippingPlaneLeftForAR;
        private Mesh _clippingPlaneMeshRight;
        private GameObject _clippingPlaneRight;
        private GameObject _clippingPlaneRightForAR;
        private Material _clippingPlaneMaterial;
        private Material _depthMaterial;

        private Portal _closestPortal;
        private Camera _currentRenderCamera;
        private Rect _defaultViewRect = new Rect(0, 0, 1, 1);

        public bool ARCompatible = false;

#if UNITY_EDITOR_OSX
        [Tooltip("When checked, in Unity for MacOSX, the console will be spammed with a message each time a mirror renders, this is a workarround to a Unity Bug that instantly crashes the editor. (disable at your own peril)")]
        public bool enableMacOSXTemporaryLogsToAvoidCrashingTheEditor = true;
#endif

        public enum AA
        {
            None = 1,
            Low = 2,
            Medium = 4,
            High = 8
        }

        private void OnEnable()
        {
            Application.targetFrameRate = -1;

            if (portalRendererInstances == null)
            {
                portalRendererInstances = new List<PortalRenderer>();
            }
            portalRendererInstances.Add(this);

            RenderPipeline.beginCameraRendering += UpdateCamera;			
            CreateClippingPlane();
        }


        private void CreateClippingPlane()
        { 
            if (!_clippingPlaneMeshLeft)
            {
                _clippingPlaneLeft = new GameObject("Clipping Plane for Portals Left " + name, typeof(MeshRenderer), typeof(MeshFilter));
                _clippingPlaneLeft.hideFlags = HideFlags.HideAndDontSave;

                _clippingPlaneMeshLeft = new Mesh();
                Vector3[] points = new Vector3[5];
                points[0] = Vector3.zero;
                points[1] = Vector3.zero;
                points[2] = Vector3.zero;
                points[3] = Vector3.zero;
                points[4] = Vector3.zero;

                int[] triangles = { 
                    0, 1, 2, 
                    0, 3, 1, 
                    0, 4, 3
                };

                _clippingPlaneMeshLeft.vertices = points;
                _clippingPlaneMeshLeft.triangles = triangles;

                MeshFilter f = _clippingPlaneLeft.GetComponent<MeshFilter>();
                f.sharedMesh = _clippingPlaneMeshLeft;

				if (ARCompatible)
				{
                    _clippingPlaneMaterial = new Material(Shader.Find("MirrorsAndPortals/Portals/PortalSurfaceForARAlwaysOnTop")); // PortalSurfaceLiteOffsetAlwaysOnTopForAR
                }
				else
				{
                    _clippingPlaneMaterial = new Material(Shader.Find("MirrorsAndPortals/Portals/PortalSurfaceLiteOffsetAlwaysOnTop"));
				}
                _clippingPlaneMaterial.name = "materialForClippingPlane";
                _clippingPlaneMaterial.renderQueue = 3001;
                _clippingPlaneMaterial.hideFlags = HideFlags.DontSave;
                //_clippingPlaneMaterial.SetFloat("_Ztest", (float)UnityEngine.Rendering.CompareFunction.Always);


                MeshRenderer r = _clippingPlaneLeft.GetComponent<MeshRenderer>();
                r.sharedMaterial = _clippingPlaneMaterial;
                r.allowOcclusionWhenDynamic = false;


                _clippingPlaneRight = new GameObject("Clipping Plane for Portals Right " + name, typeof(MeshRenderer), typeof(MeshFilter));
                _clippingPlaneRight.hideFlags = HideFlags.HideAndDontSave;


                _clippingPlaneMeshRight = new Mesh();
                Vector3[] pointsRight = new Vector3[5];
                pointsRight[0] = Vector3.zero;
                pointsRight[1] = Vector3.zero;
                pointsRight[2] = Vector3.zero;
                pointsRight[3] = Vector3.zero;
                pointsRight[4] = Vector3.zero;

                _clippingPlaneMeshRight.vertices = pointsRight;
                _clippingPlaneMeshRight.triangles = triangles;

                f = _clippingPlaneRight.GetComponent<MeshFilter>();
                f.sharedMesh = _clippingPlaneMeshRight;

                r = _clippingPlaneRight.GetComponent<MeshRenderer>();
                r.sharedMaterial = _clippingPlaneMaterial;
                r.allowOcclusionWhenDynamic = false;

                if (ARCompatible)
                {
                    _clippingPlaneLeftForAR = new GameObject("Clipping Plane for AR Portals Left " + name, typeof(MeshRenderer), typeof(MeshFilter));
                    _clippingPlaneLeftForAR.hideFlags = HideFlags.HideAndDontSave;

                    _clippingPlaneRightForAR = new GameObject("Clipping Plane for AR Portals Right " + name, typeof(MeshRenderer), typeof(MeshFilter));
                    _clippingPlaneRightForAR.hideFlags = HideFlags.HideAndDontSave;

                    _depthMaterial = new Material(Shader.Find("MirrorsAndPortals/Portals/DepthMaskWithOffset"));
                    _depthMaterial.hideFlags = HideFlags.DontSave;

                    f = _clippingPlaneLeftForAR.GetComponent<MeshFilter>();
                    f.sharedMesh = _clippingPlaneMeshLeft;

                    r = _clippingPlaneLeftForAR.GetComponent<MeshRenderer>();
                    r.sharedMaterial = _depthMaterial;
                    r.allowOcclusionWhenDynamic = false;

                    f = _clippingPlaneRightForAR.GetComponent<MeshFilter>();
                    f.sharedMesh = _clippingPlaneMeshRight;

                    r = _clippingPlaneRightForAR.GetComponent<MeshRenderer>();
                    r.sharedMaterial = _depthMaterial;
                    r.allowOcclusionWhenDynamic = false;
                }
            }
        }

        private void LateUpdate()
        {
            if (XRSettings.enabled)
            {
                _isMultipass = XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.MultiPass;
                if (_isMultipass && _reflectionCameras.Count > 0)
                {
                    foreach (Portal portal in Portals)
                    {
                        portal.PortalSurface.UpdatePositionsInMaterial(Camera.main.transform.position, Camera.main.transform.right);
                    }
                }
            }
        }

        private void UpdateCamera(UnityEngine.Rendering.ScriptableRenderContext src, Camera renderCamera)
        {
            bool renderNeeded = false;
#if UNITY_EDITOR
            // only render the first sceneView (so we can see debug info in a second sceneView)
            //int index = Mathf.Clamp(SceneViewIndex, 0, SceneView.sceneViews.Count - 1);
            //SceneView view = SceneView.sceneViews[index] as SceneView;
            renderNeeded = renderCamera.CompareTag("MainCamera") || renderCamera.tag == "SpectatorCamera" || (renderCamera.cameraType == CameraType.SceneView && renderCamera.name.IndexOf("Preview Camera") == -1);
#else
            renderNeeded = renderCamera.CompareTag("MainCamera") || renderCamera.tag == "SpectatorCamera";
#endif

            if (!renderNeeded)
            {
                if (_master == this) { _master = null; }
                return;
            }

            if (!enabled || !renderCamera)
            {
                if (_master == this) { _master = null; }
                return;
            }

            if (Portals == null || Portals.Count == 0)
            {
                if (_master == this) { _master = null; }
                return;
            }

            if (_frameCounter > 0)
            {
                _frameCounter--;
                return;
            }
            _frameCounter = framesNeededToUpdate;

            _currentRenderCamera = renderCamera;

            if (_clippingPlaneLeft && renderCamera.cameraType != CameraType.SceneView)
            {
                // reset this cashed property
                _frustumIntersectsWithPortal = false;
                //Debug.Log("Hide clipping for: " + renderCamera.name);
                _clippingPlaneLeft.SetActive(false);
                if (_clippingPlaneRight)
                {
                    _clippingPlaneRight.SetActive(false);
                }
				if (_clippingPlaneLeftForAR)
				{
                    _clippingPlaneLeftForAR.SetActive(false);
                }
                if (_clippingPlaneRightForAR)
                {
                    _clippingPlaneRightForAR.SetActive(false);
                }
            }

            _closestPortal = null;
            float minDistance = float.MaxValue;
            if (renderCamera.cameraType != CameraType.SceneView)
            {
                for (int i = 0; i < Portals.Count; i++)
                {
                    _p = Portals[i];
                    if (_p && _p.PortalSurface && _p.isActiveAndEnabled)
                    {
                        float myDistance = Vector3.Distance(renderCamera.transform.position, _p.PortalSurface.ClosestPointOnBoundsFlattenedToPlane(renderCamera.transform.position));
                        //Debug.Log("myDistance: " + myDistance);
                        if (myDistance < minDistance)
                        {
                            minDistance = myDistance;
                            _closestPortal = _p;
                        }
                    }
                }
            }

            Portal ignoreCullingForPortal = null;
            if (minDistance < renderCamera.nearClipPlane)
            {
                //Debug.Log("minDistance: " + minDistance);
                //we're really near a portal!
                ignoreCullingForPortal = _closestPortal;
            }


            //Debug.Log("START SEARCH !! Portal Cameras on Root: " + name + " : " + renderCamera.name + " : " + Portals.Count);
            /*if (renderCamera.cameraType != CameraType.SceneView  && name == "DifferentScaledPortalsRenderer")
            {
                Debug.Log("START SEARCH !! Portal Cameras on Root: " + name + " : " + renderCamera.name + " : " + Portals.Count);
            }*/

            // check the distance          
            renderNeeded = false;
            for (int i = 0; i < Portals.Count; i++)
            {
                _p = Portals[i];
                if (_p && _p.PortalSurface && _p.isActiveAndEnabled)
                {
                    // the offset here should normally be only _IPD/2, BUT! As the physics are might not run in sync with framerate
                    // the transporter might not transport in time, to get to the other side
                    // adding a bit more space so we have an extra frame or two before we deem the camera to not be visible anymore.
                    float nearClipOffset = (_IPD/2) + renderCamera.nearClipPlane + 0.05f;
                    renderNeeded = _p.PortalSurface.VisibleFromCamera(renderCamera, false, nearClipOffset, (ignoreCullingForPortal == _p)) || renderNeeded;
                    /*if (renderNeeded)
                    {
                        Debug.Log("renderNeeded for portal " + _p.name + ", cam:" + renderCamera.name + " : "   + renderNeeded + " : " + ((_IPD / 2f) + renderCamera.nearClipPlane + 0.05f));
                    }*/
                    if (renderNeeded) {
                        break;
                    }
                }
            }            

            if (!renderNeeded)
            {
                if (_master == this) { _master = null; }
                return;
            }


            if (disableRenderingWhileStillUpdatingMaterials && cameraMatricesInOrder != null)
            {
                for (int i = 0; i < Portals.Count; i++)
                {
                    _p = Portals[i];
                    if (_p && _p.PortalSurface && _p.isActiveAndEnabled)
                    { 
                        float myDistance = Vector3.Distance(renderCamera.transform.position, _p.PortalSurface.ClosestPointOnBoundsFlattenedToPlane(renderCamera.transform.position));
                        _p.PortalSurface.UpdateMaterial(Camera.StereoscopicEye.Left, null, this, 1, myDistance);
                    }
                }
                if (_master == this) { _master = null; }
                return;
            }

            if (!_master) { _master = this; }


            CreatePortalCameras(renderCamera, out _reflectionCamera);

            _reflectionCamera.CopyFrom(renderCamera);
            _reflectionCamera.rect = _defaultViewRect;
            _reflectionCamera.cullingMask = RenderTheseLayers.value;

#if UNITY_2020_3_OR_NEWER
            GetUACData(renderCamera, out _uacRenderCam);
            GetUACData(_reflectionCamera, out _uacReflectionCam);
            if (_uacRenderCam != null)
            {
                if (renderCamera.cameraType != CameraType.SceneView && _uacRenderCam.renderType == CameraRenderType.Overlay)
                {
                    Debug.LogWarning(PortalUtils.Colorize("[PORTALS] ", PortalUtils.DebugColors.Warn, true) + renderCamera.name + " is an overlay camera, portals for overlay cameras are not supported, either remove the MainCamera or SpectatorCamera tag from this cam or make it a Base Camera.");
                    return;
                }

                _UacAllowXRRendering = _uacRenderCam.allowXRRendering;

                _uacReflectionCam.requiresColorOption = OpaqueTextureMode;
                _uacReflectionCam.requiresDepthOption = DepthTextureMode;
                _uacReflectionCam.renderPostProcessing = RenderPostProcessing;
                _uacReflectionCam.renderShadows = RenderShadows;
                //_uacReflectionCam.SetRenderer(1);
            }
            else
            {
                _UacAllowXRRendering = true;
            }
#endif

            if (XRSettings.enabled && (_master == this) && _UacAllowXRRendering)
            {
                // get the IPD
                _centerEye = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
                _centerEye.TryGetFeatureValue(CommonUsages.leftEyePosition, out _leftEyePosition);
                _centerEye.TryGetFeatureValue(CommonUsages.rightEyePosition, out _rightEyePosition);

                _IPD = Vector3.Distance(_leftEyePosition, _rightEyePosition) * renderCamera.transform.lossyScale.x;
            }

            _reflectionCamera.transform.localScale = Vector3.one * renderCamera.transform.lossyScale.x;

            if (renderCamera.cameraType != CameraType.SceneView && XRSettings.enabled && _UacAllowXRRendering)
            {
                Vector3 originalPos = renderCamera.transform.position;
                renderCamera.transform.position -= (renderCamera.transform.right * _IPD / 2f);
                _reflectionCamera.transform.SetPositionAndRotation(renderCamera.transform.position, renderCamera.transform.rotation);
                _reflectionCamera.worldToCameraMatrix = renderCamera.worldToCameraMatrix;
                renderCamera.transform.position = originalPos;

                _reflectionCamera.projectionMatrix = GetStereoProjectionMatrix(renderCamera, Camera.StereoscopicEye.Left);
            }
            else
            {
                _reflectionCamera.transform.SetPositionAndRotation(renderCamera.transform.position, renderCamera.transform.rotation);
                _reflectionCamera.worldToCameraMatrix = renderCamera.worldToCameraMatrix;
                _reflectionCamera.projectionMatrix = renderCamera.projectionMatrix;
            }


            cameraMatricesInOrder.Clear();

            onStartRendering.Invoke();


            //Dictionary<PortalSurface, Vector3> startScales = new Dictionary<PortalSurface, Vector3>();  
            
            //Debug.Log("UPDATE!! " + name + " : " + renderCamera.name);
            RecusiveFindPortalsInOrder(renderCamera, cameraMatricesInOrder, 0, 1, Camera.StereoscopicEye.Left, ignoreCullingForPortal);
            RenderPortalCamera(src, _reflectionCamera, cameraMatricesInOrder, Camera.StereoscopicEye.Left);
            //Debug.Log("END RENDER!! " + name + " : " + renderCamera.name);

            if (renderCamera.cameraType != CameraType.SceneView && XRSettings.enabled && _UacAllowXRRendering)
            {
                Vector3 originalPos = renderCamera.transform.position;
                renderCamera.transform.position += (renderCamera.transform.right * _IPD / 2f);
                _reflectionCamera.transform.SetPositionAndRotation(renderCamera.transform.position, renderCamera.transform.rotation);
                _reflectionCamera.worldToCameraMatrix = renderCamera.worldToCameraMatrix;
                renderCamera.transform.position = originalPos;

                _reflectionCamera.projectionMatrix = GetStereoProjectionMatrix(renderCamera, Camera.StereoscopicEye.Right);

                cameraMatricesInOrder.Clear();

                RecusiveFindPortalsInOrder(renderCamera, cameraMatricesInOrder, 0, 1, Camera.StereoscopicEye.Right, ignoreCullingForPortal);
                RenderPortalCamera(src, _reflectionCamera, cameraMatricesInOrder, Camera.StereoscopicEye.Right);

                for (int i = 0; i < Portals.Count; i++)
                {
                    _p = Portals[i];
                    if (_p && _p.PortalSurface && _p.isActiveAndEnabled)
                    {
                        _p.PortalSurface.TurnOffForceEye();
                    }
                }
            }
            else {
                if (_isMultipass)
                {
                    for (int i = 0; i < Portals.Count; i++)
                    {
                        _p = Portals[i];
                        if (_p && _p.PortalSurface && _p.isActiveAndEnabled)
                        {
                            _p.PortalSurface.ForceLeftEye();
                        }
                    }
                }
            }

            
            // find the closest portal and draw a clipping mesh
            if (renderCamera.cameraType != CameraType.SceneView)
            {
                if (_closestPortal != null && _clippingPlaneLeft)
                {
                    //Debug.Log("closestPortal: " + closestPortal);
                    //closestPortal.PortalSurface.CalcOffset();

                    Camera.MonoOrStereoscopicEye eye = Camera.MonoOrStereoscopicEye.Mono;
                    if (XRSettings.enabled && _UacAllowXRRendering)
                    {
                        eye = Camera.MonoOrStereoscopicEye.Left;
                    }

                    bool leftMeshDrawn = DrawClippingMesh(renderCamera, _closestPortal, eye);
                    bool rightMeshDrawn = false;
                    if (XRSettings.enabled && _UacAllowXRRendering)
                    {
                        rightMeshDrawn = DrawClippingMesh(renderCamera, _closestPortal, Camera.MonoOrStereoscopicEye.Right);
                    }

                    //Debug.Log("CLOSEST PORTAL " + closestPortal.name + " : "+ leftMeshDrawn);

                    if (_clippingPlaneMaterial && (leftMeshDrawn || rightMeshDrawn))
                    {
                        _clippingPlaneMaterial.SetColor("_FadeColor", _closestPortal.PortalSurface.BlendColor);

                        _clippingPlaneMaterial.SetFloat("_FadeColorBlend", _closestPortal.PortalSurface._currentDistanceBlend);
                        _clippingPlaneMaterial.SetTexture("_TexLeft", _closestPortal.PortalSurface._currentTexLeft);
                        if (XRSettings.enabled && _UacAllowXRRendering)
                        {
                            _clippingPlaneMaterial.SetTexture("_TexRight", _closestPortal.PortalSurface._currentTexRight);
                            _clippingPlaneMaterial.SetInt("_ForceEye", -1);
                        }
                        //Debug.Break();
                    }

                    if (!leftMeshDrawn)
                    {
                        //Debug.Log("Hide clipping 2 for: " + renderCamera.name);
                        _clippingPlaneLeft.SetActive(false);
						if (_clippingPlaneLeftForAR)
						{
                            _clippingPlaneLeftForAR.SetActive(false);
						}
                    }
                    if (!rightMeshDrawn)
                    {
                        _clippingPlaneRight.SetActive(false);
                        if (_clippingPlaneRightForAR)
                        {
                            _clippingPlaneRightForAR.SetActive(false);
                        }
                    }
                }
            }
            
            onFinishedRendering.Invoke();
        }

        private bool FrustumIntersectsWithPortal(Camera renderCamera, Portal portal, ref Vector3[] frustumCorners, Camera.MonoOrStereoscopicEye eye, bool deep = false)
        {
            //Debug.Log("FrustumIntersectsWithPortal? 1");
            if (!portal.PortalTransporter || !portal.PortalTransporter.MyCollider || portal.OtherPortal == null)
            {
                return false;
            }

            if (deep)
            {
                eye = Camera.MonoOrStereoscopicEye.Mono;
            }

            Plane p = new Plane(-portal.PortalSurface.transform.forward, portal.PortalSurface.transform.position);
            Vector3 closestPointOnPlane = p.ClosestPointOnPlane(renderCamera.transform.position);
            Vector3 dir = (closestPointOnPlane - renderCamera.transform.position);
            float facing = Vector3.Dot(dir, portal.PortalSurface.transform.forward);

            if (facing < -((_IPD/2) + renderCamera.nearClipPlane + 0.05f)) // + 0.05f
            {
                return false;
            }
            /*
            if (deep && (facing < -((_IPD / 2) + renderCamera.nearClipPlane + 0.0f))) // + 0.05f
            {
                return false;
            }
            */
            //Debug.Log("FrustumIntersectsWithPortal? " + renderCamera.name +" : "  + facing);
            // calculate the depth needed, NearClipping field and IPD
            Bounds frustumSquare;

            Vector3 worldSpaceCornerPos;

            float originalNearClipPlane = renderCamera.nearClipPlane;
            float scaleFactor = 1;
            if (portal.OtherPortal)
            {
                scaleFactor = Math.Max((portal.transform.lossyScale.z / portal.OtherPortal.transform.lossyScale.z), (portal.OtherPortal.transform.lossyScale.z / portal.transform.lossyScale.z));
            }

            //scaleFactor = Math.Max(1, scaleFactor);
            //Debug.Log("scaleFactor for: " + portal.name + " : " + scaleFactor);

            renderCamera.nearClipPlane = (originalNearClipPlane * scaleFactor);
            renderCamera.nearClipPlane += 0.01f;
            /*if (deep)
            {
                renderCamera.nearClipPlane += 0.01f;
            }*/

            Vector3 originalPos = renderCamera.transform.position;
            if (!deep && XRSettings.enabled && _UacAllowXRRendering)
            {
                if (eye == Camera.MonoOrStereoscopicEye.Left)
                {
                    renderCamera.transform.position -= (renderCamera.transform.right * _IPD / 2f);
                }
                else
                {
                    renderCamera.transform.position += (renderCamera.transform.right * _IPD / 2f);
                }
            }


            renderCamera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), (_currentRenderCamera.nearClipPlane) / _currentRenderCamera.transform.lossyScale.x, eye, frustumCorners);
            // make the frustumCorners worldSpace
            for (int i = 0; i < 4; i++)
            {
                frustumCorners[i] = renderCamera.transform.TransformPoint(frustumCorners[i]);
            }

            frustumSquare = new Bounds(frustumCorners[0], Vector3.one * 0.05f);
            for (int i = 1; i < 4; i++)
            {
                worldSpaceCornerPos = frustumCorners[i];
                frustumSquare.Encapsulate(new Bounds(worldSpaceCornerPos, Vector3.one * 0.05f));
            }
            // 10 cm from the back of your head, if you're moving backwards
            frustumSquare.Encapsulate(new Bounds(renderCamera.transform.position, Vector3.one * 0.2f)); 

            renderCamera.transform.position = originalPos;

            renderCamera.nearClipPlane = originalNearClipPlane;

            _frustumIntersectsWithPortal = (portal.PortalTransporter.MyCollider && portal.PortalTransporter.MyCollider.bounds.Intersects(frustumSquare));
            //Debug.Log("FrustumIntersectsWithPortal? 3 " + renderCamera.name + " : " + portal.name + " : " + _frustumIntersectsWithPortal);
            return _frustumIntersectsWithPortal;
        }


        private bool DrawClippingMesh(Camera renderCamera, Portal portal, Camera.MonoOrStereoscopicEye eye)
        {
            Vector3[] frustumCorners = _frustumCornersLeft;
            if (eye == Camera.MonoOrStereoscopicEye.Right)
            {
                frustumCorners = _frustumCornersRight;
            }
            //Debug.Log("DrawClippingMesh for " + renderCamera.name);

            

            if (FrustumIntersectsWithPortal(renderCamera, portal, ref frustumCorners, eye))
            {
                GameObject clippingPlane = _clippingPlaneLeft;
                GameObject clippingPlaneMask = _clippingPlaneLeftForAR;
                Mesh clippingPlaneMesh = _clippingPlaneMeshLeft;
                if (eye == Camera.MonoOrStereoscopicEye.Right)
                {
                    clippingPlane = _clippingPlaneRight;
                    clippingPlaneMesh = _clippingPlaneMeshRight;
                    clippingPlaneMask = _clippingPlaneRightForAR;
                }

                //Debug.Log("show clipping plane: " + renderCamera.name);
                clippingPlane.SetActive(true);
				if (clippingPlaneMask)
				{
                    clippingPlaneMask.SetActive(true);                    
				}

                Plane p = new Plane(-portal.PortalSurface.transform.forward, portal.PortalSurface.transform.position);
                Vector3 worldSpaceCornerPos;

                Vector3 closestPointOnPlane;
                Vector3 dir;
                float facing;
                Vector3[] vertexes = new Vector3[5];

                float cornerToCornerDistance;
                Vector3 intersectionPoint = Vector3.zero;

                // find the first corner that intersects
                int vertexIndex = 0;
                for (int i = 0; i < 4; i++)
                {
                    worldSpaceCornerPos = frustumCorners[i];

                    //Debug.Log(closestPortal.PortalTransporter.MyCollider.bounds);
                    closestPointOnPlane = p.ClosestPointOnPlane(worldSpaceCornerPos);
                    dir = (closestPointOnPlane - worldSpaceCornerPos);
                    facing = Vector3.Dot(dir, portal.PortalSurface.transform.forward);
                    //Debug.Log(renderCamera.name + " dir: " + dir.magnitude + " : " + facing +" : "+ reason);
                    if (facing < 0)
                    {
                        // this corner lies behind the plane, thus is a corner of our mesh
                        vertexes[vertexIndex] = worldSpaceCornerPos;
                        vertexIndex++;
                    }
                    else
                    { 
                        // this corner is in front, so it's possibly making 2 corners of our mesh

                        // we intersect with the portal, find the intersection point towards the next corner
                        int prevI = i - 1;
                        prevI = prevI == -1 ? 3 : prevI;

                        int nextI = i + 1;
                        nextI = nextI == 4 ? 0 : nextI;

                        Vector3 prevCornerPos = frustumCorners[prevI];
                        Vector3 nextCornerPos = frustumCorners[nextI];
                        float enterPointDistance;
                        Ray rayFromCornerToCorner = new Ray(worldSpaceCornerPos, prevCornerPos - worldSpaceCornerPos);

                        //Debug.DrawRay(worldSpaceCornerPos, rayFromCornerToCorner.direction, Color.magenta, 0.3f);

                        if (p.Raycast(rayFromCornerToCorner, out enterPointDistance))
                        {
                            cornerToCornerDistance = Vector3.Distance(worldSpaceCornerPos, prevCornerPos);

                            //enterPointDistance = Mathf.Min(enterPointDistance, cornerToCornerDistance);

                            if (enterPointDistance < cornerToCornerDistance)
                            {
                                //Debug.Log("prevPoint intersection: " + i + " : " + cornerToCornerDistance + " : " + enterPointDistance);
                                intersectionPoint = rayFromCornerToCorner.origin + (rayFromCornerToCorner.direction * (enterPointDistance + 0.000f));
                                vertexes[vertexIndex] = intersectionPoint;
                                vertexIndex++;
                                //Debug.DrawLine(worldSpaceCornerPos, intersectionPoint, Color.yellow, 0.2f);
                            }
                            //Debug.Log("found1: " + enterPointDistance);
                        }

                        rayFromCornerToCorner = new Ray(worldSpaceCornerPos, nextCornerPos - worldSpaceCornerPos);

                        //Debug.DrawRay(worldSpaceCornerPos, rayFromCornerToCorner.direction, Color.magenta, 0.3f);

                        if (p.Raycast(rayFromCornerToCorner, out enterPointDistance))
                        {
                            cornerToCornerDistance = Vector3.Distance(worldSpaceCornerPos, nextCornerPos);
                            //enterPointDistance = Mathf.Min(enterPointDistance, cornerToCornerDistance);
                            if (enterPointDistance < cornerToCornerDistance)
                            {
                                //Debug.Log("nextPoint intersection: " + i + " : " + cornerToCornerDistance + " : " + enterPointDistance);
                                intersectionPoint = rayFromCornerToCorner.origin + (rayFromCornerToCorner.direction * (enterPointDistance + 0.000f));
                                vertexes[vertexIndex] = intersectionPoint;
                                vertexIndex++;
                                //Debug.DrawLine(worldSpaceCornerPos, intersectionPoint, Color.green, 0.2f);
                            }
                            //Debug.Log("found2: " + enterPointDistance);
                        }
                    }
                }

                if (clippingPlane && vertexIndex >= 3)
                {
                    //Vector3[] v = new Vector3[vertexes.Count];
                    //vertexes.CopyTo(v);
                    clippingPlane.transform.SetPositionAndRotation(renderCamera.transform.position + (renderCamera.transform.forward * (renderCamera.nearClipPlane + 0.01f)), renderCamera.transform.rotation);

                    Vector3[] vertices = clippingPlaneMesh.vertices;
                    //Debug.Log("Vertexes:" + vertexes.Length + " : " + vertexIndex);

                    Vector3 localSpace = clippingPlane.transform.InverseTransformPoint(vertexes[0]);
                    vertices[1] = localSpace;

                    localSpace = clippingPlane.transform.InverseTransformPoint(vertexes[1]);
                    vertices[2] = localSpace;

                    localSpace = clippingPlane.transform.InverseTransformPoint(vertexes[2]);
                    vertices[0] = localSpace;

                    if (vertexIndex == 3)
                    {
                        vertices[3] = localSpace;
                        vertices[4] = localSpace;
                    }

                    if (vertexIndex == 4)
                    {
                        localSpace = clippingPlane.transform.InverseTransformPoint(vertexes[3]);
                        vertices[3] = localSpace;
                        vertices[4] = localSpace;
                    }

                    if (vertexIndex >= 5)
                    {
                        localSpace = clippingPlane.transform.InverseTransformPoint(vertexes[4]);
                        vertices[3] = localSpace;
                        localSpace = clippingPlane.transform.InverseTransformPoint(vertexes[3]);
                        vertices[4] = localSpace;
                    }


                    clippingPlaneMesh.vertices = vertices;
					//clippingPlaneMesh.RecalculateBounds();

					if (ARCompatible)
					{
                        clippingPlaneMask.transform.SetPositionAndRotation(clippingPlane.transform.position, clippingPlane.transform.rotation);
                    }

                    return true;
                }
            }

            return false;
        }


        private float FindNearClippingOffset(Camera renderCamera, Portal portal, string reason)
        {
            Camera.MonoOrStereoscopicEye eye = Camera.MonoOrStereoscopicEye.Mono;
            if (XRSettings.enabled && _UacAllowXRRendering)
            {
                eye = Camera.MonoOrStereoscopicEye.Left;
            }
            bool intersectsLeft = FrustumIntersectsWithPortal(renderCamera, portal, ref _frustumCornersLeft, eye);
            bool intersectsRight = false;
            if (XRSettings.enabled && _UacAllowXRRendering)
            {
                intersectsRight = FrustumIntersectsWithPortal(renderCamera, portal, ref _frustumCornersRight, Camera.MonoOrStereoscopicEye.Right);
            }


            if (intersectsLeft || intersectsRight) { 
             
                Plane p = new Plane(-portal.PortalSurface.transform.forward, portal.PortalSurface.transform.position);
                Vector3 worldSpaceCornerPos;

                //float d = Vector3.Distance(renderCamera.transform.position, _frustumCornersLeft[0]);

                Vector3 closestPointOnPlane;
                Vector3 dir;
                float facing;
                float maxDist = 0f;

                for (int i = 0; i < 4; i++)
                {
                    worldSpaceCornerPos = _frustumCornersLeft[i];

                    //Debug.Log(closestPortal.PortalTransporter.MyCollider.bounds);
                    closestPointOnPlane = p.ClosestPointOnPlane(worldSpaceCornerPos);
                    dir = (closestPointOnPlane - worldSpaceCornerPos);
                    facing = Vector3.Dot(dir, portal.PortalSurface.transform.forward);
                    //Debug.Log(renderCamera.name + " dir: " + dir.magnitude + " : " + facing +" : "+ reason);
                    if (facing < 0)
                    {
                        //Debug.DrawRay(worldSpaceCornerPos, dir, Color.red, 0.2f, false);
                        maxDist = Mathf.Max(maxDist, dir.magnitude);
                    }
                    else
                    {
                        //Debug.DrawRay(worldSpaceCornerPos, dir, Color.blue, 0.2f, true);
                    }
                }

                if (XRSettings.enabled && _UacAllowXRRendering)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        worldSpaceCornerPos = _frustumCornersRight[i];
                        closestPointOnPlane = p.ClosestPointOnPlane(worldSpaceCornerPos);

                        dir = (closestPointOnPlane - worldSpaceCornerPos);
                        facing = Vector3.Dot(dir, portal.PortalSurface.transform.forward);
                        //Debug.DrawRay(worldSpaceCornerPos, dir, Color.blue, 0.2f, true);
                        //Debug.Log(renderCamera.name + " dir: " + dir.magnitude + " : " + facing);
                        if (facing < 0)
                        {
                            maxDist = Mathf.Max(maxDist, dir.magnitude);
                        }
                    }
                }

                return maxDist;
            }

            return 0;
        }

        private void RecusiveFindPortalsInOrder(Camera renderCamera, List<CameraPortalMatrices> cameraPortalMatricesInOrder,
            float previousDistance, int depth, Camera.StereoscopicEye eye, Portal ignoreCullingForPortal,
            PortalSurface parentSurface = null,
            PortalSurface parentsParentSurface = null,
            PortalSurface parentsParentsParentSurface = null,
            PortalSurface parentsParentsParentsParentSurface = null)
        {
            _reflectionCamera.ResetWorldToCameraMatrix();



            // look one deeper to know which deepest mirrors to turn dark
            if (depth > recursions + 1)
            {
                return;
            }

            float originalNearClip = _reflectionCamera.nearClipPlane;
            Vector3 eyePosition = _reflectionCamera.transform.position;
            Quaternion eyeRotation = _reflectionCamera.transform.rotation;
            Matrix4x4 projectionMatrix = _reflectionCamera.projectionMatrix;

            if (renderCamera.cameraType != CameraType.SceneView && XRSettings.enabled && _UacAllowXRRendering)
            {
                projectionMatrix = GetStereoProjectionMatrix(_reflectionCamera, eye);
            }

            //Vector3[] occlusionBoundsLocal = new Vector3[4];
            float myDistance;
            PortalSurface portalSurface;
            for (int i = 0; i < Portals.Count; i++)
            {
                _p = Portals[i];

                if (_p && _p.PortalSurface && _p.isActiveAndEnabled)
                {
                    portalSurface = _p.PortalSurface;
                    if (portalSurface != null && _p.OtherPortal != null)
                    {
                        float nearClipOffset = 0;
                        if (depth == 1)
                        {
                            nearClipOffset = FindNearClippingOffset(_reflectionCamera, _p, "recusiveFind"); // (_IPD/2) +_reflectionCamera.nearClipPlane + 0.05f; //
                        }
                        //Debug.Log("gonna test VisibleFromCamera " + _p.name + " : " + depth + " : "+ _reflectionCamera.name);
                        if (portalSurface.VisibleFromCamera(_reflectionCamera, true, nearClipOffset, (ignoreCullingForPortal == _p)))
                        {
                            Vector3 closestPointOnBoundsFromRenderCamera = portalSurface.ClosestPointOnBoundsFlattenedToPlane(eyePosition);
                            float distanceToSurface = Vector3.Distance(eyePosition, closestPointOnBoundsFromRenderCamera);
                            myDistance = previousDistance + distanceToSurface;
                            //Debug.Log(_p.name +" : "+ depth + " : " +nearClipOffset + " previousDistance: " + previousDistance + " myDistance: " + myDistance);

                            if (myDistance <= portalSurface.maxRenderingDistance)
                            {
                                //myDistance = previousDistance + Vector3.Distance(eyePosition, portalSurface.ClosestPointOnPlane(eyePosition));
                                if (!portalSurface.Portal || !portalSurface.Portal.OtherPortal || !portalSurface.Portal.OtherPortal.PortalSurface)
                                {
                                    break;
                                }

                                Transform inTransform = portalSurface.transform;
                                Transform outTransform = portalSurface.Portal.OtherPortal.PortalSurface.transform;

                                Transform reflectionCameraTransform = _reflectionCamera.transform;

                                Vector3[] occlusionBounds = portalSurface.Portal.PortalSurface.ShrinkPointsToBounds(_reflectionCamera, 0, (renderCamera.cameraType != CameraType.SceneView));
                                

                                for (int x = 0; x < 4; x++)
                                {
								    /*if (renderCamera.cameraType != CameraType.SceneView)
                                    {
                                        Vector3 direction = (occlusionBounds[x] - reflectionCameraTransform.position).normalized;
                                        direction.Scale(Vector3.one * 100);
                                        Debug.DrawRay(reflectionCameraTransform.position, direction);
                                        DebugExtension.DebugWireSphere(occlusionBounds[x], Color.red, 0.05f, 0, false);
                                    }*/

                                    // make points local space arrording to relfectionCam
                                    occlusionBounds[x] = reflectionCameraTransform.InverseTransformPoint(occlusionBounds[x]);
								}
                                

                                reflectionCameraTransform.position = eyePosition;
                                reflectionCameraTransform.rotation = eyeRotation;

                                // Position the camera behind the other portal.
                                Vector3 relativePos = inTransform.InverseTransformPoint(reflectionCameraTransform.position);
                                relativePos = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativePos;
                                reflectionCameraTransform.position = outTransform.TransformPoint(relativePos);

                                // Rotate the camera to look through the other portal.
                                Quaternion relativeRot = Quaternion.Inverse(inTransform.rotation) * reflectionCameraTransform.rotation;
                                relativeRot = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativeRot;
                                reflectionCameraTransform.rotation = outTransform.rotation * relativeRot;


                                float scaleFactor = (outTransform.lossyScale.x / inTransform.lossyScale.x);
                                reflectionCameraTransform.localScale = reflectionCameraTransform.localScale * scaleFactor;

                                _reflectionCamera.transform.SetPositionAndRotation(reflectionCameraTransform.position, reflectionCameraTransform.rotation);
                                _reflectionCamera.transform.localScale = reflectionCameraTransform.localScale;

                                //_reflectionCamera.transform.position = reflectionCameraTransform.position;
                                //_reflectionCamera.transform.rotation = reflectionCameraTransform.rotation;
                                Quaternion newRot = reflectionCameraTransform.rotation;
                                Vector3 newPos = reflectionCameraTransform.position;

                                for (int x = 0; x < 4; x++)
                                {
                                    occlusionBounds[x] = _reflectionCamera.transform.TransformPoint(occlusionBounds[x]);
                                    /*if (renderCamera.cameraType != CameraType.SceneView)
                                    { 
                                        DebugExtension.DebugWireSphere(occlusionBounds[x], Color.green, 0.05f, 0, false);
                                    }*/
                                }


                                // calculate the direction to the plane
                                Plane plane = new Plane(-outTransform.forward, outTransform.position); // + (outTransform.forward * -0.01f)
                                Vector3 closestPointOnPlane = plane.ClosestPointOnPlane(_reflectionCamera.transform.position);
                                Vector3 dirToPlane = _reflectionCamera.transform.position - closestPointOnPlane;
                                float sideOfTheMirrorWithCenterEye = Vector3.Dot(-1 * outTransform.forward, dirToPlane);

                             

                                Vector3 minDistPoint = ClosestPointOnRectangle(occlusionBounds, _reflectionCamera.transform.position);
                                float minDistance = Vector3.Distance(_reflectionCamera.transform.position, minDistPoint);
                                
                                /*if (renderCamera.cameraType != CameraType.SceneView)
                                {
                                    Debug.DrawLine(_reflectionCamera.transform.position, minDistPoint, Color.red);
                                }*/

                                // calc the angle with the reflection forward
                                float angle = Vector3.Angle(newPos - (newPos + reflectionCameraTransform.forward), newPos - minDistPoint);
                                minDistance = (minDistance * Mathf.Cos((angle * Mathf.PI) / 180));
                                minDistance -= 0.01f;
                                minDistance = MathF.Max(minDistance + portalSurface.clippingPlaneOffset, renderCamera.nearClipPlane*0.8f);

                                /*if (renderCamera.cameraType != CameraType.SceneView)
                                {
                                    Debug.DrawLine(newPos, newPos + (reflectionCameraTransform.forward * minDistance), Color.red, 0.05f);                                    
                                    Debug.DrawLine(newPos, minDistPoint, Color.green, 0.05f);
                                }*/


                                float newNearClip = minDistance;
                                float newFarClip = renderCamera.farClipPlane;

                                _reflectionCamera.nearClipPlane = minDistance;


                                Matrix4x4 newProjectionMatrix = _reflectionCamera.projectionMatrix;
                                if (renderCamera.cameraType != CameraType.SceneView && XRSettings.enabled && _UacAllowXRRendering)
                                {
                                    newProjectionMatrix = GetStereoProjectionMatrix(_reflectionCamera, eye);
                                }



                                // very weird, the near plane is not kept when getting GetStereoProjectionMatrix in VR
                                // fixing the projection matrix
                                newProjectionMatrix.m22 = (renderCamera.farClipPlane + newNearClip) / (newNearClip - renderCamera.farClipPlane);
                                newProjectionMatrix.m23 = (2 * renderCamera.farClipPlane * newNearClip) / (newNearClip - renderCamera.farClipPlane);



                                if (_p.PortalSurface.requireObliqueProjectionMatrix)
                                {

                                    // only make the projectionMatrix oblique when we're still in front of the mirror with the reflectionCam
                                    // makes no sense to do it behind the portal, also stay atleast the camera's nearplane away!
                                    if (sideOfTheMirrorWithCenterEye < -(renderCamera.nearClipPlane + _p.PortalSurface.nearDistanceToStartDisablingObliquePM))
                                    {
                                        // Set the camera's oblique view frustum.
                                        //Debug.Log("p.distance=: " + (p.distance - portalSurface.clippingPlaneOffset));
                                        Vector4 clipPlaneWorldSpace = new Vector4(plane.normal.x, plane.normal.y, plane.normal.z, plane.distance - portalSurface.clippingPlaneOffset);
                                        Vector4 clipPlaneCameraSpace =
                                            Matrix4x4.Transpose(Matrix4x4.Inverse(_reflectionCamera.worldToCameraMatrix)) * clipPlaneWorldSpace;

                                        PortalUtils.MakeProjectionMatrixOblique(ref newProjectionMatrix, clipPlaneCameraSpace);
                                    }
                                }


                                Matrix4x4 newWorldToCameraMatrix = _reflectionCamera.worldToCameraMatrix;

                                Matrix4x4 newCullingMatrix = Matrix4x4.identity;
                               
                                //Debug.Log(sideOfTheMirrorWithCenterEye);
                                if (UseOcclusionCulling  && - sideOfTheMirrorWithCenterEye > 0.01f)  //  && _reflectionCamera.fieldOfView > fov && fov != 0
                                {
                                    // bottomLeft / bottomRight / topLeft
                                    newCullingMatrix = PortalUtils.OffAxisProjectionMatrix(newNearClip, renderCamera.farClipPlane, occlusionBounds[0], occlusionBounds[1], occlusionBounds[2], newPos);
                                }
                                else
                                {
                                    // calc a normal culling matrix 
                                    Matrix4x4 PMMatrixForCulling = _reflectionCamera.projectionMatrix;
                                        
                                    if (renderCamera.cameraType != CameraType.SceneView && XRSettings.enabled && _UacAllowXRRendering)
                                    {
                                        PMMatrixForCulling = GetStereoProjectionMatrix(_reflectionCamera, eye);
                                    }

                                    PMMatrixForCulling.m22 = (renderCamera.farClipPlane + renderCamera.nearClipPlane) / (renderCamera.nearClipPlane - renderCamera.farClipPlane);
                                    PMMatrixForCulling.m23 = (2 * renderCamera.farClipPlane * renderCamera.nearClipPlane) / (renderCamera.nearClipPlane - renderCamera.farClipPlane);

                                    newCullingMatrix = PMMatrixForCulling * _reflectionCamera.worldToCameraMatrix;
                                }

                                //Debug.Log(newPos + "hFov: " + hFov + " vFov: " + vFov + " : "+ _reflectionCamera.fieldOfView + " : " + fov);
                           



                                //Debug.Log("Search Mirror Cameras seen by " + reflectionMs.name + " : " + depth);
                                RecusiveFindPortalsInOrder(renderCamera, cameraMatricesInOrder, myDistance, depth + 1, eye, ignoreCullingForPortal ,portalSurface, parentSurface, parentsParentSurface, parentsParentsParentSurface);


                                _reflectionCamera.nearClipPlane = originalNearClip;

                                // we might have moved the reflection camera in a previous iteration
                                // reset it for the next 
                                _reflectionCamera.transform.position = eyePosition;
                                _reflectionCamera.transform.rotation = eyeRotation;
                                //_reflectionCamera.worldToCameraMatrix = worldToCameraMatrix;
                                _reflectionCamera.projectionMatrix = projectionMatrix;

                                // if we're at depth.. then only add a real cameraMatricesInOrder if the parentSurface is ours. We don't want other stuff


                                //Debug.Log("Found Mirror: " + portalSurface.Portal.name + " depth: " + depth + " parent: " + parentSurface?.Portal.name +" : "+ nearClipOffset +" : " + myDistance);
                                cameraMatricesInOrder.Add(new CameraPortalMatrices(newProjectionMatrix, newWorldToCameraMatrix, newCullingMatrix, portalSurface, depth % 2 != 0, newPos, newRot, newNearClip, newFarClip, depth, myDistance, parentSurface, parentsParentSurface, parentsParentsParentSurface, parentsParentsParentsParentSurface));
                            }
                            else
                            {
                                //Debug.Log("to far: " + portalSurface.Portal.name + " : " + depth + " : " + myDistance + " : " + portalSurface.maxRenderingDistance);
                                cameraMatricesInOrder.Add(new CameraPortalMatrices(Matrix4x4.identity, Matrix4x4.identity, Matrix4x4.identity, portalSurface, true, Vector3.zero, Quaternion.identity, 0.1f, 10f, recursions + 1, myDistance, parentSurface, parentsParentSurface, parentsParentsParentSurface, parentsParentsParentsParentSurface));
                            }
                        }
                    }
                }
            }

            //Debug.Log("stop search: " + depth + " : " + parentSurface?.name);
        }

        public static Vector3 NearestPointOnLine(Vector3 linePnt, Vector3 lineDir, Vector3 pnt)
        {
            lineDir.Normalize();//this needs to be a unit vector
            var v = pnt - linePnt;
            var d = Vector3.Dot(v, lineDir);
            return linePnt + lineDir * d;
        }

        public Vector3 ClosestPointOnRectangle(Vector3[] rectanglePoints, Vector3 pointToCheck)
        {
            Vector3 A = rectanglePoints[0];
            Vector3 B = rectanglePoints[1];
            Vector3 C = rectanglePoints[3];
            Vector3 D = rectanglePoints[2];

            Vector3 AB = B - A;
            Vector3 AD = D - A;
            Vector3 planeNormal = Vector3.Cross(AB, AD).normalized;


            Plane plane = new Plane(A, B, C);
            //Debug.DrawRay(A, plane.normal, Color.cyan);

            Vector3 closestPointOnPlane = plane.ClosestPointOnPlane(pointToCheck);
            

            Vector3 AP = closestPointOnPlane - A;
            float dotABAP = Vector3.Dot(AB, AP);
            float dotADAP = Vector3.Dot(AD, AP);

            if (dotABAP >= 0f && dotABAP <= Vector3.Dot(AB, AB) && dotADAP >= 0f && dotADAP <= Vector3.Dot(AD, AD))
            {
                return closestPointOnPlane;
            }
            else
            {
                // find the closest point on the edges of the rectangle
                Vector3[] edgePoints = new Vector3[] { A, B, C, D };
                Vector3 closestEdgePoint = edgePoints[0];
                float closestDistance = float.MaxValue;

                for (int i = 0; i < 4; i++)
                {
                    Vector3 edgeStart = edgePoints[i];
                    Vector3 edgeEnd = edgePoints[(i + 1) % 4];
                    Vector3 edgeDirection = (edgeEnd - edgeStart).normalized;

                    Vector3 nearestPointOnLine = NearestPointOnLine(edgeStart, edgeDirection, pointToCheck);

                    float distanceToEdge = Vector3.Distance(nearestPointOnLine, pointToCheck);
                    if (distanceToEdge < closestDistance)
                    {
                        closestDistance = distanceToEdge;
                        closestEdgePoint = nearestPointOnLine;
                    }
                }

                return closestEdgePoint;
            }
        }

        /**
         * override this method to support canted high fov headsets like a Pimax
         */
        protected Matrix4x4 GetStereoProjectionMatrix(Camera m_Camera, Camera.StereoscopicEye eye) {

            Matrix4x4 projectionMatrix = m_Camera.GetStereoProjectionMatrix(eye);

            /*  uncomment the below lines to support Pimax and other canted high fov screens until unity gives the correct Matrix back by itselff
            // need to find OpenXR alternatives to these methods to fix for canted high FOV HMDs and get rig of SteamVR requirement
            bool isMultipass = (XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.MultiPass);
            m_Camera = GetComponent<Camera>();
            HmdMatrix34_t eyeToHeadL = SteamVR.instance.hmd.GetEyeToHeadTransform(EVREye.Eye_Left);
            if (eyeToHeadL.m0 < 1)  //m0 = 1 for parallel projections
            {
                float l_left = 0.0f, l_right = 0.0f, l_top = 0.0f, l_bottom = 0.0f;
                SteamVR.instance.hmd.GetProjectionRaw(EVREye.Eye_Left, ref l_left, ref l_right, ref l_top, ref l_bottom);
                float eyeYawAngle = Mathf.Acos(eyeToHeadL.m0);  //since there are no x or z rotations, this is y only. 10 deg on Pimax
                if (isMultipass) eyeYawAngle *= 2;  //for multipass left eye frustum is used twice? causing right eye to end up 20 deg short
                float eyeHalfFov = Mathf.Atan(SteamVR.instance.tanHalfFov.x);
                float tanCorrectedEyeHalfFovH = Mathf.Tan(eyeYawAngle + eyeHalfFov);

                //increase horizontal fov by the eye rotation angles
                projectionMatrix.m00 = 1 / tanCorrectedEyeHalfFovH;  //m00 = 0.1737 for Pimax

                //because of canting, vertical fov increases towards the corners. calculate the new maximum fov otherwise culling happens too early at corners
                float eyeFovLeft = Mathf.Atan(-l_left);
                float tanCorrectedEyeHalfFovV = SteamVR.instance.tanHalfFov.y * Mathf.Cos(eyeFovLeft) / Mathf.Cos(eyeFovLeft + eyeYawAngle);
                projectionMatrix.m11 = 1 / tanCorrectedEyeHalfFovV;   //m11 = 0.3969 for Pimax

                //set the near and far clip planes
                projectionMatrix.m22 = -(m_Camera.farClipPlane + m_Camera.nearClipPlane) / (m_Camera.farClipPlane - m_Camera.nearClipPlane);
                projectionMatrix.m23 = -2 * m_Camera.farClipPlane * m_Camera.nearClipPlane / (m_Camera.farClipPlane - m_Camera.nearClipPlane);
                projectionMatrix.m32 = -1;
                print("REturning new matrix");
            }
            */

            return projectionMatrix;
        }

        private void RenderPortalCamera(UnityEngine.Rendering.ScriptableRenderContext src, Camera reflectionCamera, List<CameraPortalMatrices> cameraMatricesInOrder, Camera.StereoscopicEye eye)
        {
            //Debug.Log("RenderMirrorCamera: " + reflectionCamera.name);

            // Optionally disable pixel lights for reflection
            int oldPixelLightCount = QualitySettings.pixelLightCount;
            if (disablePixelLights)
            {
                QualitySettings.pixelLightCount = 0;
            }

            PooledPortalTexture _ptex = null;
            // position and render the camera
            CameraPortalMatrices matrices = null;

            PooledPortalTexture previousTex = null;
            

            for (int i = 0; i < cameraMatricesInOrder.Count; i++)
            {
                matrices = cameraMatricesInOrder[i];

                if (matrices.depth >= recursions + 1)
                {
                    //Debug.Log(" render surface lite: " + matrices.mirrorSurface.name + " de: " + matrices.depth + " pa: " + matrices.parentMirrorSurface?.name + " di: " + matrices.distance);
                    // make it completely blended, no need to render these
                    matrices.mirrorSurface.UpdateMaterial(eye, null, this, matrices.depth, Mathf.Infinity);
                }
                else
                {
                    GetFreeTexture(out _ptex, eye);
                    _ptex.matrices = matrices;

                    //Debug.Log(" depth: " + matrices.depth + " render op: " + matrices.mirrorSurface.name + " VOOR parent: " + matrices.parentMirrorSurface?.name + " using tex: " + _ptex.texture.name + " parentsParent: "+ matrices.parentsParentMirrorSurface);

                    _ptex.liteLock = true;

                    if (matrices.parentMirrorSurface == null)
                    {
                        _pooledTextures.ForEach(pTex => {
                            pTex.liteLock = false;
                        });
                        _ptex.fullLock = true;
                    }



                    reflectionCamera.targetTexture = _ptex.texture;
                    reflectionCamera.worldToCameraMatrix = matrices.worldToCameraMatrix;
                    reflectionCamera.projectionMatrix = matrices.projectionMatrix;
                    
                    reflectionCamera.transform.position = matrices.camPos;
                    reflectionCamera.transform.rotation = matrices.camRot;

                    //Debug.Log(" render surface heav: " + matrices.mirrorSurface.name + " de : " + matrices.depth + " pa: " + matrices.parentMirrorSurface?.name + " di: " + matrices.distance + " tex: " + _ptex.texture.name);

                    reflectionCamera.useOcclusionCulling = UseOcclusionCulling;
                    reflectionCamera.cullingMatrix = matrices.cullingMatrix;

                    // setting the projectionMatrix does not let the post processing know about a changed nearClipping plane, we have to set it manually
                    //float near = -((_currentRenderCamera.farClipPlane * (matrices.projectionMatrix.m22 + 1f)) / (1f - matrices.projectionMatrix.m22));
                    reflectionCamera.nearClipPlane = matrices.near;
                    reflectionCamera.farClipPlane = matrices.far;

#if UNITY_EDITOR_OSX
                    if(enableMacOSXTemporaryLogsToAvoidCrashingTheEditor){
                        Debug.Log(" a bug in Unity for MacOSX causes the editor to crash if this message is not here. Terribly sorry about this");
                    }
#endif

                    // in the editor we want to delete the skybox from the PortalSurface and see the result
                    // todo: can we do away with the getComponent each frame if there is no Skybox on the main cam?
                    Material skyboxMaterial = matrices.mirrorSurface.CustomSkybox;
                    if (!skyboxMaterial)
                    {
                        GetSkybox(_currentRenderCamera, out _skyboxRenderCam);
                        if (_skyboxRenderCam)
                        {
                            skyboxMaterial = _skyboxRenderCam.material;
                        }
                    }

                    GetSkybox(reflectionCamera, out _skyboxReflectionCam);
                    if (_skyboxReflectionCam)
                    {
						if (skyboxMaterial)
						{
                            _skyboxReflectionCam.material = skyboxMaterial;
						}
						else
                        {
                            _skyboxReflectionCam.material = RenderSettings.skybox;
                        }
                    }



                    float renderScale = UniversalRenderPipeline.asset.renderScale;
                    UniversalRenderPipeline.asset.renderScale = 1f;

#if UNITY_2022_3_OR_NEWER
                    if(UseSubmitRenderRequest && _currentRenderCamera.cameraType != CameraType.SceneView)
                    {
                        UniversalRenderPipeline.SingleCameraRequest req = new UniversalRenderPipeline.SingleCameraRequest();
                        req.destination = reflectionCamera.targetTexture;
                        reflectionCamera.SubmitRenderRequest(req);
                    }else
                    {
#pragma warning disable CS0618 // Type or member is obsolete
						UniversalRenderPipeline.RenderSingleCamera(src, reflectionCamera);
#pragma warning restore CS0618 // Type or member is obsolete
                    }
#else
                    UniversalRenderPipeline.RenderSingleCamera(src, reflectionCamera);
#endif

                    UniversalRenderPipeline.asset.renderScale = renderScale;


                    matrices.mirrorSurface.UpdateMaterial(eye, _ptex.texture, this, matrices.depth, matrices.distance);

                    // reset the material to the one with the lowest depth
                    List<CameraPortalMatrices> li = cameraMatricesInOrder.FindAll(x => x.depth == matrices.depth
                        && x.depth == matrices.depth
                        && x.parentMirrorSurface == matrices.parentMirrorSurface
                        && x.parentsParentMirrorSurface == matrices.parentsParentMirrorSurface
                        && x.parentsParentsParentMirrorSurface == matrices.parentsParentsParentMirrorSurface
                        && x.parentsParentsParentsParentMirrorSurface == matrices.parentsParentsParentsParentMirrorSurface);
                    //Debug.Log("how many?" + li.Count);

                    if (li.Count > 0)
                    {
                        foreach (CameraPortalMatrices cm in li)
                        {
                            if (cm != matrices)
                            {
                                PooledPortalTexture p = _pooledTextures.Find(ptex => ptex.matrices.mirrorSurface == cm.mirrorSurface
                                    && ptex.matrices.parentMirrorSurface == cm.parentMirrorSurface
                                    && ptex.matrices.parentsParentMirrorSurface == cm.parentsParentMirrorSurface
                                    && ptex.matrices.parentsParentsParentMirrorSurface == cm.parentsParentsParentMirrorSurface
                                    && ptex.matrices.parentsParentsParentsParentMirrorSurface == cm.parentsParentsParentsParentMirrorSurface
                                    && ptex.matrices.depth == cm.depth && ptex.eye == eye);
                                if (p != null)
                                {
                                    cm.mirrorSurface.UpdateMaterial(eye, p.texture, this, cm.depth, cm.distance);
                                }
                            }
                        }
                    }


                    // turn on occlusionCulling even though there is an issue with the cullingMatrix for mirrors
                    // the RecusiveFindMirrorsInOrder will use VisibleFromCamera and that can early exit 
                    reflectionCamera.useOcclusionCulling = true;



                    if (_closestPortal != null && matrices.mirrorSurface.Portal == _closestPortal.OtherPortal)
                    {
                        previousTex = _ptex;
                    }
                }
            }


            // break the textureLocks
            _pooledTextures.ForEach(pTex => {
                pTex.liteLock = false;
                pTex.fullLock = false;
            });

            // Restore pixel light count
            if (disablePixelLights)
            {
                QualitySettings.pixelLightCount = oldPixelLightCount;
            }
        }

        private void GetFreeTexture(out PooledPortalTexture textureOut, Camera.StereoscopicEye eye = Camera.StereoscopicEye.Left)
        {
            PooledPortalTexture tex = _pooledTextures.Find(tex => !tex.fullLock && !tex.liteLock && tex.eye == eye);
            if (tex == null)
            {
                tex = new PooledPortalTexture();
                tex.eye = eye;
                _pooledTextures.Add(tex);

                // create the texture
                //Debug.Log("creating new pooledTexture: " + _pooledTextures.Count);

                if (useScreenScaleFactor && screenScaleFactor > 0)
                {
                    float scale = screenScaleFactor; // * (1f / depth);
                    textureSize = new Vector2(Screen.width * scale, Screen.height * scale);
                }

                //RenderTextureDescriptor desc = new RenderTextureDescriptor((int)textureSize.x, (int)textureSize.y, RenderTextureFormat.ARGB32, 1);
                RenderTextureDescriptor desc = new RenderTextureDescriptor((int)textureSize.x, (int)textureSize.y, _renderTextureFormat, 1);
                desc.useMipMap = false;
                desc.autoGenerateMips = false;

                desc.msaaSamples = (int)antiAliasing;

                tex.texture = RenderTexture.GetTemporary(desc);
                tex.texture.wrapMode = TextureWrapMode.Mirror;
                tex.texture.filterMode = FilterMode.Trilinear;
                tex.texture.anisoLevel = 9;
                tex.texture.name = "_Tex" + gameObject.name + "_" + _pooledTextures.Count;
                tex.texture.hideFlags = HideFlags.HideAndDontSave;
            }

            textureOut = tex;
        }

        private void Update()
        {
            if (Portals == null)
            {
                return;
            }
            foreach (Portal p in Portals) {
                p.MyRenderer = this;
            }

            if (_oldTextureSize != ((int)textureSize.x + (int)textureSize.y)
                || _oldScreenScaleFactor != screenScaleFactor
                || _oldAntiAliasing != antiAliasing
                || _oldRenderTextureFormat != _renderTextureFormat
                || _oldUseScreenScaleFactor != useScreenScaleFactor)
            {
                _oldUseScreenScaleFactor = useScreenScaleFactor;
                _oldAntiAliasing = antiAliasing;
                _oldRenderTextureFormat = _renderTextureFormat;
                _oldScreenScaleFactor = screenScaleFactor;
                _oldTextureSize = ((int)textureSize.x + (int)textureSize.y);

                foreach (PooledPortalTexture tex in _pooledTextures)
                {
                    DestroyImmediate(((RenderTexture)tex.texture));
                }
                _pooledTextures.Clear();

            }

            if (recursions > 8)
            {
                recursions = 8;

            }
        }
        private void GetUACData(Camera cam, out UniversalAdditionalCameraData uac)
        {
            UniversalAdditionalCameraData uacOut;

            if (!_UacPerCameras.TryGetValue(cam, out uacOut))
            {
                uacOut = cam.GetComponent<UniversalAdditionalCameraData>();
                _UacPerCameras.Add(cam, uacOut);
            }
            uac = uacOut;
        }

        private void GetSkybox(Camera cam, out Skybox skybox)
        {
            Skybox skyboxOut;

            if (!_SkyboxPerCameras.TryGetValue(cam, out skyboxOut))
            {
                skyboxOut = cam.GetComponent<Skybox>();
                _SkyboxPerCameras.Add(cam, skyboxOut);
            }
            skybox = skyboxOut;
        }

        private void CreatePortalCameras(Camera renderCamera, out Camera reflectionCamera)
        {
            reflectionCamera = null;

            // Camera for reflection
            Camera reflectionCam;
            _reflectionCameras.TryGetValue(renderCamera, out reflectionCam);

            if (reflectionCam == null)
            {
                //Debug.Log("new reflection camera for " + renderCamera.name);
                GameObject go = new GameObject("Portal Camera for " + renderCamera.name, typeof(Camera), typeof(Skybox));
                reflectionCamera = go.GetComponent<Camera>();
                reflectionCamera.useOcclusionCulling = true;
                reflectionCamera.enabled = false;
                reflectionCamera.transform.position = transform.position;
                reflectionCamera.transform.rotation = transform.rotation;
                reflectionCamera.gameObject.AddComponent<FlareLayer>();

                Skybox skybox = reflectionCamera.gameObject.GetComponent<Skybox>();
                _SkyboxPerCameras.Add(reflectionCamera, skybox);


                GetUACData(renderCamera, out _uacRenderCam);
                //_uacRenderCam = renderCamera.GetComponent<UniversalAdditionalCameraData>();
                if (_uacRenderCam != null)
                {
                    _uacReflectionCam = reflectionCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                    _uacReflectionCam.requiresColorOption = OpaqueTextureMode;
                    _uacReflectionCam.requiresDepthOption = DepthTextureMode;
                    _uacReflectionCam.renderPostProcessing = RenderPostProcessing;
                    _uacReflectionCam.renderShadows = RenderShadows;
                    //_uacReflectionCam.SetRenderer(1);

#if UNITY_2020_3_OR_NEWER
                    _uacReflectionCam.allowXRRendering = _uacRenderCam.allowXRRendering;
#endif
                }


                go.hideFlags = HideFlags.HideAndDontSave;

                if (_reflectionCameras.ContainsKey(renderCamera))
                {
                    _reflectionCameras[renderCamera] = reflectionCamera;
                }
                else
                {
                    _reflectionCameras.Add(renderCamera, reflectionCamera);
                }
            }
            else
            {
                reflectionCamera = reflectionCam;
            }
        }


        private void OnDestroy()
        {
            OnDisable();
        }

        // Cleanup all the objects we possibly have created
        void OnDisable()
        {
            //Debug.Log("OnDisable");
            portalRendererInstances.Remove(this);
            RenderPipeline.beginCameraRendering -= UpdateCamera;

            if (_master == this)
            {
                _master = null;
            }

            if (!Application.isPlaying)
            {
                foreach (var pTex in _pooledTextures)
                {
                    DestroyImmediate(((RenderTexture)pTex.texture));
                }

                foreach (var kvp in _reflectionCameras)
                {
                    DestroyImmediate(((Camera)kvp.Value).gameObject);
                }
         
                DestroyImmediate(_clippingPlaneMeshLeft);
                DestroyImmediate(_clippingPlaneLeft);
                DestroyImmediate(_clippingPlaneMeshRight);
                DestroyImmediate(_clippingPlaneRight);
                DestroyImmediate(_clippingPlaneMaterial);

                DestroyImmediate(_clippingPlaneLeftForAR);
                DestroyImmediate(_clippingPlaneRightForAR);
                DestroyImmediate(_depthMaterial);
            }
            else
            {
                foreach (var kvp in _reflectionCameras)
                {
                    Destroy(((Camera)kvp.Value).gameObject);
                }

                foreach (var pTex in _pooledTextures)
                {
                    Destroy(((RenderTexture)pTex.texture));
                }

                Destroy(_clippingPlaneMeshLeft);
                Destroy(_clippingPlaneLeft);
                Destroy(_clippingPlaneMeshRight);
                Destroy(_clippingPlaneRight);
                Destroy(_clippingPlaneMaterial);

                Destroy(_clippingPlaneLeftForAR);
                Destroy(_clippingPlaneRightForAR);
                Destroy(_depthMaterial);
            }

            _pooledTextures.Clear();
            _reflectionCameras.Clear();

            _UacPerCameras.Clear();
            _SkyboxPerCameras.Clear();
            _uacReflectionCam = null;
            _uacRenderCam = null;

            _clippingPlaneLeft = null;
            _clippingPlaneMeshLeft = null;
            _clippingPlaneRight = null;
            _clippingPlaneMeshRight = null;
            _clippingPlaneMaterial = null;

            _clippingPlaneLeftForAR = null;
            _clippingPlaneRightForAR = null;
            _depthMaterial = null;
        }

        public static Portal FindClosestPortalInAllRenderers(Vector3 pos) 
        {
            Portal closestPortal = null;
            float minDistance = float.MaxValue;

            for (int i = 0; i < portalRendererInstances.Count; i++)
            {
                PortalRenderer pr = portalRendererInstances[i];
                for (int j = 0; j < pr.Portals.Count; j++)
                {
                    Portal _p = pr.Portals[j];
                    if (_p && _p.PortalSurface && _p.isActiveAndEnabled)
                    {
                        float myDistance = Vector3.Distance(pos, _p.PortalSurface.ClosestPointOnBoundsFlattenedToPlane(pos));
                        //Debug.Log("myDistance: " + myDistance);
                        if (myDistance < minDistance)
                        {
                            minDistance = myDistance;
                            closestPortal = _p;
                        }
                    }
                }
            }

            return closestPortal;
        }


#if UNITY_EDITOR
        public void SurfaceGotDeselectedInEditor()
        {
            // notify the other surfaces as the material might have changed, to update their materials
            if (Portals == null)
            {
                return;
            }



            foreach (Portal portal in Portals)
            {
                if (portal && portal.PortalSurface)
                {
                    portal.PortalSurface.RefreshMaterialInEditor();
                }
            }
        }
#endif
    }

    public class PooledPortalTexture
    {
        public bool liteLock;
        public bool fullLock;
        public CameraPortalMatrices matrices;
        public RenderTexture texture;
        //public int depth;
        public Camera.StereoscopicEye eye = Camera.StereoscopicEye.Left;

        public PooledPortalTexture()
        {
        }
    }

    public class CameraPortalMatrices
    {
        public Matrix4x4 projectionMatrix;
        public Matrix4x4 worldToCameraMatrix;
        public Matrix4x4 cullingMatrix;
        public PortalSurface mirrorSurface;
        public PortalSurface parentMirrorSurface;
        public PortalSurface parentsParentMirrorSurface;
        public PortalSurface parentsParentsParentMirrorSurface;
        public PortalSurface parentsParentsParentsParentMirrorSurface;
        public bool even;
        public Vector3 camPos;
        public Quaternion camRot;
        public float near;
        public float far;
        public int depth;
        public float distance;

        public CameraPortalMatrices(Matrix4x4 projectionMatrix, Matrix4x4 worldToCameraMatrix, Matrix4x4 cullingMatrix,
            PortalSurface mirrorSurface, 
            bool even,
            Vector3 camPos, Quaternion camRot, float near, float far, int depth, float distance,
            PortalSurface parentMirrorSurface,
            PortalSurface parentsParentMirrorSurface,
            PortalSurface parentsParentsParentMirrorSurface,
            PortalSurface parentsParentsParentsParentMirrorSurface)
        {
            this.projectionMatrix = projectionMatrix;
            this.worldToCameraMatrix = worldToCameraMatrix;
            this.mirrorSurface = mirrorSurface;
            this.even = even;
            this.camPos = camPos;
            this.camRot = camRot;
            this.near = near;
            this.far = far;
            this.depth = depth;
            this.distance = distance;
            this.parentMirrorSurface = parentMirrorSurface;
            this.parentsParentMirrorSurface = parentsParentMirrorSurface;
            this.parentsParentsParentMirrorSurface = parentsParentsParentMirrorSurface;
            this.parentsParentsParentsParentMirrorSurface = parentsParentsParentsParentMirrorSurface;
            this.cullingMatrix = cullingMatrix;
        }
    }
}