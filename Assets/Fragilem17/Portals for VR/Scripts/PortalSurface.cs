using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR;
using System;
using UnityEngine.Rendering.Universal;
using System.Linq;
//using Newtonsoft.Json;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Fragilem17.MirrorsAndPortals
{
    [ExecuteInEditMode]
    public class PortalSurface : MonoBehaviour
    {
        private Portal _portal;

        [Tooltip("The source material, disable and re-enable this component if you make changes to the material")]
        public Material Material;

        [Tooltip("When the camera is further from this distance, the surface stops updating it's texture.")]
        [MinAttribute(0)]
        public float maxRenderingDistance = 5f;

        [Tooltip("Defines whether a certain amount of color should be blended in with the reflection depending on distance.")]
        public bool useColorBlending = true;

        public Color BlendColor = Color.black;

        [Tooltip("The value at 'Time' 0, means when you're directly in front of the portal at a distance of 0. 'Time' 1 reflects the maxRenderingDistance.")]
        public AnimationCurve colorBlendingCurve = AnimationCurve.Linear(0, -99f, 1f, 99f);


        [Space(10)]

        [Tooltip("Defines whether the albedo map alpha should be changed over distance.")]
        public bool useAlbedoAlphaFading = false;

        public AnimationCurve albedoAlphaFadeCurve = AnimationCurve.Linear(0, -99f, 1f, 99f);


        [Space(10)]

        [Tooltip("Defines whether the amount of Refraction should be changed depending on distance.")]
        public bool useRefractionFading = false;

        [Tooltip("The value at 'Time' 0, means when you're directly in front of the portal at a distance of 0. 'Time' 1 reflects the maxRenderingDistance.")]
        public AnimationCurve refractionFadingCurve = AnimationCurve.Linear(0, -99f, 1f, 99f);



        [Header("Other")]

        [Tooltip("The custom skybox the portal will use, when none is used, the skybox from the MainCamera is used or the skybox from lighting settings.")]
        public Material CustomSkybox;

        [Tooltip("The portal camera's nearPlane will be at the distance between the camera and the portalSurface, use this offset to modify the nearPlane")]
        public float clippingPlaneOffset = -0.002f;

        [Tooltip("An oblique projection matrix rotates the PortalCamera's near clipping field so it aligns with the portals surface. It's required on if stuff is near the back side of the portal. However, the ObliquePM messes up several postprocessing effects like SSAO and the occlusion culling of the portalCamera. It's a tradeoff!")]
        public bool requireObliqueProjectionMatrix = true;

        [Tooltip("An oblique projection matrix rotates the PortalCamera's near clipping field so it aligns with the portals surface. It's required \"on\" if stuff is near the back side of the portal. However, the ObliquePM messes up several postprocessing effects like SSAO and the occlusion culling of the portalCamera. It's a tradeoff!")]
        public float nearDistanceToStartDisablingObliquePM = 0.02f;

        public MeshRenderer myMeshRenderer;
        private MeshFilter _myMeshFilter;



        private Plane _plane;
        private Material _material;
        private Material _myMaterialInstance;
        private PortalRenderer _myRenderer;
        private Color _oldFadeColor = Color.black;
        private Material _oldMaterial;
        private bool _wasToFar = false;

        private bool _isSelectedInEditor = false;

        [HideInInspector]
        public Texture _currentTexLeft;
        [HideInInspector]
        public Texture _currentTexRight;
        [HideInInspector]
        public float _currentDistanceBlend;

        private Vector3 _startPosition;


        public Portal Portal { get => _portal; }
		public Material MyMaterialInstance { get => _myMaterialInstance; }

		// the worldspace bounds of this portalSurface (precompute?)
		private Vector3[] _portalBounds;

        enum SideEdge { 
            Top,
            Left, 
            Bottom,
            Right,
            WRONG
        }

        private void OnEnable()
        {
            _startPosition = transform.localPosition;

#if UNITY_EDITOR
            // our surfaces can't be batched! we rely on the mesh to get the real worls bounds
            // also, you want to hide this mesh as soon as it's out of view.
            // this can't happen when batched with a lot of other meshes.. forcing the recursion to keep rendering.
            var flags = GameObjectUtility.GetStaticEditorFlags(gameObject);
            flags &= ~StaticEditorFlags.BatchingStatic;
            GameObjectUtility.SetStaticEditorFlags(gameObject, flags);
#endif

            _portal = GetComponentInParent<Portal>();
            if (!_portal)
            {
                Debug.LogWarning(PortalUtils.Colorize("[PORTALS] ", PortalUtils.DebugColors.Warn, true) + name + " a PortalSurface needs to be a child gameObject of a Portal");
            }

            //Debug.Log("_isSelectedInEditor " + _isSelectedInEditor + " : "+ gameObject.name);
            if (myMeshRenderer == null)
            {
                myMeshRenderer = GetComponent<MeshRenderer>();
            }
            if (_myMeshFilter == null)
            {
                _myMeshFilter = GetComponent<MeshFilter>();
            }


            _wasToFar = false;

            if (!Material && myMeshRenderer)
            {
                Material = myMeshRenderer.sharedMaterial;
            }


            if (myMeshRenderer && Material)
            {
                _oldMaterial = Material;

                if (_isSelectedInEditor)
                {
                    // make sure we're editing the source materials, not the instance
                    Material.SetColor("_FadeColor", BlendColor);
                    myMeshRenderer.sharedMaterial = Material;
                    _material = Material;
                }
                else
                {
                    _material = new Material(Material);
                    _myMaterialInstance = _material;
                    _material.name += " (for " + gameObject.name + ")";
                    _material.SetColor("_FadeColor", BlendColor);
                    myMeshRenderer.material = _material;
                }

                // find my bounds and save them
                
            }

            if (_material.HasProperty("_FadeColorBlend"))
            {
                _currentDistanceBlend = _material.GetFloat("_FadeColorBlend");
            }

#if UNITY_EDITOR
            Selection.selectionChanged += OnSelectionChange;
#endif          
        }

#if UNITY_EDITOR
        private void Start()
        {
            SetDefaultCurves();
        }
#endif


        private void OnDisable()
        {

#if UNITY_EDITOR
            //Debug.Log("disabling");
            _isSelectedInEditor = false;
            Selection.selectionChanged -= OnSelectionChange;
#endif
            if (_material != Material)
            {
                DestroyImmediate(_material, true);
            }
            if (myMeshRenderer)
            {
                myMeshRenderer.material = Material;
            }
        }

#if UNITY_EDITOR
        private void OnDestroy()
        {
            //Debug.Log("destroying");
            _isSelectedInEditor = false;
            Selection.selectionChanged -= OnSelectionChange;
            //DestroyImmediate(gameObject);
        }

        [ContextMenu("Copy my properties and settings to the other PortalSurface.")]
        void CopyPropertiesToOtherPortal()
        {
            if (_portal && _portal.OtherPortal && _portal.OtherPortal.PortalSurface)
            {
                _portal.OtherPortal.PortalSurface.Material = Material;
                _portal.OtherPortal.PortalSurface.maxRenderingDistance = maxRenderingDistance;
                _portal.OtherPortal.PortalSurface.useColorBlending = useColorBlending;
                _portal.OtherPortal.PortalSurface.BlendColor = BlendColor;
                _portal.OtherPortal.PortalSurface.colorBlendingCurve.keys = colorBlendingCurve.keys;
                _portal.OtherPortal.PortalSurface.useAlbedoAlphaFading = useAlbedoAlphaFading;
                _portal.OtherPortal.PortalSurface.albedoAlphaFadeCurve.keys = albedoAlphaFadeCurve.keys;
                _portal.OtherPortal.PortalSurface.useRefractionFading = useRefractionFading;
                _portal.OtherPortal.PortalSurface.refractionFadingCurve.keys = refractionFadingCurve.keys;
                _portal.OtherPortal.PortalSurface.clippingPlaneOffset = clippingPlaneOffset;
                EditorUtility.SetDirty(_portal.OtherPortal.PortalSurface);
            }
        }

        [ContextMenu("Serialise My Curves")]
        private void SerialiseMyCurves()
        {
            SerializableCurve sc = new SerializableCurve(colorBlendingCurve);
            string json = JsonUtility.ToJson(sc);
            //json = JsonConvert.ToString(json);
            Debug.Log("colorBlendingCurve: " + json);

            sc = new SerializableCurve(albedoAlphaFadeCurve);
            json = JsonUtility.ToJson(sc);
            //json = JsonConvert.ToString(json);
            Debug.Log("albedoAlphaFadeCurve: " + json);

            sc = new SerializableCurve(refractionFadingCurve);
            json = JsonUtility.ToJson(sc);
            //json = JsonConvert.ToString(json);
            Debug.Log("refractionFadingCurve: " + json);
        }

        private void SetDefaultCurves()
        {
            if (colorBlendingCurve.keys.Length == 2 && colorBlendingCurve.keys[0].value == -99f && colorBlendingCurve.keys[1].value == 99f)
            {
                Debug.Log("a PortalSurface was enabled, SetDefaultColorBlendingCurve on " + Portal?.name);
                SetDefaultColorBlendingCurve();
                EditorUtility.SetDirty(this);
            }

            if (albedoAlphaFadeCurve.keys.Length == 2 && albedoAlphaFadeCurve.keys[0].value == -99f && albedoAlphaFadeCurve.keys[1].value == 99f)
            {
                Debug.Log("a PortalSurface was enabled, SetDefaultAlbedoAlphaFadeCurve on " + Portal?.name);
                SetDefaultAlbedoAlphaFadeCurve();
                EditorUtility.SetDirty(this);
            }

            if (refractionFadingCurve.keys.Length == 2 && refractionFadingCurve.keys[0].value == -99f && refractionFadingCurve.keys[1].value == 99f)
            {
                Debug.Log("a PortalSurface was enabled, SetDefaultRefractionFadingCurve on " + Portal?.name);
                SetDefaultRefractionFadingCurve();
                EditorUtility.SetDirty(this);
            }
        }

        [ContextMenu("Reset RefractionFadingCurve")]
        private void SetDefaultRefractionFadingCurve()
        {
            string curve = "{\"keys\":[{\"inTangent\":0.0,\"inWeight\":0.0,\"outTangent\":-0.0003126605006400496,\"outWeight\":1.0,\"weightedMode\":0,\"time\":0.0,\"value\":0.0},{\"inTangent\":0.0,\"inWeight\":0.3333333432674408,\"outTangent\":0.0,\"outWeight\":0.3333333432674408,\"weightedMode\":0,\"time\":0.05000000074505806,\"value\":0.0},{\"inTangent\":-0.005003984551876783,\"inWeight\":0.12056756019592285,\"outTangent\":-0.005003984551876783,\"outWeight\":0.0,\"weightedMode\":0,\"time\":0.9946242570877075,\"value\":0.1989855170249939}],\"postWrapMode\":\"ClampForever\",\"preWrapMode\":\"ClampForever\"}";
            SerializableCurve sc2 = JsonUtility.FromJson<SerializableCurve>(curve);
            refractionFadingCurve = sc2.toCurve();
        }

        [ContextMenu("Reset AlbedoAlphaFadeCurve")]
        private void SetDefaultAlbedoAlphaFadeCurve()
        {
            string curve = "{\"keys\":[{\"inTangent\":0.0,\"inWeight\":0.0,\"outTangent\":0.0,\"outWeight\":0.0,\"weightedMode\":0,\"time\":0.0,\"value\":0.0},{\"inTangent\":0.0,\"inWeight\":1.0,\"outTangent\":45.0,\"outWeight\":0.022694604471325876,\"weightedMode\":0,\"time\":0.009999999776482582,\"value\":0.0},{\"inTangent\":45.0,\"inWeight\":0.8593189716339111,\"outTangent\":0.0,\"outWeight\":0.11139071732759476,\"weightedMode\":0,\"time\":0.029999999329447748,\"value\":0.8999999761581421},{\"inTangent\":0.03659231960773468,\"inWeight\":0.1438542902469635,\"outTangent\":0.03659231960773468,\"outWeight\":0.3333333432674408,\"weightedMode\":0,\"time\":0.6000000238418579,\"value\":0.8999999761581421},{\"inTangent\":0.0,\"inWeight\":0.0,\"outTangent\":0.0,\"outWeight\":0.0,\"weightedMode\":0,\"time\":1.0,\"value\":2.200000047683716}],\"postWrapMode\":\"ClampForever\",\"preWrapMode\":\"ClampForever\"}";
            SerializableCurve sc2 = JsonUtility.FromJson<SerializableCurve>(curve);
            albedoAlphaFadeCurve = sc2.toCurve();
        }

        [ContextMenu("Reset ColorBlendingCurve")]
        private void SetDefaultColorBlendingCurve()
        {
            string curve = "{\"keys\":[{\"inTangent\":0.0,\"inWeight\":0.0,\"outTangent\":0.0,\"outWeight\":0.0,\"weightedMode\":0,\"time\":0.0,\"value\":0.0},{\"inTangent\":0.0,\"inWeight\":0.3333333432674408,\"outTangent\":0.0,\"outWeight\":0.3333333432674408,\"weightedMode\":0,\"time\":0.6000000238418579,\"value\":0.0},{\"inTangent\":0.0,\"inWeight\":0.0,\"outTangent\":0.0,\"outWeight\":0.0,\"weightedMode\":0,\"time\":1.0,\"value\":1.0}],\"postWrapMode\":\"ClampForever\",\"preWrapMode\":\"ClampForever\"}";
            SerializableCurve sc2 = JsonUtility.FromJson<SerializableCurve>(curve);
            colorBlendingCurve = sc2.toCurve();
        }
#endif

        public void UpdatePositionsInMaterial(Vector3 position, Vector3 direction)
        {
            if (_material && _material.HasProperty("_WorldPos"))
            {
                _material.SetVector("_WorldPos", position);
                _material.SetVector("_WorldDir", direction);
            }
        }

        public bool VisibleFromCamera(Camera renderCamera, bool ignoreDistance = true, float offset = 0, bool ignoreCulling = false)
        {
            if (!enabled || !myMeshRenderer || !_material || !gameObject.activeInHierarchy)
            {
                return false;
            }

            if (!ignoreCulling && !myMeshRenderer.isVisible)
            {
                //Debug.Log(name + " i'm not visible!");
                return false;
            }

            // check the normal of the mirror. if the camera is behind it, return early
            Vector3 forward = -1 * transform.forward; //transform.TransformDirection(Vector3.forward);
            //Vector3 toOther = (renderCamera.transform.position+(renderCamera.transform.forward * -offset)) - (transform.position);
            Vector3 toOther = (renderCamera.transform.position) - (transform.position);


            if (Vector3.Dot(forward, toOther) < -offset) // if we're behind the mirror 
            {
                return false;
            }

            if (!ignoreDistance)
            {
                float d = Vector3.Distance(ClosestPointOnBoundsFlattenedToPlane(renderCamera.transform.position), renderCamera.transform.position);
                bool toFar = d > maxRenderingDistance;
                if (toFar && !_wasToFar)
                {
                    _wasToFar = true;

                    // blend the surface
                    if (useColorBlending && _material.HasProperty("_FadeColorBlend"))
                    {
                        //Debug.Log("toFar: " + gameObject.name + " distnce: "+ d);
                        _material.SetFloat("_FadeColorBlend", colorBlendingCurve.Evaluate(1));
                    }
                    if (useAlbedoAlphaFading && _material.HasProperty("_AlbedoAlpha"))
                    {
                        _material.SetFloat("_AlbedoAlpha", albedoAlphaFadeCurve.Evaluate(1));
                    }
                    if (useRefractionFading && _material.HasProperty("_refraction"))
                    {
                        _material.SetFloat("_refraction", refractionFadingCurve.Evaluate(1));
                    }
                }
                if (!toFar && _wasToFar)
                {
                    _wasToFar = false;
                }

                if (toFar)
                {
                    return false;
                }
            }

            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(renderCamera);
            planes[4].Translate(renderCamera.transform.forward * offset);
            bool inBounds = GeometryUtility.TestPlanesAABB(planes, myMeshRenderer.bounds);
            return inBounds;
        }

        public Vector3 ClosestPointOnBoundsFlattenedToPlane(Vector3 toPos)
        {
            Vector3 p = myMeshRenderer.bounds.ClosestPoint(toPos);
            p = _plane.ClosestPointOnPlane(p);
            return p;
        }

        public Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivotPosition, Quaternion rotation)
        {
            return pivotPosition + (rotation * (point - pivotPosition)); // returns new position of the point;
        }


        private static Rect CreateRectFromPoints(Vector2[] points) 
        {
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            foreach (Vector2 point in points)
            {
                if (point.x < minX)
                {
                    minX = point.x;
                }
                if (point.y < minY)
                {
                    minY = point.y;
                }
                if (point.x > maxX)
                {
                    maxX = point.x;
                }
                if (point.y > maxY)
                {
                    maxY = point.y;
                }
            }

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }
       

        private bool RectIntersects(Rect r1, Rect r2, out Rect area)
        {
            area = new Rect();

            if (r2.Overlaps(r1))
            {
                float x1 = Mathf.Min(r1.xMax, r2.xMax);
                float x2 = Mathf.Max(r1.xMin, r2.xMin);
                float y1 = Mathf.Min(r1.yMax, r2.yMax);
                float y2 = Mathf.Max(r1.yMin, r2.yMin);
                area.x = Mathf.Min(x1, x2);
                area.y = Mathf.Min(y1, y2);
                area.width = Mathf.Max(0.0f, x1 - x2);
                area.height = Mathf.Max(0.0f, y1 - y2);

                return true;
            }

            return false;
        }

        private Vector3[] _shrinkToVectorArray = new Vector3[4];
        public Vector3[] ShrinkPointsToBounds(Camera reflectionCamera, float offset, bool debug)
        {
            _portalBounds = GetPortalBounds(reflectionCamera, offset);
          
            
            Vector3[] fustrumBoundsOnPlane = new Vector3[4];
            bool allPointsFound = GetFustrumBoundsOnPlane(reflectionCamera, offset, ref fustrumBoundsOnPlane);

            /*for (int x = 0; x < 4; x++)
            {
                DebugExtension.DebugWireSphere(fustrumBoundsOnPlane[x], Color.blue, 0.1f);
            }*/
            
            for (int x = 0; x < 4; x++)
            {
                _shrinkToVectorArray[x] = _portalBounds[x];

                fustrumBoundsOnPlane[x] = RotatePointAroundPivot(fustrumBoundsOnPlane[x], transform.position, Quaternion.Inverse(transform.rotation));
                _portalBounds[x] = RotatePointAroundPivot(_portalBounds[x], transform.position, Quaternion.Inverse(transform.rotation));

                //DebugExtension.DebugWireSphere(fustrumBoundsOnPlane[x], Color.red, 0.1f);
                //DebugExtension.DebugWireSphere(_portalBounds[x], Color.green, 0.1f);
            }
            

            Vector2[] rect1 = new Vector2[4];
            Vector2[] rect2 = new Vector2[4];
            for (int x = 0; x < 4; x++)
            {
                rect1[x] = new Vector2(fustrumBoundsOnPlane[x].x, fustrumBoundsOnPlane[x].y);
                rect2[x] = new Vector2(_portalBounds[x].x, _portalBounds[x].y);
            }

            Rect r1 = CreateRectFromPoints(rect1);
            Rect r2 = CreateRectFromPoints(rect2);
            Rect rectOut;

            if(RectIntersects(r1, r2, out rectOut))
            {
                // bottomLeft / bottomRight / topLeft
                _shrinkToVectorArray[0] = new Vector3(rectOut.center.x - (rectOut.width/2f), rectOut.center.y - (rectOut.height / 2f), fustrumBoundsOnPlane[0].z);
                _shrinkToVectorArray[1] = new Vector3(rectOut.center.x + (rectOut.width/2f), rectOut.center.y - (rectOut.height / 2f), fustrumBoundsOnPlane[0].z);
                _shrinkToVectorArray[2] = new Vector3(rectOut.center.x - (rectOut.width/2f), rectOut.center.y + (rectOut.height / 2f), fustrumBoundsOnPlane[0].z);
                _shrinkToVectorArray[3] = new Vector3(rectOut.center.x + (rectOut.width/2f), rectOut.center.y + (rectOut.height / 2f), fustrumBoundsOnPlane[0].z);
            
                for (int x = 0; x < 4; x++)
                {
                    _shrinkToVectorArray[x] = RotatePointAroundPivot(_shrinkToVectorArray[x], transform.position, transform.rotation);
                    _shrinkToVectorArray[x] = _shrinkToVectorArray[x] + ((_shrinkToVectorArray[x] - reflectionCamera.transform.position) * 0.025f);

					/*if (debug)
					{
                        DebugExtension.DebugWireSphere(_shrinkToVectorArray[x], Color.green, 0.1f, 0, false);
					}*/
                }
            }
            
            return _shrinkToVectorArray;
        }


        private bool GetFustrumBoundsOnPlane(Camera reflectionCamera, float surfaceDist, ref Vector3[] positionsOnPlane)
        {
            float offset = 0;
            if (surfaceDist < 0.01f)
            {
                offset = 0.01f - MathF.Max(surfaceDist, 0);
            }

            Vector3[] frustumCorners = new Vector3[4];
            reflectionCamera.CalculateFrustumCorners(new Rect(0f, 0f, 1f, 1f), 1, Camera.MonoOrStereoscopicEye.Mono, frustumCorners);
            bool allSucceeded = true;
            for (int i = 0; i < 4; i++)
            {
                frustumCorners[i] = reflectionCamera.transform.TransformPoint(frustumCorners[i]);
                if (!ProjectPointOnPlane(reflectionCamera.transform.position, frustumCorners[i], ref positionsOnPlane[i], offset)) {
                    // we're not hitting the plane.. hit far enough, then get the closest to the plane
                    positionsOnPlane[i] = reflectionCamera.transform.position + ((frustumCorners[i] - reflectionCamera.transform.position) * 25f);
                    //DebugExtension.DebugWireSphere(positionsOnPlane[i], Color.magenta, 0.07f);
                    positionsOnPlane[i] = _plane.ClosestPointOnPlane(positionsOnPlane[i]);
                    allSucceeded = false;
                }
                //DebugExtension.DebugWireSphere(frustumCorners[i], Color.magenta, 0.1f);
                //DebugExtension.DebugWireSphere(positionsOnPlane[i], Color.blue, 0.05f);

                //positionsOnPlane[i] += transform.forward * -0.1f;
            }

            return allSucceeded;
        }


        public void UpdateMaterial(Camera.StereoscopicEye eye = Camera.StereoscopicEye.Left, RenderTexture texture = null, PortalRenderer myRenderer = null, int depth = 1, float distance = 0)
        {
            if (myMeshRenderer && _material)
            {
                //Debug.Log(gameObject.name + " set prop 3");

                float distPercent = distance / maxRenderingDistance;
                float dist = distance;
                _myRenderer = myRenderer;
                //Debug.Log(name + " : " + distance + " : " + distPercent);
                Material m = _material;

                if (depth >= _myRenderer.recursions + 1)
                {
                    // we need to be fully opaque.. no need to do anything else
                    if (useColorBlending && m.HasProperty("_FadeColorBlend"))
                    {
                        m.SetFloat("_FadeColorBlend", colorBlendingCurve.Evaluate(1));
                    }
                    if (useAlbedoAlphaFading && m.HasProperty("_AlbedoAlpha"))
                    {
                        m.SetFloat("_AlbedoAlpha", albedoAlphaFadeCurve.Evaluate(1));
                    }
                    if (useRefractionFading && m.HasProperty("_refraction"))
                    {
                        m.SetFloat("_refraction", refractionFadingCurve.Evaluate(1));
                    }
                    return;
                }

                if (m.HasProperty("_flipY"))
                {
                    //Debug.Log("depth: " + depth);
                    if (_myRenderer.FlipSecondRecursion)
                    {
                        m.SetInt("_flipY", depth == 1 ? 0 : 1);
                    }
                    else
                    {
                        m.SetInt("_flipY", 0);
                    }
                }

                if (m.HasProperty("_ForceEye"))
                {
                    m.SetInt("_ForceEye", eye == Camera.StereoscopicEye.Left ? 0 : 1);
                }

                if (eye == Camera.StereoscopicEye.Left && m.HasProperty("_TexLeft") && texture != null)
                {
                    m.SetTexture("_TexLeft", texture);
                    _currentTexLeft = texture;
                }

                if (eye == Camera.StereoscopicEye.Right && XRSettings.enabled && m.HasProperty("_TexRight") && texture != null)
                {
                    m.SetTexture("_TexRight", texture);
                    _currentTexRight = texture;
                }

                EnableTransparancy();


                if (depth != -1)
                {
                    if (useColorBlending)
                    {
                        if (m.HasProperty("_FadeColorBlend"))
                        {
                            float curveVal = colorBlendingCurve.Evaluate(distPercent);
                            m.SetFloat("_FadeColorBlend", curveVal);
                            //Debug.Log("from surface: " + transform.parent.name + " : " + curveVal);
                            _currentDistanceBlend = curveVal;
                            //Debug.Log(gameObject.name + " 2 blend " + curveVal + " distance: " + distance);
                        }
                    }

                    if (useRefractionFading)
                    {
                        if (m.HasProperty("_refraction"))
                        {
                            float curveVal = refractionFadingCurve.Evaluate(distPercent);
                            m.SetFloat("_refraction", curveVal);
                            //Debug.Log(gameObject.name + " 2 blend " + blend + " distance: " + distance);
                        }
                    }

                    if (useAlbedoAlphaFading)
                    {
                        if (m.HasProperty("_AlbedoAlpha"))
                        {
                            float curveVal = albedoAlphaFadeCurve.Evaluate(distPercent);
                            m.SetFloat("_AlbedoAlpha", curveVal);
                            //Debug.Log(gameObject.name + " 2 blend " + blend + " distance: " + distance);
                        }
                    }
                }
            }
        }

        // returns worldspace bounds of this meshRenderer
        public Vector3[] GetPortalBounds(Camera reflectionCamera, float offset) // , float depthDistance, Camera.MonoOrStereoscopicEye eye
        {
            Bounds b = _myMeshFilter.sharedMesh.bounds;

            // worldspace 0,0,0 is now the transform
            Vector3 boundsCenterOffset = Vector3.zero - b.center;
            b.center = transform.position - boundsCenterOffset;



            //Vector3 closestPointOnPlane = ClosestPointOnBoundsFlattenedToPlane(reflectionCamera.transform.position);
            //float distance = Vector3.Distance(closestPointOnPlane, reflectionCamera.transform.position);
            /*if (offset < 0.015f)
            {
                b.center += transform.forward * -(0.015f - MathF.Max(offset, 0));
            }*/
            // move forward a bit

            Vector3 bottomLeft = b.min; // bottomLeft
            Vector3 topRight = b.max; // topRight
            Vector3 topLeft = new Vector3(b.min.x, b.max.y, b.min.z);
            Vector3 bottomRight = new Vector3(b.max.x, b.min.y, b.min.z);

            float scaleLarger = 1f;

            bottomLeft = ScaleAroundPivot(bottomLeft, transform.position, transform.lossyScale * scaleLarger);
            topRight = ScaleAroundPivot(topRight, transform.position, transform.lossyScale * scaleLarger);
            topLeft = ScaleAroundPivot(topLeft, transform.position, transform.lossyScale * scaleLarger);
            bottomRight = ScaleAroundPivot(bottomRight, transform.position, transform.lossyScale * scaleLarger);

            bottomLeft = RotatePointAroundPivot(bottomLeft, transform.position, transform.rotation);
            topRight = RotatePointAroundPivot(topRight, transform.position, transform.rotation);
            topLeft = RotatePointAroundPivot(topLeft, transform.position, transform.rotation);
            bottomRight = RotatePointAroundPivot(bottomRight, transform.position, transform.rotation);
            /*
            DebugExtension.DebugWireSphere(bottomLeft, Color.cyan, 0.2f);
            DebugExtension.DebugWireSphere(topRight, Color.red, 0.2f);
            DebugExtension.DebugWireSphere(topLeft, Color.green, 0.2f);
            DebugExtension.DebugWireSphere(bottomRight, Color.yellow, 0.2f);
            DebugExtension.DebugBounds(b, Color.cyan);
            */
            Vector3[] points = new Vector3[4];
            points[0] = bottomLeft;
            points[1] = bottomRight;
            points[2] = topLeft;
            points[3] = topRight;

            return points;
        }

        private bool ProjectPointOnPlane(Vector3 origin, Vector3 target, ref Vector3 posOnPlane, float offset = 0)
        {
            //Plane p = new Plane(-transform.forward, transform.position + (-transform.forward * offset));
            //
            Ray r = new Ray(origin, (target - origin));
            float distance = 0;
            if(_plane.Raycast(r, out distance))
            {

                posOnPlane = r.origin + (r.direction.normalized * (distance));
                //DebugExtension.DebugWireSphere(posOnPlane, Color.blue, 0.05f);
				
                return true;
            }
            return false;
        }


        public Vector3 ScaleAroundPivot(Vector3 target, Vector3 pivot, Vector3 newScale)
        {
            // calc final position post-scale
            Vector3 dir = (target - pivot);
            dir.Scale(newScale);
            return pivot + dir;
        }


#if UNITY_EDITOR
        void OnSelectionChange()
        {
            if (this && isActiveAndEnabled)
            {
                if (gameObject == Selection.activeGameObject)
                {
                    _isSelectedInEditor = true;

                    // make sure we're editing the source materials, not the instance
                    if (Material != null)
                    {
                        Material.SetColor("_FadeColor", BlendColor);
                        myMeshRenderer.sharedMaterial = Material;
                        _material = Material;
                    }
                }
                else if (_isSelectedInEditor)
                {
                    // i'm no longer selected
                    _isSelectedInEditor = false;

                    OnDisable();
                    OnEnable();

                    if (_myRenderer != null)
                    {
                        _myRenderer.SurfaceGotDeselectedInEditor();
                    }
                }
            }
        }

        public void RefreshMaterialInEditor()
        {
            OnDisable();
            OnEnable();
        }


        private void Update()
        {
            _plane = new Plane(-transform.forward, transform.position);

            //if (_oldFadeColor != (FadeColor.r + FadeColor.g + FadeColor.b))
            if (!BlendColor.Equals(_oldFadeColor))
            {
                if (_material)
                {
                    //Debug.Log(gameObject.name + " set prop 1");
                    _material.SetColor("_FadeColor", BlendColor);
                }
                //_oldFadeColor = (FadeColor.r + FadeColor.g + FadeColor.b);
                _oldFadeColor = BlendColor;
            }

            if (_oldMaterial != Material)
            {
                _material = Material;
                RefreshMaterialInEditor();
            }
        }

#endif

#if !UNITY_EDITOR 
        private void Update()
        {
            _plane = new Plane(-transform.forward, transform.position);
        }
#endif

        public void EnableTransparancy()
        {
            if (_material)
            {
                //Debug.Log(gameObject.name + " set prop 2");
                _material.SetInt("_useTransparency", 1);
            }
        }

        public void DisableTransparancy()
        {
            if (_material)
            {
                //Debug.Log(gameObject.name + " set prop 2");
                _material.SetInt("_useTransparency", 0);
            }
        }

        public void TurnOffForceEye()
        {
            if (_material && _material.HasProperty("_ForceEye"))
            {
                //Debug.Log(gameObject.name + " set prop 2");
                _material.SetInt("_ForceEye", -1);
            }
        }
        public void ForceLeftEye()
        {
            if (_material && _material.HasProperty("_ForceEye"))
            {
                //Debug.Log(gameObject.name + " set prop 2");
                _material.SetInt("_ForceEye", 0);
            }
        }
    }

    public class MinToAttribute : PropertyAttribute
    {
        public float? max;
        public float min;

        public MinToAttribute() { }
        public MinToAttribute(float max, float min = 0)
        {
            this.max = max;
            this.min = min;
        }
    }

    public class Triangle
    {
        private Vector3 point1, point2, point3;

        public Triangle(Vector3 point1, Vector3 point2, Vector3 point3)
        {
            this.point1 = point1;
            this.point2 = point2;
            this.point3 = point3;
        }

        public float SurfaceArea()
        {
            // Using Heron's Formula to calculate surface area
            float a = Vector3.Distance(point1, point2);
            float b = Vector3.Distance(point2, point3);
            float c = Vector3.Distance(point3, point1);
            float s = (a + b + c) / 2;
            return Mathf.Sqrt(s * (s - a) * (s - b) * (s - c));
        }

        public float Height(Vector3 basePoint)
        {
            // Using the formula for height of a triangle
            Vector3 baseToVertex = Vector3.Cross(point2 - point1, point3 - point1);
            return Vector3.Dot(baseToVertex, basePoint - point1) / baseToVertex.magnitude;
        }

        public float Height(float longSide)
        {
            return (2f * SurfaceArea()) / longSide;
        }
    }


#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(MinToAttribute))]
    public class MinToDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position,
                                   SerializedProperty property,
                                   GUIContent label)
        {
            var ctrlRect = EditorGUI.PrefixLabel(position, label);
            Rect[] r = SplitRectIn3(ctrlRect, 36, 5);
            var att = (MinToAttribute)attribute;
            var type = property.propertyType;
            if (type == SerializedPropertyType.Vector2)
            {
                var vec = property.vector2Value;
                float min = vec.x;
                float to = vec.y;
                min = EditorGUI.FloatField(r[0], min);
                to = EditorGUI.FloatField(r[2], to);
                EditorGUI.MinMaxSlider(r[1], ref min, ref to, att.min, att.max ?? to);
                vec = new Vector2(min < to ? min : to, to);
                property.vector2Value = vec;
            }
            else if (type == SerializedPropertyType.Vector2Int)
            {
                var vec = property.vector2IntValue;
                float min = vec.x;
                float to = vec.y;
                min = EditorGUI.IntField(r[0], (int)min);
                to = EditorGUI.IntField(r[2], (int)to);
                EditorGUI.MinMaxSlider(r[1], ref min, ref to, att.min, att.max ?? to);
                vec = new Vector2Int(Mathf.RoundToInt(min < to ? min : to), Mathf.RoundToInt(to));
                property.vector2IntValue = vec;
            }
            else
                EditorGUI.HelpBox(ctrlRect, "MinTo is for Vector2!!", MessageType.Error);
        }

        public static Rect[] SplitRectIn3(Rect rect, int bordersSize, int space = 0)
        {
            var r = SplitRect(rect, 3);
            int pad = (int)r[0].width - bordersSize;
            int ps = pad + space;
            r[0].width = r[2].width -= ps;
            r[1].width += pad * 2;
            r[1].x -= pad;
            r[2].x += ps;
            return r;
        }
        public static Rect[] SplitRect(Rect a, int n)
        {
            Rect[] r = new Rect[n];
            for (int i = 0; i < n; ++i)
                r[i] = new Rect(a.x + a.width / n * i, a.y, a.width / n, a.height);
            return r;
        }
    }
#endif
}
