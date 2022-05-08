using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniGame : MonoBehaviour
{
    //мини-игра, которую можно запускать в рамках действия Action
    public GameObject gameInterface;
    public Camera cameraView;
    public float cameraFocusTime = 1f;

    protected System.Action<int> onComplete;
    protected bool isGameStarted = false;
    protected bool isPlaying = false;

    protected Action action;  //действие, из которого была запущена игра
    Vector3 camBasePos;
    Quaternion camBaseRot;
    float camBaseFov;

    static List<MiniGame> registeredMiniGameList;

    public static void OnQuitApplication()
    {
        Debug.Log("MiniGame.OnQuitApplication");
        if (registeredMiniGameList != null)
            registeredMiniGameList.ForEach(x => {
                if (x != null && x.IsActive) 
                    x.AlarmRestoreCamera(); 
            });
    }

    static void Register(MiniGame miniGame)
    {
        if (registeredMiniGameList == null)
            registeredMiniGameList = new List<MiniGame>();
        else
            registeredMiniGameList.Remove(null);

        if (miniGame != null && !registeredMiniGameList.Contains(miniGame))
        {
            //Debug.Log("* * * * * * MiniGame.Register: " + miniGame.name);
            registeredMiniGameList.Add(miniGame);
        }
    }

    public bool IsActive
    {
        get
        {
            return isGameStarted;
        }
    }

    private void Awake()
    {
        //Debug.Log("* * * * * * MiniGame " + name + " Awake");
        Register(this);
        ShowGameInterface(false);
    }

    private void OnDestroy()
    {
        AlarmRestoreCamera();
    }

    private void OnApplicationQuit()
    {
        //AlarmRestoreCamera(); //вместо этого восстановление вызывается из Root.OnApplicationQuit
    }

    public void AlarmRestoreCamera()
    {
        //Debug.Log("! ! ! ! AlarmResoreCamera: camView=" + (cameraView != null ? cameraView.name : "null") + " cam=" + (Camera.main != null ? Camera.main.name : "null"));

        if (isGameStarted && cameraView != null)
        {
            isGameStarted = false;
            if (Camera.main != null)
            {
                FocusOn(camBasePos, camBaseRot, camBaseFov, null, 0);
                Account.SaveCurrentAvatarState();
            }
            else
            {
                Account.SetAvatarCameraState(camBasePos);
                Account.Save();
            }
        }
    }

    protected IEnumerator IEAcion(float delay, System.Action action)
    {
        yield return new WaitForSeconds(delay);
        if (action != null) action.Invoke();
    }

    protected void ShowGameInterface(bool isOn = true)
    {
        if (gameInterface != null) gameInterface.SetActive(isOn);
    }

    void FocusOn(Vector3 pos, Quaternion rot, float fov, string callback, float time = -1)
    {
        float dt = time >= 0 ? time : cameraFocusTime;
        var cam = Camera.main;
        if (cam != null)
        {
            if (dt > 0)
            {
                iTween.MoveTo(cam.gameObject, iTween.Hash("position", pos, "time", dt, "easetype", "linear", "oncompletetarget", gameObject, "oncomplete", callback));
                iTween.RotateTo(cam.gameObject, iTween.Hash("rotation", rot.eulerAngles, "time", dt, "easetype", "linear"));
                iTween.ValueTo(cam.gameObject, iTween.Hash("from", cam.fieldOfView, "to", fov, "onupdatetarget", gameObject, "onupdate", "SetCameraFOV"));
            }
            else
            {
                cam.transform.position = pos;
                cam.transform.rotation = rot;
                cam.fieldOfView = fov;
            }
        }
    }

    protected void SetCameraFOV(float fov)
    {
        Camera.main.fieldOfView = fov;
    }

    public void Play(Action action, System.Action<int> onComplete)
    {
        if (!isGameStarted)
        {
            //TouchObjectsController.Instance.Lock();
            this.action = action;

            isGameStarted = true;
            ShowGameInterface();
            this.onComplete = onComplete;
            if (cameraView != null)
            {
                Camera cam = Camera.main;
                camBasePos = cam.transform.position;
                camBaseRot = cam.transform.rotation;
                camBaseFov = cam.fieldOfView;

                LockCamera();
                if (cameraView != null)
                    FocusOn(cameraView.transform.position, cameraView.transform.rotation, cameraView.fieldOfView, "OnPlay");
                else
                    OnPlay();
            }
            else
                OnPlay();
        }
    }

    void LockCamera(bool isLocked = true)
    {
        var cam = Camera.main.GetComponent<CameraRotor3D>();
        cam.Lock(isLocked);
    }

    virtual protected void OnPlay()
    {
        isPlaying = true;
    }

    virtual protected void Finish(int result)   //result = -1 (fail), 0 (none), 1 (win)
    {
        Debug.Log(name + " Finish: result=" + result);

        if (isGameStarted)
        {
            isGameStarted = false;
            ShowGameInterface(false);

            if (cameraView != null)
                FocusOn(camBasePos, camBaseRot, camBaseFov, result > 0 ? "OnWin" : "OnFail");
            else
            {
                if (result > 0) OnWin();
                else OnFail();
            }
        }
    }

    void OnWin()
    {
        OnFinish(1);
    }

    void OnFail()
    {
        OnFinish(-1);
    }

    virtual protected void OnFinish(int result)
    {
        if (cameraView != null)
            LockCamera(false);

        if (onComplete != null) 
            onComplete.Invoke(result);
    }
}
