using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
#endif

namespace XDPaint.Demo
{
	public class CameraMover : MonoBehaviour
	{
		[SerializeField] private Transform target;
		[SerializeField] private float distance = 10.0f;
		[SerializeField] private float axisRatio = 0.02f;
		[SerializeField] private int minDistance = 3;
		[SerializeField] private int maxDistance = 10;

		private int fingerId = -1;
		private float x, y;
		private float defaultDistance;
		private readonly Vector2 axisMoveSpeedMouse = new Vector2(17f, 7f);
		private readonly Vector2 axisMoveSpeedTouch = new Vector2(17f, 7f);
		
		void Awake()
		{
			defaultDistance = distance;
#if ENABLE_INPUT_SYSTEM
			if (!EnhancedTouchSupport.enabled)
			{
				EnhancedTouchSupport.Enable();
			}
#endif
		}

		void Update()
		{
#if ENABLE_INPUT_SYSTEM
			if (Mouse.current != null)
			{
				distance += Mouse.current.scroll.y.ReadValue() * Time.deltaTime;
			}
#elif ENABLE_LEGACY_INPUT_MANAGER
			distance += Input.GetAxis("Mouse ScrollWheel");
#endif
			distance = Mathf.Clamp(distance, minDistance, maxDistance);
			
#if ENABLE_INPUT_SYSTEM
			if (Mouse.current != null)
			{
				if (Mouse.current.leftButton.isPressed)
				{
					x += Mouse.current.delta.x.ReadValue() * axisMoveSpeedMouse.x * axisRatio;
					y -= Mouse.current.delta.y.ReadValue() * axisMoveSpeedMouse.y * axisRatio;
				}
			}
#elif ENABLE_LEGACY_INPUT_MANAGER
			if (!Input.touchSupported || Input.mousePresent)
			{
				if (Input.GetMouseButton(0))
				{
					x += Input.GetAxis("Mouse X") * axisMoveSpeedMouse.x * axisRatio;
					y -= Input.GetAxis("Mouse Y") * axisMoveSpeedMouse.y * axisRatio;
				}
			}
#endif

#if ENABLE_INPUT_SYSTEM
			if (Touchscreen.current != null && Touch.activeTouches.Count > 0)
			{
				foreach (var touch in Touch.activeTouches)
				{
					if (touch.phase == TouchPhase.Began && fingerId == -1)
					{
						fingerId = touch.finger.index;
					}
					if (touch.finger.index == fingerId)
					{
						if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
						{
							x += touch.delta.x * axisMoveSpeedTouch.x * axisRatio;
							y -= touch.delta.y * axisMoveSpeedTouch.y * axisRatio;
						}
						if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
						{
							fingerId = -1;
						}
					}
				}
			}
#elif ENABLE_LEGACY_INPUT_MANAGER
			if (Input.touchSupported)
			{
				foreach (var touch in Input.touches)
				{
					if (touch.phase == TouchPhase.Began && fingerId == -1)
					{
						fingerId = touch.fingerId;
					}
					if (touch.fingerId == fingerId)
					{
						if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
						{
							x += Input.touches[fingerId].deltaPosition.x * axisMoveSpeedTouch.x * axisRatio;
							y -= Input.touches[fingerId].deltaPosition.y * axisMoveSpeedTouch.y * axisRatio;
						}
						if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
						{
							fingerId = -1;
						}
					}
				}
			}
#endif
			var rotation = Quaternion.Euler(y, x, 0);
			var position = rotation * Vector3.back * distance + target.position;
			transform.position = position;
			transform.rotation = rotation;
		}
		
		public void ResetCamera()
		{
			distance = defaultDistance;
			x = 0;
			y = 0;
			Update();
		}
	}
}