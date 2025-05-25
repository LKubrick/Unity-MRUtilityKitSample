using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Fragilem17.MirrorsAndPortals
{
	public class StraightenTrackingSpace : MonoBehaviour
	{
		public float speed = 1f;
		public bool useLateUpdate = true;
		public bool useLerp = true;


		private void Update()
		{
			if (!useLateUpdate)
			{
				Straighten();
			}
		}

		void LateUpdate()
		{
			if (useLateUpdate)
			{
				Straighten();
			}
		}

		private void Straighten()
		{
			Vector3 euler = transform.rotation.eulerAngles;

			if (useLerp)
			{
				transform.rotation = Quaternion.LerpUnclamped(transform.rotation, Quaternion.Euler(0, euler.y, 0), speed * Time.deltaTime);
			}
			else
			{
				transform.rotation = Quaternion.Euler(0, euler.y, 0);
			}
		}

	}
}
