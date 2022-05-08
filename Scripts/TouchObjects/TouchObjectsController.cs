using System.Collections.Generic;
using UnityEngine;

public class TouchObjectState
{
    public TouchObject touchObject;
    public float timeTouchStart = -1;
    public float timeTapStart = -1;
    public Vector2 touchPoint;
}

public class TouchObjectsController : MonoBehaviour //управление нажатием на предметы
{
    public static TouchObjectsController Instance;
    public const float TapDuration = 0.5f;

    public delegate void OnGlobalTouchEvent(Vector2 pos);
    public event OnGlobalTouchEvent onGlobalTouch;

    public delegate void OnGlobalSlideEvent(Vector2 screenDV);
    public event OnGlobalSlideEvent onGlobalSlide;

    public delegate void OnGlobalReleaseEvent(Vector2 pos);
    public event OnGlobalReleaseEvent onGlobalRelease;

    public delegate void OnGlobalTapEvent(Vector2 pos);
    public event OnGlobalTapEvent onGlobalTap;

    public delegate void OnGlobalPinchPunchEvent(float rate);
    public event OnGlobalPinchPunchEvent onGlobalPinchPunch;

    public enum TouchState
    {
        None = 0,
        Mouse = 1,
        Touch = 2,
    }

    TouchState touchState = TouchState.None;
    TouchState touchStatePrev = TouchState.None;
    Vector2 touchPoint;
    Vector2 touchPointStart;
    List<TouchObjectState> touchedPrev = null;   //список TouchObject, на которые были нажато в предыдущем кадре
    List<TouchObject> registeredTouchObjects;
    bool isLocked = false;
    float touchTimeStart = -1;

    float tapDuration
    {
        get
        {
            return Mathf.Max(TapDuration, Time.deltaTime);
        }
    }

    public bool IsLocked
    {
        get
        {
            return isLocked;
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            touchedPrev = new List<TouchObjectState>();
        }
        else
        {
            Debug.LogError("TouchObjectController.Instance already exists!");
            DestroyImmediate(gameObject);
        }
    }

    public void RegisterTouchObject(TouchObject touchObject)
    {
        if (touchObject != null)
        {
            if (registeredTouchObjects == null) registeredTouchObjects = new List<TouchObject>();
            if (!registeredTouchObjects.Contains(touchObject)) registeredTouchObjects.Add(touchObject);
        }
    }

    public void Lock()
    {
        isLocked = true;
    }

    public void Unlock()
    {
        isLocked = false;
    }

    public TouchState GetTouchState()
    {
        return touchState;
    }

    public Vector2 GetTouchPointScreen()
    {
        return touchPoint;
    }

    public GameObject HitObject(float radius = 0)
    {
        return HitObject(touchPoint, radius);
    }

    public static Camera MainCamera
    {
        get
        {
            if (Camera.main != null) return Camera.main;
            if (Camera.allCamerasCount > 0) return Camera.allCameras[0];
            return null;
        }
    }

    public GameObject HitObject(Vector2 pointScreen, float radius = 0)
    {
        GameObject res = null;
        var camera = MainCamera;

        if (camera != null)
        {
            Ray touchBeam = camera.ScreenPointToRay(pointScreen);
            RaycastHit hit;

            if (radius > 0)
                Physics.SphereCast(touchBeam, radius, out hit);
            else
                Physics.Raycast(touchBeam, out hit);
            if (hit.collider != null) res = hit.collider.gameObject;
        }
        else Debug.LogError("no camera was found!");
        return res;
    }

    TouchState DefineTouchState()
    {
        if (Input.touchCount > 0) return TouchState.Touch;
        else if (Input.GetMouseButton(0)) return TouchState.Mouse;
        return TouchState.None;
    }

    TouchObjectState GetTouchedPrev(TouchObject touchObject)
    {
        if (touchedPrev != null)
            foreach (var t in touchedPrev)
                if (t.touchObject == touchObject)
                    return t;
        return null;
    }

    void UpdateTouchedObjects(List<TouchObject> touched)
    {
        if (touchedPrev != null)
        {
            foreach (var state in touchedPrev)
                if (state != null && state.touchObject.enabled && (touched == null || !touched.Contains(state.touchObject)))
                {
                    if (touchState == TouchState.None)
                    {
                        state.touchObject.OnRelease();
                        if (state.timeTapStart > 0 && Time.realtimeSinceStartup - state.timeTapStart < tapDuration && (lastTouchPoint - state.touchPoint).magnitude < Screen.width / 100f)
                            state.touchObject.OnTap();
                    }
                    else state.touchObject.OnTouchOut();
                }
        }

        if (touched != null)
        {
            var touchedPrevNew = new List<TouchObjectState>();
            float startTouchTime = Time.realtimeSinceStartup;
            float startTapTime = touchStatePrev == TouchState.None ? Time.realtimeSinceStartup : -1;

            foreach (var touchObject in touched)
                if (touchObject.enabled)
                {
                    touchObject.OnTouch();
                    var tp = GetTouchedPrev(touchObject);
                    if (tp != null) touchedPrevNew.Add(tp);
                    else
                    {
                        touchedPrevNew.Add(new TouchObjectState { touchObject = touchObject, timeTouchStart = startTouchTime, timeTapStart = startTapTime, touchPoint = touchPoint });
                        if (touchStatePrev == TouchState.None) touchObject.OnPress();
                    }
                }

            if (touchedPrev != null)
                touchedPrev.Clear();

            touchedPrev = touchedPrevNew;
        }
        else touchedPrev.Clear();
    }

    float screenDiagonal = -1;

    float ScreenDiagonal
    {
        get
        {
            if (screenDiagonal < 0)
                screenDiagonal = Mathf.Sqrt(Mathf.Pow(Screen.width, 2) + Mathf.Pow(Screen.height, 2));
            return screenDiagonal;
        }
    }

    float lastPinchPunchRate = -1;

    void UpdateTouches(List<Vector2> touchPoints)
    {
        List<TouchObject> touched = null;
        if (touchPoints != null && touchPoints.Count > 0)
        {
            //если это pinch-punch
            if (touchPoints.Count == 2 && onGlobalPinchPunch != null)
            {
                float dist = (touchPoints[0] - touchPoints[1]).magnitude;
                float rate = dist / ScreenDiagonal;
                if (lastPinchPunchRate > 0)
                    onGlobalPinchPunch.Invoke(rate - lastPinchPunchRate);
                lastPinchPunchRate = rate;
            }
            else
            {
                //обычное нажатие
                lastPinchPunchRate = -1;

                touchPoint = touchPoints[0];
                foreach (var point in touchPoints)
                {
                    GameObject obj = HitObject(point);

                    //если такой объект есть, то собираем его TouchObjects
                    if (obj != null)
                    {
                        TouchObject[] touchObjects = obj.GetComponents<TouchObject>();
                        if (touchObjects != null)
                        {
                            if (touched == null) touched = new List<TouchObject>();
                            touched.AddRange(touchObjects);
                            touchPoint = point;
                        }
                    }
                }
            }
        }
        UpdateTouchedObjects(touched);
    }

    void UpdateTouch()
    {
        //Debug.Log("UpdateTouch: touches=" + Input.touchCount);

        List<Vector2> touchPoints = null;

        if (Input.touchCount > 0)
        {
            touchPoints = new List<Vector2>();
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.touches[i];
                touchPoints.Add(touch.position);
            }
        }

        UpdateTouches(touchPoints);
    }

    void UpdateMouse()
    {
        //Debug.Log("UpdateMouseTouch: " + Input.GetMouseButton(0));
        List<Vector2> touchPoints = Input.GetMouseButton(0) ? new List<Vector2> { Input.mousePosition } : null;

        //если нажат Ctrl, то имитируем жест pinch-punch
        if (touchPoints != null && Input.GetKey(KeyCode.LeftControl))
        {
            float dx = Input.mousePosition.x - Screen.width / 2f;
            float dy = Input.mousePosition.y - Screen.height / 2f;

            float x = Screen.width / 2f - dx;
            float y = Screen.height / 2f - dy;
            touchPoints.Add(new Vector2 { x = x, y = y });
        }
        
        UpdateTouches(touchPoints);
    }

    Vector2 lastTouchPoint;

    void OnGlobalTouch()
    {
        if (registeredTouchObjects != null)
            foreach (var touchObject in registeredTouchObjects)
                if (touchObject != null)
                    touchObject.OnGlobalTouch();

        if (onGlobalTouch != null) onGlobalTouch.Invoke(touchPoint);
        if (Time.realtimeSinceStartup - touchTimeStart >= tapDuration || (touchPoint - lastTouchPoint).magnitude > Mathf.Min(Screen.height, Screen.width) / 10f )
        {
            if (onGlobalSlide != null) onGlobalSlide.Invoke(touchPoint - lastTouchPoint);
            lastTouchPoint = touchPoint;
        }
    }

    void OnGlobalRelease()
    {
        if (registeredTouchObjects != null && touchTimeStart > 0)
            foreach (var touchObject in registeredTouchObjects)
                if (touchObject != null)
                    touchObject.OnGlobalRelease();

        if (onGlobalRelease != null) onGlobalRelease.Invoke(touchPoint);
    }

    void Update()
    {
        touchStatePrev = touchState;
        touchState = DefineTouchState();

        if (touchState != TouchState.None)
        {
            //если ткнули в элемент интерфейса
            UnityEngine.EventSystems.EventSystem eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if ((eventSystem != null && eventSystem.currentSelectedGameObject != null) || isLocked)
            {
                touchState = TouchState.None;
                OnGlobalRelease();
                touchTimeStart = -1;
                return;
            }
        }

        switch (touchState)
        {
            case TouchState.Touch:
                UpdateTouch();
                touchState = TouchState.Touch;
                break;
            case TouchState.Mouse:
                UpdateMouse();
                touchState = TouchState.Mouse;
                break;
            default:
                touchState = TouchState.None;
                //UpdateTouchedObjects(null);
                lastPinchPunchRate = -1;
                break;
        }

        if (touchState != TouchState.None && lastPinchPunchRate < 0)
        {
            //если ткнули в экран
            if (touchTimeStart < 0)
            {
                touchTimeStart = Time.realtimeSinceStartup;
                touchPointStart = lastTouchPoint = touchPoint;
            }

            //if (lastPinchPunchRate < 0)
                OnGlobalTouch();
        }
        else
        {
            //отпустили палец от экрана
            UpdateTouchedObjects(null);

            if (touchTimeStart > 0 && onGlobalTap != null && Time.realtimeSinceStartup - touchTimeStart <= tapDuration)
            {
                float dist = Vector2.Distance(touchPoint, touchPointStart);
                if (dist < Mathf.Min(Screen.width, Screen.height) / 100)
                    onGlobalTap.Invoke(touchPoint);
            }

            OnGlobalRelease();
            touchTimeStart = -1;
        }
    }
}

