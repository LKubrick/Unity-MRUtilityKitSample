using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fragilem17.MirrorsAndPortals
{
    [ExecuteInEditMode]
    public class PortalUtil_FollowTransform : MonoBehaviour
    {
        public Transform Target;
        public bool FollowRotation = true;

        void Update()
        {
            Follow();
        }

        public void Follow() {
            if (Target)
            {
                transform.position = Target.position;
				if (FollowRotation)
				{
                    transform.rotation = Target.rotation;
				}
            }
        }
    }
}