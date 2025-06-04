using Fragilem17.MirrorsAndPortals;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fragilem17.MirrorsAndPortals
{
    [ExecuteInEditMode]
    public class PortalAffectorSphere : MonoBehaviour
    {
        public List<MeshRenderer> renderers;

        private static List<string> _shaderNames = new List<string>(new string[] { "_SphereParams1", "_SphereParams2", "_SphereParams3" });
        //private static int _portalAffectorsInUse = 0;
        private string _myShaderName = "unset";

        private Portal _inPortal;
        private Vector3 _originalPortalScaleIn;
        private Vector3 _originalPortalScaleOut;
        private static readonly Quaternion halfTurn = Quaternion.Euler(0.0f, 180.0f, 0.0f);

        private void OnEnable()
        {
            if (_shaderNames.Count == 0)
            {
                Debug.LogWarning("a maximum of 3 portal affectors will have an effect on the portal shader");
                return;
            }

            _myShaderName = _shaderNames[0];
            _shaderNames.RemoveAt(0);
        }

        private void OnDisable()
        {
            if (_myShaderName != "unset" && !_shaderNames.Contains(_myShaderName))
            {
                _shaderNames.Add(_myShaderName);
            }
        }

        private void Update()
        {

        }

        private void LateUpdate()
        {
            if (_inPortal)
            {
                Vector3 p = transform.position;
                _inPortal.PortalSurface.myMeshRenderer.sharedMaterial.SetVector(_myShaderName, new Vector4(p.x, p.y, p.z, transform.lossyScale.x));
            }
            /*if (renderers != null && renderers.Count > 0)
            {
                foreach (MeshRenderer r in renderers)
                {
                    Vector3 p = transform.position;
                    if (r != null && r.sharedMaterial != null)
                    {
                        r.sharedMaterial.SetVector(_myShaderName, new Vector4(p.x, p.y, p.z, transform.lossyScale.x));
                    }
                }

                //Debug.Log(transform.parent.name + "used " + _shaderNames[_usedShaderSlotIndex]);
                //PositionClone();
            }*/
        }

        private void PositionClone()
        {
            if (_inPortal != null)
            {
                Transform inTransform = _inPortal.PortalSurface.transform;
                Transform outTransform = _inPortal.OtherPortal.PortalSurface.transform;

                float scaleFactor = (_inPortal.OtherPortal.transform.lossyScale.x / _inPortal.transform.lossyScale.x);


                _originalPortalScaleIn = inTransform.localScale;
                _originalPortalScaleOut = outTransform.localScale;

                inTransform.localScale = Vector3.one;
                outTransform.localScale = Vector3.one;


                // Update position of clone.
                Vector3 relativePos = inTransform.InverseTransformPoint(transform.position);
                relativePos = halfTurn * relativePos;
                Vector3 p = outTransform.TransformPoint(relativePos);

                _inPortal.OtherPortal.PortalSurface.myMeshRenderer.sharedMaterial.SetVector(_myShaderName, new Vector4(p.x, p.y, p.z, transform.lossyScale.x));
                
                inTransform.localScale = _originalPortalScaleIn;
                outTransform.localScale = _originalPortalScaleOut;
            }
        }

        public void SetIsInPortal(Portal portal)
        {
            //Debug.Log("SetIsInPortal " + transform.parent.name + " inPortal: " + portal.name);
            _inPortal = portal;
            if (portal.PortalSurface && !renderers.Contains(portal.PortalSurface.myMeshRenderer))
            {
                renderers.Add(portal.PortalSurface.myMeshRenderer);
            }
            //PositionClone();
        }

        public void ExitPortal(Portal portal)
        {
            //Debug.Log("ExitPortal " + transform.parent.name + " inPortal: " + portal.name);
            if (portal.PortalSurface)
            {
                Vector3 p = new Vector3(999f, 999f, 999f);
                portal.PortalSurface.myMeshRenderer.sharedMaterial.SetVector(_myShaderName, new Vector4(p.x, p.y, p.z, 0.0001f));
                
                _inPortal = null;
                renderers.Remove(portal.PortalSurface.myMeshRenderer);
            }
        }
    }
}