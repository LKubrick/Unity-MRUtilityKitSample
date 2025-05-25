using Fragilem17.MirrorsAndPortals;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fragilem17.MirrorsAndPortals
{
    [ExecuteInEditMode]
    public class RayCastingExample : MonoBehaviour
    {
        public Transform ForwardGO;
        public LayerMask layerMask;
        public float maxDistance = 10;
        public LineRenderer LineRendererPrefab;
        private List<LineRenderer> LineRenderInstances;
        public int maxRecursions = 10;

        public bool ChangeColorOfPortalOnHit = false;
        public Color ColorToChangePortalTo = Color.white;

        private void OnEnable()
        {
            if (LineRendererPrefab)
            {
                LineRenderInstances = new List<LineRenderer>();
                LineRenderInstances.Add(LineRendererPrefab);
            }
        }

        private void OnDisable()
        {
            if (LineRenderInstances != null)
            {
                foreach (LineRenderer lr in LineRenderInstances)
                {
                    if (lr != LineRendererPrefab)
                    {
#if UNITY_EDITOR
                        DestroyImmediate(lr.gameObject);
#else
                    Destroy(lr.gameObject);
#endif
                    }
                }
                LineRenderInstances = null;
            }
        }

        void Update()
        {
            if (!LineRendererPrefab || !ForwardGO || LineRenderInstances == null || LineRenderInstances.Count == 0)
            {
                return;
            }

            HideLines();
            RayCast(ForwardGO.position, ForwardGO.forward, 0);
        }

        private void HideLines()
        {
            foreach (LineRenderer lr in LineRenderInstances)
            {
                if (lr)
                {
                    lr.gameObject.SetActive(false);
                }
            }
        }

        private void RayCast(Vector3 startPosition, Vector3 direction, int iteration)
        {
            if (iteration < maxRecursions)
            {

                Vector3 endPosition;
                Ray ray = new Ray(startPosition, direction);

                iteration = iteration + 1;

                if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, layerMask))
                {
                    endPosition = hit.point;

                    Portal portal = hit.collider.gameObject.GetComponent<Portal>();
                    if (portal)
                    {
                        // where the ray hits the portal we set the rays new original to be the hitPoint
                        ray.origin = endPosition;

                        // Then we portal the ray
                        portal.PortalRay(ref ray);

                        if (ChangeColorOfPortalOnHit)
                        {
                            portal.PortalSurface.MyMaterialInstance.SetColor("_Tint", ColorToChangePortalTo);
                        }

                        // The ray is now pointing outwards of the other portal
                        // we recurse in this function so we can try and find another portal
                        RayCast(ray.origin, ray.direction, iteration);
                    }
                }
                else
                {
                    // we didn't hit anything so we just add the endPos to be a meter from our startPos
                    endPosition = startPosition + (direction * 1f);
                }

                DrawLineRenderer(startPosition, endPosition, iteration);
                //Debug.DrawLine(startPosition, endPosition, Color.blue);
            }
        }

        private void DrawLineRenderer(Vector3 startPosition, Vector3 endPosition, int iteration)
        {
            iteration = iteration - 1;
            LineRenderer lineRenderer;
            if (LineRenderInstances.Count > iteration)
            {
                lineRenderer = LineRenderInstances[iteration];
            }
            else
            {
                lineRenderer = Instantiate<LineRenderer>(LineRendererPrefab, ForwardGO);
                lineRenderer.gameObject.hideFlags = HideFlags.DontSave;
                LineRenderInstances.Add(lineRenderer);
            }

            if (lineRenderer != null)
            {
                Vector3[] positions = new Vector3[] {
                    startPosition,
                    endPosition
                };

                lineRenderer.gameObject.SetActive(true);
                lineRenderer.SetPositions(positions);
            }
        }
    }
}
