using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuDialogItem : MonoBehaviour
{
    public Text text;
    public Text textIndex;  //на случай, если есть нумерация элементов диалога
    public CanvasGroup canvasGroup;
    public float doneAlpha = 0.8f;
    public Image progress;

    public RectTransform _rectTransform;
    public RectTransform rectTransform
    {
        get
        {
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
            return _rectTransform;
        }
    }
    public float vSpacing = 10;

    //float resizeTime = -1;
    bool isDone = false;
    float timeFrom = -1;
    float timeTo = -1;
    System.Action onTapAction;

    public void Set(string text, float delay, System.Action onTapAction = null)
    {
        this.text.text = text;
        this.onTapAction = onTapAction;
        
        isDone = false;
        timeFrom = delay > 0 ? Time.time : -1;
        timeTo = delay > 0 ? timeFrom + delay : -1;

        if (progress != null)
            progress.gameObject.SetActive(delay > 0);

        //resizeTime = Time.realtimeSinceStartup + Time.deltaTime;
    }

    public void SetIndex(string strIndex)
    {
        if (textIndex != null) textIndex.text = strIndex;
    }

    public void SetDone(bool isDone)
    {
        this.isDone = isDone;
        if (canvasGroup != null) canvasGroup.alpha = isDone ? doneAlpha : 1;
    }

    void Resize()
    {
        var rectText = text.rectTransform.rect;
        var rect = rectTransform.rect;
        float dh = rectText.height + vSpacing * 2 - rect.height;
        rectTransform.anchorMax += Vector2.up * dh / 2;
        rectTransform.anchorMin -= Vector2.up * dh / 2;
    }

    public void OnTap()
    {
        if (onTapAction != null) onTapAction.Invoke();
    }

    private void Update()
    {
        /*if (resizeTime >= 0 && Time.realtimeSinceStartup >= resizeTime)
            resizeTime = -1;*/

        if (!isDone && timeTo > 0)
        {
            if (progress != null)
                progress.fillAmount = Mathf.Max(0, Mathf.Min(1, (Time.time - timeFrom) / (timeTo - timeFrom)));

            if (Time.time >= timeTo)
            {
                OnTap();
                isDone = true;
            }
        }
    }
}
