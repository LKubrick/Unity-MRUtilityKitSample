using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Fragilem17.MirrorsAndPortals
{
    /**
     * Used to contain the reference to the linked portal
     */
    [ExecuteInEditMode]
    public class Portal : MonoBehaviour
    {
        private static readonly Quaternion halfTurn = Quaternion.Euler(0.0f, 180.0f, 0.0f);

        [Tooltip("a reference to the Portal this Portal is looking out of.")]
        public Portal OtherPortal;

        [Space(10)]

        [Tooltip("The collider that will be disabled the moment we enter the Portal, the collider of the wall this portal is shot at.")]
        public Collider wallCollider;

        [Space(10)]

        [Tooltip("(autoFilled) a reference to the PortalSurface Component, this actually handles showing the portal")]
        [HideInInspector]
        public PortalSurface PortalSurface;

        [Tooltip("(autoFilled) a reference to a PortalTransporter Component, this handles transporting through it.")]
        [HideInInspector]
        public PortalTransporter PortalTransporter;

        [HideInInspector]
        public PortalRenderer MyRenderer;

        protected void OnEnable()
        {
#if UNITY_EDITOR
			ObjectFactory.componentWasAdded -= ObjectFactory_componentWasAdded;
			ObjectFactory.componentWasAdded += ObjectFactory_componentWasAdded;
#endif

            Initialise();
        }

        protected void Initialise()
		{
            //if (!PortalSurface)
            //{
            PortalSurface = GetComponentInChildren<PortalSurface>();
            //}
            //if (!PortalTransporter)
            //{
            PortalTransporter = GetComponentInChildren<PortalTransporter>();
            //}

            if (!PortalSurface)
            {
                Debug.LogWarning(PortalUtils.Colorize("[PORTALS] ", PortalUtils.DebugColors.Warn, true) + name + " has a Portal Component but no PortalSurface Component, add a PortalSurface Component to this or a child GameObject.");
            }


            if (!PortalTransporter)
            {
                Debug.LogWarning(PortalUtils.Colorize("[PORTALS] ", PortalUtils.DebugColors.Warn, true) + name + " has a Portal Component but no PortalTransporter Component, transporting through this portal won't be possible until you add a PortalTransporter Component.");
            }
            else
            {
                PortalTransporter.Initialise();
            }
        }

#if UNITY_EDITOR
        protected void OnDisable()
		{
            ObjectFactory.componentWasAdded -= ObjectFactory_componentWasAdded;
        }

        protected void ObjectFactory_componentWasAdded(Component obj)
		{
            //Debug.Log("ObjectFactory_componentWasAdded: " + obj.GetType().Name);
			if (obj.GetType().Name == "PortalSurface" || obj.GetType().Name == "PortalTransporter" || obj.GetType().Name == "RigidBody")
			{
               Initialise();
			}
        }
#endif

		public bool Place(Collider wallCollider, Vector3 pos, Quaternion rot)
        {
            this.wallCollider = wallCollider;
            transform.SetPositionAndRotation(pos, rot);
            gameObject.SetActive(true);

            // todo: can we be placed on this collider?

            return true;
        }

        public void SetOtherPortal(Portal portal) {
            OtherPortal = portal;
        }


        public void Remove()
        {
            gameObject.SetActive(false);
        }

        public void PortalRay(ref Ray ray)
        {
            if (PortalSurface && OtherPortal && OtherPortal.PortalSurface)
            {
                Transform _inTransform = PortalSurface.transform;
                Transform _outTransform = OtherPortal.PortalSurface.transform;

                Vector3 relativePos = _inTransform.InverseTransformPoint(ray.origin);
                relativePos = halfTurn * relativePos;
                ray.origin = _outTransform.TransformPoint(relativePos);

                // Update rotation of clone.
                Quaternion relativeRot = Quaternion.Inverse(_inTransform.rotation) * Quaternion.LookRotation(ray.direction, Vector3.up);
                relativeRot = halfTurn * relativeRot;
                Quaternion newRotation = _outTransform.rotation * relativeRot;
                ray.direction = newRotation * Vector3.forward;
            }
        }

        public void PortalTransform(ref Vector3 position, ref Quaternion rotation, ref float scale) 
        {
            Transform _inTransform = PortalSurface.transform;
            Transform _outTransform = OtherPortal.PortalSurface.transform;

            float scaleFactor = (OtherPortal.transform.lossyScale.x / transform.lossyScale.x);
            scale = scale * scaleFactor;

            // Update position of clone.
            Vector3 relativePos = _inTransform.InverseTransformPoint(position);
            relativePos = halfTurn * relativePos;
            position = _outTransform.TransformPoint(relativePos);

            // Update rotation of clone.
            Quaternion relativeRot = Quaternion.Inverse(_inTransform.rotation) * rotation;
            relativeRot = halfTurn * relativeRot;
            rotation = _outTransform.rotation * relativeRot;
            
        }
    }
}