using UnityEngine;
using UnityEngine.EventSystems;

public enum DragState
{
    Idle = 0,       //лежит и никто его не трогает
    Begin = 1,  //начали тащить
    Release = 2,    //отпустили
    Back = 3,       //едет назад
}

public class ObjectDragUI : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    public delegate void OnDragEvent(DragState state, Vector2 mousePosition);
    public event OnDragEvent onDragEvent;

    public float speedBack = 0; //скорость, с которой возвращается назад

    private Vector2 lastMousePosition;
    private Vector2 startPosition;
    private Vector2 lastPosition;
    private float timeRelease = -1;
    private bool isBack = false;
    private RectTransform rectTransform;

    public Vector2 StartPosition
    {
        get
        {
            return startPosition;
        }
    }

    public Vector2 CurrentPosition
    {
        get
        {
            return rectTransform.position;
        }
    }

    public bool IsBusy
    {
        get
        {
            return isBack;
        }
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsBusy)
        {
            Debug.Log("Begin Drag");

            TouchObjectsController.Instance.Lock();
            lastMousePosition = eventData.position;
            startPosition = rectTransform.position;
            if (onDragEvent != null) onDragEvent.Invoke(DragState.Begin, lastMousePosition);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsBusy)
        {
            Vector2 currentMousePosition = eventData.position;
            Vector2 diff = currentMousePosition - lastMousePosition;
            RectTransform rect = rectTransform;// GetComponent<RectTransform>();

            Vector3 newPosition = rect.position + new Vector3(diff.x, diff.y, transform.position.z);
            Vector3 oldPos = rect.position;
            rect.position = newPosition;
            if (!IsRectTransformInsideSreen(rect))
            {
                rect.position = oldPos;
            }
            lastMousePosition = currentMousePosition;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!IsBusy)
        {
            Debug.Log("End Drag");

            TouchObjectsController.Instance.Unlock();
            lastPosition = rectTransform.position;
            if (speedBack > 0) isBack = true;
            timeRelease = Time.realtimeSinceStartup;

            if (onDragEvent != null) onDragEvent.Invoke(DragState.Release, eventData.position);
        }
    }

    private bool IsRectTransformInsideSreen(RectTransform rectTransform)
    {
        bool isInside = false;
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        int visibleCorners = 0;
        Rect rect = new Rect(0, 0, Screen.width, Screen.height);
        foreach (Vector3 corner in corners)
        {
            if (rect.Contains(corner))
            {
                visibleCorners++;
            }
        }
        if (visibleCorners == 4)
        {
            isInside = true;
        }
        return isInside;
    }

    public void SetBack()
    {
        transform.position = startPosition;
        isBack = false;
    }

    private void Update()
    {
        if (isBack)
        {
            Vector2 dv = lastPosition - startPosition;
            float dT = dv.magnitude / speedBack;
            float dt = Time.realtimeSinceStartup - timeRelease;
            if (dt < dT)
                rectTransform.position = lastPosition - dv * dt / dT;
            else
            {
                rectTransform.position = startPosition;
                isBack = false;
            }
        }
    }
}