using System;
using UnityEngine;
using UnityEngine.Events;

public class TouchObject : MonoBehaviour    //предмет, реагирующий на нажатия пользователя
{
    public static Vector2 NULL_VECTOR = new Vector2(float.MaxValue, float.MaxValue);

    [Serializable]
    public class OnItemTap : UnityEvent<TouchObject>  {}
    public class OnItemRelease : UnityEvent<TouchObject> { }

    public OnItemTap onItemTap;
    public OnItemRelease onItemRelease;

    public Camera MainCamera
    {
        get
        {
            return TouchObjectsController.MainCamera;
        }
    }

    public Vector2 TouchPosScreen
    {
        get
        {
            if (TouchObjectsController.Instance != null)
                return TouchObjectsController.Instance.GetTouchPointScreen();
            return NULL_VECTOR;
        }
    }

    public static bool IsGlobalTouch()
    {
        return Input.touchCount > 0 || Input.GetMouseButtonDown(0);
    }

    virtual protected void Awake()
    {
        //if (onItemTap == null) onItemTap = new OnItemTap();
        //if (onItemRelease == null) onItemRelease = new OnItemRelease();
    }

    virtual protected void Start()
    {
        if (TouchObjectsController.Instance != null)
            TouchObjectsController.Instance.RegisterTouchObject(this);
        else
            Debug.LogError("No TouchObjectsController on the scene!");
    }

    virtual public void OnTouch()   //прикоснулись к объекту (нажали или провели по нему пальцем/мышкой)
    {
        //Debug.Log(name + " OnTouch");
    }

    virtual public void OnRelease()
    {
        //Debug.Log(name + " OnRelease");
        if (onItemRelease != null) onItemRelease.Invoke(this);
    }

    virtual public void OnTouchOut()
    {
        //Debug.Log(name + " OnTouchOut");
    }

    virtual public void OnPress()   //нажали на объект
    {
        //Debug.Log(name + " OnPress");
    }

    virtual public void OnTap() //нажали и отпустили
    {
        //Debug.Log(name + " OnPress");
        if (onItemTap != null) onItemTap.Invoke(this);
    }

    virtual public void OnGlobalTouch()
    {

    }

    virtual public void OnGlobalRelease()
    {

    }

    virtual public void OnGlobalPinchPunch(float dist)
    {

    }
}
