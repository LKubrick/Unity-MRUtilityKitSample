using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Fragilem17.MirrorsAndPortals
{
    [RequireComponent(typeof(Collider))]
    public class PortalableObject : MonoBehaviour
    {
        [Tooltip("Leave empty to target this gameObject, use this to transport a PlayerController as soon as the 'head' passes through.")]
        [FormerlySerializedAs("TransformToTransport")]
        public Transform TransformToPortal;

        [Tooltip("When true, this object will be transported by a portal when passing through it. unless PortalWithMasterPortalable is turned on, then it will only portal when the master is portalled. ")]
        [FormerlySerializedAs("AllowTransporting")]
        public bool PortallingEnabled = true;

        [Space(10)] // 10 pixels of spacing here.

        [Header("Events")]
        public UnityEvent<Portal> OnEnterPortalCollider;

        [FormerlySerializedAs("OnPreWarpEvent")]
        public UnityEvent<PortalableObject, Portal> OnPrePortalEvent;

        [FormerlySerializedAs("OnPostWarpEvent")]
        public UnityEvent<PortalableObject, Portal> OnPostPortalEvent;

        public UnityEvent<PortalableObject, Portal> OnExitPortalCollider;


        [Space(10)] // 10 pixels of spacing here.

        [Tooltip("Turn on if this is your playerController.")]
        public bool IsMasterPortalableObject;

        [Tooltip("When true, this object will transport keeping it's relative distance from the MasterPortalableObject (your PlayerController)")]
        [FormerlySerializedAs("PortalWithMasterPortalable")]
        public bool PortalAlongWithMasterPortalable;


        protected Portal _inPortal;
        protected TransformTargetOfPortalableObject _myTransformTargetOfPortalableObject;
        protected Rigidbody _rigidbody;
        protected CharacterController _characterController;
        protected Collider[] _colliders;
        public List<Rigidbody> RigidbodiesToPortal;

        [HideInInspector]
        public CloneRenderer[] childCloneRenderers;

        private static readonly Quaternion halfTurn = Quaternion.Euler(0.0f, 180.0f, 0.0f);

        private bool childOfAnotherPortalable = false;

        public static PortalableObject MasterPortalable;

        public bool EnablePortalAlongWithMasterPortalable
		{
			get => PortalAlongWithMasterPortalable; 
            set
			{
                PortalAlongWithMasterPortalable = value;
				/*if (PortalAlongWithMasterPortalable != value)
				{
				    PortalAlongWithMasterPortalable = value;
				    if (!PortalAlongWithMasterPortalable && _inPortal != null)
				    {
                        // if we're no longer gonna portal along, then make sure we exit the current portal
                        ExitPortal(_inPortal);
				    }
				}*/
			}
		}
		public bool SetPortallingEnabled { get => PortallingEnabled; set => PortallingEnabled = value; }

        [Tooltip("Assign a transform and it will always be positioned on the other side of the nearest portal relative to this object")]
        public Transform ClonePositionToClosestPortal;

        protected virtual void Awake()
        {
            //Debug.Log("AWAKE!! " + name);
            if (!TransformToPortal)
            {
                TransformToPortal = GetComponent<Transform>();
            }

            if (MasterPortalable != null && IsMasterPortalableObject && MasterPortalable != this)
            {
                Debug.LogError("There appears to be a second MasterPortalableObject in the scene, there should be only one! (" + name + ")");
            }

            if (IsMasterPortalableObject)
            {
                MasterPortalable = this;
            }


            _characterController = TransformToPortal.GetComponentInChildren<CharacterController>();

            _rigidbody = TransformToPortal.GetComponent<Rigidbody>();
            _colliders = TransformToPortal.GetComponentsInChildren<Collider>(true);
        }

        protected virtual void LateUpdate() 
        {
            if (ClonePositionToClosestPortal) 
            {
                Portal closest = PortalRenderer.FindClosestPortalInAllRenderers(transform.position);
            
                Transform _inTransform = closest.PortalSurface.transform;
                Transform _outTransform = closest.OtherPortal.PortalSurface.transform;

                float scaleFactor = (closest.OtherPortal.transform.lossyScale.x / closest.transform.lossyScale.x);
                ClonePositionToClosestPortal.transform.localScale = transform.lossyScale * scaleFactor;

                // Update position of clone.
                Vector3 relativePos = _inTransform.InverseTransformPoint(transform.position);
                relativePos = halfTurn * relativePos;
                ClonePositionToClosestPortal.transform.position = _outTransform.TransformPoint(relativePos);

                // Update rotation of clone.
                Quaternion relativeRot = Quaternion.Inverse(_inTransform.rotation) * transform.rotation;
                relativeRot = halfTurn * relativeRot;
                ClonePositionToClosestPortal.transform.rotation = _outTransform.rotation * relativeRot;
            }
        }

        protected void OnEnable()
        {
            // todo, if we change the TransformToTransport pointer, we need to remove the target below
            _myTransformTargetOfPortalableObject = TransformToPortal.GetComponent<TransformTargetOfPortalableObject>();
            if (_myTransformTargetOfPortalableObject == null)
            {
                _myTransformTargetOfPortalableObject = TransformToPortal.gameObject.AddComponent<TransformTargetOfPortalableObject>();
            }
            _myTransformTargetOfPortalableObject.PortalableObject = this;

            if (IsMasterPortalableObject)
            {
                MasterPortalable = this;
            }

            if (MasterPortalable != null && MasterPortalable != this)
            {
                MasterPortalable.OnEnterPortalCollider.RemoveListener(OnMasterPortalEnterPortalCollider);
                MasterPortalable.OnExitPortalCollider.RemoveListener(OnMasterPortalExitPortalCollider);
                MasterPortalable.OnPrePortalEvent.RemoveListener(OnMasterPortalablePreWarp);
                MasterPortalable.OnPostPortalEvent.RemoveListener(OnMasterPortalableWarped);

                //Debug.Log("ADDING LISTENER! " + name);
                MasterPortalable.OnEnterPortalCollider.AddListener(OnMasterPortalEnterPortalCollider);
                MasterPortalable.OnExitPortalCollider.AddListener(OnMasterPortalExitPortalCollider);
                MasterPortalable.OnPrePortalEvent.AddListener(OnMasterPortalablePreWarp);
                MasterPortalable.OnPostPortalEvent.AddListener(OnMasterPortalableWarped);
			}

            OnTransformParentChanged();
        }

		protected virtual void Start()
        {
            // Awake and OnEnable are called together, Start is called when all other components' Awake and OnEnable have been Called
            // only here are we sure a MasterPortalable exists.
            OnEnable();
        }

        protected void OnDisable()
        {
            if (MasterPortalable != null && MasterPortalable != this)
            {
                MasterPortalable.OnEnterPortalCollider.RemoveListener(OnMasterPortalEnterPortalCollider);
                MasterPortalable.OnExitPortalCollider.RemoveListener(OnMasterPortalExitPortalCollider);
                MasterPortalable.OnPrePortalEvent.RemoveListener(OnMasterPortalablePreWarp);
                MasterPortalable.OnPostPortalEvent.RemoveListener(OnMasterPortalableWarped);
            }
            if (IsMasterPortalableObject)
            {
                MasterPortalable = null;
            }
        }


        protected virtual void OnMasterPortalExitPortalCollider(PortalableObject masterObject, Portal portal)
        {
        }

        protected virtual void OnMasterPortalEnterPortalCollider(Portal portal)
        {
			if (PortalAlongWithMasterPortalable)
			{
                SetIsInPortal(portal);
			}
        }

        protected virtual void OnMasterPortalablePreWarp(PortalableObject masterObject, Portal fromPortal)
        {
            if (PortalAlongWithMasterPortalable)
            {
                PreWarp(fromPortal);
            }
        }

        protected virtual void OnMasterPortalableWarped(PortalableObject masterObject, Portal fromPortal)
        {
            if (PortalAlongWithMasterPortalable)
            {
                //Debug.Log("OnMasterPortalableWarped! i will go along " + name);
                PortalFrom(fromPortal, true);
            }
        }

        protected virtual void OnTransformParentChanged() 
        {
            CheckForParentPortalables();
        }

        protected void CheckForParentPortalables()
        {
            // check for the existance of a parent PortalableObject
            TransformTargetOfPortalableObject[] parentPortalables = GetComponentsInParent<TransformTargetOfPortalableObject>();
            //Debug.Log("check OnTransformParentChanged: " + name + " :" + parentPortalables.Length);
            childOfAnotherPortalable = (parentPortalables.Length > 1);
        }

        public virtual void SetIsInPortal(Portal portal)
        {
            ExitPortal(portal.OtherPortal);

            _inPortal = portal;
            OnEnterPortalCollider?.Invoke(portal);
            if (PortallingEnabled) // !childOfAnotherPortalable &&
            {
                //Debug.Log("SetIsInPortal " + this.name + " inPortal: " + portal.name + " turning OFF colliders ");
                if (portal.wallCollider)
                {
                    // if that's the case.. then we can turn of the colliders
                    // disable collisions with other portal
                    _colliders = TransformToPortal.GetComponentsInChildren<Collider>(true);
                    for (int i = 0; i < _colliders.Length; i++)
                    {
                        Physics.IgnoreCollision(_colliders[i], portal.wallCollider);
                    }
                }
            }
        }

        public virtual void ExitPortal(Portal portal)
        {
			if (_inPortal == portal)
			{
                _inPortal = null;
            }

            OnExitPortalCollider?.Invoke(this, portal);
            //Debug.Log("ExitPortal " + this.name + " inPortal: " + portal.name + " turning ON colliders ");


            if (portal.wallCollider)
            {
                for (int i = 0; i < _colliders.Length; i++)
                {
                    Physics.IgnoreCollision(_colliders[i], portal.wallCollider, false);
                }
            }
        }


		protected virtual void PreWarp(Portal fromPortal)
        {
            //Debug.Log("  PRE: " + name);
            OnPrePortalEvent.Invoke(this, fromPortal);
            if (_characterController)
            {
                _characterController.enabled = false;
            }

        }
        protected virtual void PostWarp(Portal fromPortal)
        {
            //Debug.Log("POST: " + name);
            OnPostPortalEvent.Invoke(this, fromPortal);
            //Debug.Log("PortalableObject postWarp, gonna triggered teleport function in HVR");
            if (_characterController)
            {
                _characterController.enabled = true;
            }


            // instant move clones to other portal
            childCloneRenderers = FindChildCloneRenderers();
            for (int i = 0; i < childCloneRenderers.Length; i++)
            {
                fromPortal.PortalTransporter.cloneObjects.Remove(childCloneRenderers[i]);
                childCloneRenderers[i].ExitPortal(fromPortal, false);

                Collider[] myColliders = childCloneRenderers[i].GetComponents<Collider>();
                foreach (Collider c in myColliders) {

                    //Debug.DrawLine(childCloneRenderers[i].transform.position, fromPortal.OtherPortal.PortalTransporter.transform.position, Color.cyan, 10);                    

                    Vector3 direction;
                    float distance;
                    bool overlapped = Physics.ComputePenetration(

                    c, childCloneRenderers[i].transform.position, childCloneRenderers[i].transform.rotation,
                    fromPortal.OtherPortal.PortalTransporter.MyCollider, fromPortal.OtherPortal.PortalTransporter.transform.position, fromPortal.OtherPortal.PortalTransporter.transform.rotation,
                    out direction, out distance);

                    //Debug.Log(childCloneRenderers[i].name + " : " + direction + " : " + distance + " : " + overlapped);

                    if (overlapped)
                    {
                        //Debug.Log("AFTER WARP " + childCloneRenderers[i].name + " WOULD BE TOUCHING SO SET ME IN PORTAL: " + fromPortal.OtherPortal.name);
                        childCloneRenderers[i].SetIsInPortal(fromPortal.OtherPortal, false);
                        fromPortal.OtherPortal.PortalTransporter.cloneObjects.Add(childCloneRenderers[i]);
                        break;
					}
					/*else
					{
                        Debug.Log("AFTER WARP not touching new portal: " + fromPortal.OtherPortal.name);
                    }*/
                }
            }

        }


		protected virtual CloneRenderer[] FindChildCloneRenderers()
		{
            return TransformToPortal.GetComponentsInChildren<CloneRenderer>(true);
        }

		public virtual bool CanPortal() {

            //Debug.Log("AllowTransporting canWarp?: " + name + " :" + AllowTransporting);
            return (!childOfAnotherPortalable && PortallingEnabled && !PortalAlongWithMasterPortalable);
        }

        public virtual bool PortalFrom(Portal fromPortal, bool force = false)
        {
            if (!force && !CanPortal())
            {
                return false;
            }

            //Debug.Log("WARP " + name + " from " + fromPortal.name + " childOfAnotherPortalable: " + childOfAnotherPortalable + " force: " + force);

            // if we're forced, that means the Master got us here and a preWarp has already been done
            if (!force) { 
                PreWarp(fromPortal);
            }

            if (force)
            {
                // master got us here.. we might have entered through physics.. but on the wrong side of the portal therefor not adding to the list in transporter
                if (!fromPortal.OtherPortal.PortalTransporter.portalableObjects.Contains(this))
                {
                    //Debug.Log("(from master) Adding to portal list of " + fromPortal.OtherPortal.name + " object:" + this.name);
                    fromPortal.OtherPortal.PortalTransporter.portalableObjects.Add(this);
                }
            }

            // disable collisions with other portal
            if (fromPortal.OtherPortal.wallCollider)
            {
                for (int i = 0; i < _colliders.Length; i++)
                {
                    Physics.IgnoreCollision(_colliders[i], fromPortal.OtherPortal.wallCollider);
                }
            }

            var inTransform = fromPortal.PortalSurface.transform;
            var outTransform = fromPortal.OtherPortal.PortalSurface.transform;

            // van scale p1.0 to p0.5 = player 0.5 = portal out / portal in 
            // van scale p0.5 to p1.0 = player 2 = portal out / portal in 
            float scaleFactor = (fromPortal.OtherPortal.transform.lossyScale.x / fromPortal.transform.lossyScale.x);


            Vector3 originalScaleBeforeWarp = TransformToPortal.localScale;
            TransformToPortal.localScale = TransformToPortal.localScale * scaleFactor;


            // Position the camera behind the other portal.
            Vector3 relativePos = inTransform.InverseTransformPoint(TransformToPortal.position);
            relativePos = halfTurn * relativePos;
            TransformToPortal.position = outTransform.TransformPoint(relativePos);

            // Rotate the camera to look through the other portal.
            Quaternion relativeRot = Quaternion.Inverse(inTransform.rotation) * TransformToPortal.rotation;
            relativeRot = halfTurn * relativeRot;
            TransformToPortal.rotation = outTransform.rotation * relativeRot;

            // Update velocity of rigidbody.
            if (_rigidbody)
            {
                Vector3 relativeVel = inTransform.InverseTransformDirection(_rigidbody.linearVelocity);
                relativeVel = halfTurn * relativeVel;
                _rigidbody.linearVelocity = outTransform.TransformDirection(relativeVel);

                Vector3 relativeAngVel = inTransform.InverseTransformDirection(_rigidbody.angularVelocity);
                relativeAngVel = halfTurn * relativeAngVel;
                _rigidbody.angularVelocity = outTransform.TransformDirection(relativeAngVel);

                _rigidbody.position = TransformToPortal.position; // so it works for RB with interpolation
                _rigidbody.rotation = TransformToPortal.rotation; // so it works for RB with interpolation
            }

            if (RigidbodiesToPortal.Count > 0)
            {
                foreach (Rigidbody rb in RigidbodiesToPortal) 
                {
                    if (rb && rb != _rigidbody)
                    {
                        Vector3 relativeVel = inTransform.InverseTransformDirection(rb.linearVelocity);
                        relativeVel = halfTurn * relativeVel;
                        rb.linearVelocity = outTransform.TransformDirection(relativeVel);

                        Vector3 relativeAngVel = inTransform.InverseTransformDirection(rb.angularVelocity);
                        relativeAngVel = halfTurn * relativeAngVel;
                        rb.angularVelocity = outTransform.TransformDirection(relativeAngVel);
                    }
                }
            }

            PostWarp(fromPortal);

            SendMessage("OnWarped", transform.rotation, SendMessageOptions.DontRequireReceiver);
            return true;
        }
    }
}