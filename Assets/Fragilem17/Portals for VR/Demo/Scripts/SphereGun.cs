using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.XR;
#endif

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace Fragilem17.MirrorsAndPortals
{
    public class SphereGun : MonoBehaviour
    {
        public GameObject Prefab;
        public float force = 1;
        private float _delay = 0;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        public InputAction ShootAction1;

        private void OnEnable()
        {
            ShootAction1.Enable();
        }

        private void OnDisable()
        {
            ShootAction1.Disable();
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
    private UnityEngine.XR.InputDevice _leftHandDevice;

    void OnEnable()
    {
        List<InputDevice> allDevices = new List<InputDevice>();
        InputDevices.GetDevices(allDevices);
        foreach (InputDevice device in allDevices)
            InputDevices_deviceConnected(device);

        InputDevices.deviceConnected += InputDevices_deviceConnected;
    }

    private void OnDisable()
    {
        InputDevices.deviceConnected -= InputDevices_deviceConnected;
    }

    private void InputDevices_deviceConnected(InputDevice device)
    {
        bool discardedValue;
        if (device.TryGetFeatureValue(CommonUsages.primaryButton, out discardedValue))
        {
            if ((device.characteristics & InputDeviceCharacteristics.Left) == InputDeviceCharacteristics.Left)
            {
                _leftHandDevice = device;
            }
        }
    }
#endif

        // Update is called once per frame
        void Update()
        {
            _delay += Time.deltaTime;
            bool shoot = false;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            shoot = (ShootAction1 != null && ShootAction1.ReadValue<float>() > 0.25f && _delay > 0.5f);
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        float buttonValue = 1;
        shoot = ((Input.GetKey(KeyCode.Space) || (_leftHandDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out buttonValue) && buttonValue > 0.2f)) && _delay > 0.5f);
        //Debug.Log(buttonValue);
#endif

            if (shoot)
            {
                _delay = 0;
                GameObject s = Instantiate(Prefab, null, true);
                s.name = "SphereGunBullet";
                s.transform.position = transform.position + (transform.forward * 0.5f);
                s.transform.rotation = transform.rotation;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                s.GetComponent<Rigidbody>().AddForce(s.transform.forward * force * ShootAction1.ReadValue<float>(), ForceMode.Impulse);
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            s.GetComponent<Rigidbody>().AddForce(s.transform.forward * force * buttonValue, ForceMode.Impulse);
#endif
            }
        }
    }
}
