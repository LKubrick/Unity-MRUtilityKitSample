using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Fragilem17.MirrorsAndPortals
{
    /**
     * Used to transport PortalableObjects to the linked portal defined in the portalSurface
     */
    [RequireComponent(typeof(PortalTransporter))]
    public class TransporterEventFilter : MonoBehaviour
    {

        [Tooltip("Any gameObject wich contains this string in the name will trigger the event.")]
        public String NameFilter = "*";

        private PortalTransporter _portalTransporter;
        [Space(10)]

        [Header("Events")]
        public UnityEvent<PortalableObject> OnObjectEnteredPortal;
        public UnityEvent<PortalableObject> OnObjectTransportedAwayFromHere;
        public UnityEvent<PortalableObject> OnObjectTransportedToHere;
        public UnityEvent<PortalableObject> OnObjectExitedPortal;


        protected void OnEnable()
        {
            _portalTransporter = GetComponent<PortalTransporter>();
            if (_portalTransporter == null) 
            {
                Debug.LogWarning("Could not find a PortalTransporter component");
                return;
            }

            _portalTransporter.OnObjectEnteredPortal.AddListener(OnObjectEnteredPortalEvent);
            _portalTransporter.OnObjectTransportedAwayFromHere.AddListener(OnObjectTransportedAwayFromHereEvent);
            _portalTransporter.OnObjectTransportedToHere.AddListener(OnObjectTransportedToHereEvent);
            _portalTransporter.OnObjectExitedPortal.AddListener(OnObjectExitedPortalEvent);
        }

		private void OnDisable()
        {
            _portalTransporter.OnObjectEnteredPortal.RemoveListener(OnObjectEnteredPortalEvent);
            _portalTransporter.OnObjectTransportedAwayFromHere.RemoveListener(OnObjectTransportedAwayFromHereEvent);
            _portalTransporter.OnObjectTransportedToHere.RemoveListener(OnObjectTransportedToHereEvent);
            _portalTransporter.OnObjectExitedPortal.RemoveListener(OnObjectExitedPortalEvent);
        }

		private void OnObjectEnteredPortalEvent(PortalableObject obj)
		{
			if (AllowedByFilter(obj))
			{
                OnObjectEnteredPortal.Invoke(obj);
            }
		}

		private void OnObjectTransportedAwayFromHereEvent(PortalableObject obj)
        {
            if (AllowedByFilter(obj))
            {
                OnObjectTransportedAwayFromHere.Invoke(obj);
            }
        }

		private void OnObjectTransportedToHereEvent(PortalableObject obj)
        {
            if (AllowedByFilter(obj))
            {
                OnObjectTransportedToHere.Invoke(obj);
            }
        }

		private void OnObjectExitedPortalEvent(PortalableObject obj)
        {
            if (AllowedByFilter(obj))
            {
                OnObjectExitedPortal.Invoke(obj);
            }
        }

		private bool AllowedByFilter(PortalableObject obj)
		{
			if (NameFilter == "" || NameFilter == "*")
			{
                return true;
			}

            if (obj.name.Contains(NameFilter))
			{
                return true;
			}

            return false;
		}
	}
}