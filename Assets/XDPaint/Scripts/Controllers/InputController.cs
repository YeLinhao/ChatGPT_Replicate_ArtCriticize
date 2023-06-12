// #define XDPAINT_VR_ENABLE

#if XDPAINT_VR_ENABLE
using System.Collections.Generic;
using UnityEngine.XR;
using InputDevice = UnityEngine.XR.InputDevice;
using CommonUsages = UnityEngine.XR.CommonUsages;
#endif

using System;
using UnityEngine;
using XDPaint.Tools;
using XDPaint.Utils;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
#endif

namespace XDPaint.Controllers
{
	public class InputController : Singleton<InputController>
	{
		[Header("Ignore Raycasts Settings")]
		[SerializeField] private Canvas canvas;
		[SerializeField] private GameObject[] ignoreForRaycasts;
		
		[Header("VR Settings")]
		public Transform PenTransform;

		public event Action OnUpdate;
		public event Action<Vector3> OnMouseHover;
		public event Action<Vector3, float> OnMouseDown;
		public event Action<Vector3, float> OnMouseButton;
		public event Action<Vector3> OnMouseUp;
		
		public Canvas Canvas => canvas;
		public GameObject[] IgnoreForRaycasts => ignoreForRaycasts;

		public Camera Camera { private get; set; }
		private int fingerId = -1;
		private bool isVRMode;
		
#if XDPAINT_VR_ENABLE
		private List<InputDevice> leftHandedControllers;
		private bool isPressed;
#endif
		
#if UNITY_WEBGL
		private bool isWebgl = true;
#else
		private bool isWebgl = false;
#endif

		void Start()
		{
			isVRMode = Settings.Instance.IsVRMode;
			InitVR();
#if ENABLE_INPUT_SYSTEM
			if (!EnhancedTouchSupport.enabled)
			{
				EnhancedTouchSupport.Enable();
			}
#endif
		}

		private void InitVR()
		{
#if XDPAINT_VR_ENABLE
			leftHandedControllers = new List<InputDevice>();
			var desiredCharacteristics = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller;
			InputDevices.GetDevicesWithCharacteristics(desiredCharacteristics, leftHandedControllers);
#endif
		}
		
		void Update()
		{
			//VR
			if (isVRMode)
			{
#if XDPAINT_VR_ENABLE
				OnUpdate?.Invoke();

				Vector3 screenPoint;
				if (OnMouseHover != null)
				{
					screenPoint = Camera.WorldToScreenPoint(PenTransform.position);
					OnMouseHover(screenPoint);
				}
				
				//VR input
				//next line can be changed for VR device input
				if (leftHandedControllers.Count > 0 && leftHandedControllers[0].TryGetFeatureValue(CommonUsages.triggerButton, out var triggerValue) && triggerValue)
				{
					if (!isPressed)
					{
						isPressed = true;
						if (OnMouseDown != null)
						{
							screenPoint = Camera.WorldToScreenPoint(PenTransform.position);
							OnMouseDown(screenPoint);
						}
					}
					else
					{
						if (OnMouseButton != null)
						{
							screenPoint = Camera.WorldToScreenPoint(PenTransform.position);
							OnMouseButton(screenPoint);
						}
					}
				}
				else if (isPressed)
				{
					isPressed = false;
					if (OnMouseUp != null)
					{
						screenPoint = Camera.WorldToScreenPoint(PenTransform.position);
						OnMouseUp(screenPoint);
					}
				}
#endif
			}
			else
			{
				//Pen / Touch / Mouse
#if ENABLE_INPUT_SYSTEM
				if (Pen.current != null && (Pen.current.press.isPressed || Pen.current.press.wasReleasedThisFrame))
				{
					if (Pen.current.press.isPressed)
					{
						OnUpdate?.Invoke();

						var pressure = Settings.Instance.PressureEnabled ? Pen.current.pressure.ReadValue() : 1f;
						var position = Pen.current.position.ReadValue();

						if (Pen.current.press.wasPressedThisFrame)
						{
							OnMouseDown?.Invoke(position, pressure);
						}

						if (!Pen.current.press.wasPressedThisFrame)
						{
							OnMouseButton?.Invoke(position, pressure);
						}
					}
					else if (Pen.current.press.wasReleasedThisFrame)
					{
						var position = Pen.current.position.ReadValue();
						OnMouseUp?.Invoke(position);
					}
				}
				else if (Touchscreen.current != null && Touch.activeTouches.Count > 0 && !isWebgl)
				{
					foreach (var touch in Touch.activeTouches)
					{
						OnUpdate?.Invoke();

						var pressure = Settings.Instance.PressureEnabled ? touch.pressure : 1f;

						if (touch.phase == TouchPhase.Began && fingerId == -1)
						{
							fingerId = touch.finger.index;
							OnMouseDown?.Invoke(touch.screenPosition, pressure);
						}

						if (touch.finger.index == fingerId)
						{
							if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
							{
								OnMouseButton?.Invoke(touch.screenPosition, pressure);
							}

							if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
							{
								fingerId = -1;
								OnMouseUp?.Invoke(touch.screenPosition);
							}
						}
					}
				}
				else if (Mouse.current != null)
				{
					OnUpdate?.Invoke();

					var mousePosition = Mouse.current.position.ReadValue();
					OnMouseHover?.Invoke(mousePosition);

					if (Mouse.current.leftButton.wasPressedThisFrame)
					{
						OnMouseDown?.Invoke(mousePosition, 1f);
					}

					if (Mouse.current.leftButton.isPressed)
					{
						OnMouseButton?.Invoke(mousePosition, 1f);
					}

					if (Mouse.current.leftButton.wasReleasedThisFrame)
					{
						OnMouseUp?.Invoke(mousePosition);
					}
				}
#elif ENABLE_LEGACY_INPUT_MANAGER
				//Touch / Mouse
				if (Input.touchSupported && Input.touchCount > 0 && !isWebgl)
				{
					foreach (var touch in Input.touches)
					{
						OnUpdate?.Invoke();

						var pressure = Settings.Instance.PressureEnabled ? touch.pressure : 1f;
			
						if (touch.phase == TouchPhase.Began && fingerId == -1)
						{
							fingerId = touch.fingerId;
							OnMouseDown?.Invoke(touch.position, pressure);
						}

						if (touch.fingerId == fingerId)
						{
							if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
							{
								OnMouseButton?.Invoke(touch.position, pressure);
							}
							
							if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
							{
								fingerId = -1;
								OnMouseUp?.Invoke(touch.position);
							}
						}
					}
				}
				else
				{
					OnUpdate?.Invoke();

					OnMouseHover?.Invoke(Input.mousePosition);

					if (Input.GetMouseButtonDown(0))
					{
						OnMouseDown?.Invoke(Input.mousePosition, 1f);
					}

					if (Input.GetMouseButton(0))
					{
						OnMouseButton?.Invoke(Input.mousePosition, 1f);
					}

					if (Input.GetMouseButtonUp(0))
					{
						OnMouseUp?.Invoke(Input.mousePosition);
					}
				}
#endif
			}
		}
	}
}