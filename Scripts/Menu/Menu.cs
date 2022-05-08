using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
	public Text		textMessage;
	public float	timeOpen = 0.5f;
	public float	timeClose = 0.5f;

	public AudioClip	onOpenSound;
	public AudioClip	onCloseSound;
	public GameObject	sfxPlayer;		//если null, то отыгрывает на текущем gameObject

    public GameObject buttonOk;
    public GameObject buttonCancel;

	public string triggerOn = "on";
	public string triggerOff = "off";
	public bool onKillActionOnSwitchOff = false;    //true - onKillAction срабатывает не после закрытия меню, а сразу в SwitchOff
	public bool lockTouchObjects = true;

	public System.Action onOkAction;
	public System.Action onCancelAction;

	protected System.Action onKillAction;
	protected bool isOn = false;
	protected Animator _animator;
	protected Animator animator
	{
		get
		{
			if (_animator == null) _animator = GetComponent<Animator>();
			return _animator;
		}
	}

	IEnumerator ieSwitchOn;
	IEnumerator ieSwitchOff;
	bool		isBusy = false;

	public bool IsBusy
    {
		get
        {
			return isBusy;
        }
    }

	void Awake()
	{
		Init();
	}

	virtual protected void Start ()
	{
	}

	void CheckIENumerator()
	{
		if (ieSwitchOff != null)
		{
			StopCoroutine(ieSwitchOff);
			SwitchOffComplete();
		}

		if (ieSwitchOn != null)
		{
			StopCoroutine(ieSwitchOn);
			SwitchOnComplete();
		}
	}

	void OnDisable()
	{
		CheckIENumerator();

		if (animator != null)
		{
			isBusy = false;
			isOn = false;
		}
	}
		
	void OnDestroy()
	{
		CheckIENumerator();
	}

	virtual protected void Init()
	{
	}

	public bool IsOn
	{
		get
		{
			return isOn;
		}
	}

    protected void CheckButtons()
    {
        if (buttonOk != null) buttonOk.SetActive(onOkAction != null);
        if (buttonCancel != null) buttonCancel.SetActive(onCancelAction != null);
    }

	virtual public void SwitchOn(string message, System.Action onOkAction, System.Action onCancelAction)
	{
		this.textMessage.text = message;
		SwitchOn(onOkAction, onCancelAction);
	}

	public void SwitchOn(System.Action onOkAction, System.Action onCancelAction)
	{
		this.onOkAction = onOkAction;
		this.onCancelAction = onCancelAction;
		SwitchOn();
	}

	virtual public void SwitchOn()
	{
		if (isOn || isBusy)
			return;

		if (!gameObject.activeInHierarchy)
		{
			Debug.LogWarning(name + " disabled!");
			return;
		}

		if (lockTouchObjects && TouchObjectsController.Instance != null) TouchObjectsController.Instance.Lock();
		if (!isOn && animator != null) animator.SetTrigger(triggerOn);
		isOn = true;
		isBusy = true;
		OnOpen();
		AddMenu(this);
		PlayAudio(onOpenSound);
        CheckButtons();

		ieSwitchOn = IESwitchOnComplete();
		StartCoroutine(ieSwitchOn);
	}

	IEnumerator IESwitchOnComplete()
	{
		yield return new WaitForSeconds(timeOpen);
		ieSwitchOn = null;
		SwitchOnComplete();
	}

	void SwitchOnComplete()
	{
		isBusy = false;
		OnOpenComplete();
	}

	public void SwitchOff(System.Action onKillAction)
	{
		SetOnKillAction(onKillAction);
		SwitchOff();
	}

	public void SwitchOff()
	{
		if (!isOn || isBusy)
			return;
		
		if (animator != null)
		{
			isBusy = true;
			animator.SetTrigger(triggerOff);
		}

		OnClose();
		PlayAudio(onCloseSound);
		ieSwitchOff = IESwitchOffComplete();
		StartCoroutine(ieSwitchOff);

		if (onKillActionOnSwitchOff)
        {
			if (onKillAction != null) onKillAction.Invoke();
			onKillAction = null;
        }
	}

	IEnumerator IESwitchOffComplete()
	{
		yield return new WaitForSeconds(timeClose);
		ieSwitchOff = null;
		SwitchOffComplete();
	}

	void SwitchOffComplete()
	{
		isOn = false;
		isBusy = false;
		DelMenu(this);
		OnCloseComplete();
		if (lockTouchObjects && TouchObjectsController.Instance != null) TouchObjectsController.Instance.Unlock();

		if (onKillAction != null) onKillAction.Invoke();
		onKillAction = null;
	}

	virtual protected void OnOpen()
	{
	}

	virtual protected void OnOpenComplete()
	{
	}

	virtual protected void OnClose()
	{
	}

	virtual protected void OnCloseComplete()
	{
	}

	public void OnOk()
	{
		SwitchOff(onOkAction);
	}

	public void OnCancel()
	{
		SwitchOff(onCancelAction);
	}

	protected void PlayAudio(AudioClip clip)
	{
		var sfxPlayer = this.sfxPlayer != null ? this.sfxPlayer : gameObject;
		Tools.PlayAudio(sfxPlayer, clip);
		//Debug.Log("- - - - PlayAudio: " + sfxPlayer.name + " clip=" + (clip != null ? clip.name : "null"));
	}

	void SetOnKillAction(System.Action action)
	{
		onKillAction = action;
	}

	virtual protected void onAndroidHome()
	{
		//Debug.Log("onAndroidHome");
	}

	virtual protected void onAndroidEscape()
	{
        //Debug.Log("onAndroidEscape");
        if (isOn && !isBusy)
        {
			if (MenuError.Instance != null && MenuError.Instance.IsOn) MenuError.Instance.SwitchOff();
            else if (onCancelAction != null) SwitchOff(onCancelAction);
        }
	}

	virtual protected void onAndroidMenu()
	{
		//Debug.Log("onAndroidMenu");
	}

	virtual protected void onKeyboardLeft()
	{
		//Debug.Log("onKeyboardLeft");
	}

	virtual protected void onKeyboardRight()
	{
		//Debug.Log("onKeyboardRight");
	}

	virtual protected void onKeyboardUp()
	{
		//Debug.Log("onKeyboardUp");
	}

	virtual protected void onKeyboardDown()
	{
		//Debug.Log("onKeyboardDown");
	}

	virtual protected void onKeyboardEnter()
	{
		//Debug.Log("onKeyboardEnter");
	}

	void CheckAndroidButtons()
	{
		if ( Input.GetKeyDown( KeyCode.Escape ) )		onAndroidEscape();
		if ( Input.GetKeyDown( KeyCode.Home ) )			onAndroidHome();
		if ( Input.GetKeyDown( KeyCode.Menu ) )			onAndroidMenu();
		if ( Input.GetKeyDown( KeyCode.LeftArrow) ) 	onKeyboardLeft();
		if ( Input.GetKeyDown( KeyCode.RightArrow) )	onKeyboardRight();
		if ( Input.GetKeyDown( KeyCode.UpArrow) )		onKeyboardUp();
		if ( Input.GetKeyDown( KeyCode.DownArrow) )		onKeyboardDown();
		if ( Input.GetKeyDown( KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return) )	onKeyboardEnter();
	}

	virtual protected void Update()
	{
		CheckAndroidButtons();
	}

	//static methods

	static List<Menu> menu_open;

	static void AddMenu(Menu menu)
	{
		if (menu_open == null) menu_open = new List<Menu>();
		if (!menu_open.Contains(menu)) menu_open.Add(menu);
	}

	static void DelMenu(Menu menu)
	{
		if (menu_open != null)
		{
			if (menu_open.Contains(menu)) menu_open.Remove(menu);
		}
	}

	static void CheckOpenMenu()
	{
		if (menu_open != null)
		{
			List<Menu> list = new List<Menu>();
			foreach (var menu in menu_open)
				if (menu != null && menu.IsOn)
					list.Add(menu);
			menu_open = list;
		}
	}

	public static List<Menu> OpenMenuList()
    {
		CheckOpenMenu();
		return menu_open;
    }

	public static int OpenMenuCount()
	{
		var list = OpenMenuList();
		if (list != null) 
			return list.Count;
		return 0;
	}

	public static void CloseAllMenu()
    {
		List<Menu> list = OpenMenuList();
		if (list != null)
			list.ForEach(menu => { if (menu.isOn) menu.SwitchOff();  });
    }
}
