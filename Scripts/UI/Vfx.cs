using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vfx : MonoBehaviour
{
    public ParticleSystem effect;
    public Camera mainCamera;

    RectTransform target = null;
    bool destroyWithTarget = false;
    bool effectEnabled = false;
    IEnumerator ieAction = null;

    private void Start()
    {
        if (effect != null) effect.gameObject.SetActive(effectEnabled);
    }

    public void SetTo(RectTransform rectTransform)
    {
        var screenPos = Tools.CanvasToScreen(rectTransform);
        var worldPos = mainCamera.ScreenToWorldPoint(screenPos);
        var localPos = mainCamera.transform.InverseTransformPoint(worldPos);
        localPos.z = transform.localPosition.z;
        transform.localPosition = localPos;
    }

    public void FollowTo(RectTransform target, bool destroyWithTarget = true)
    {
        this.target = target;
        this.destroyWithTarget = destroyWithTarget;
        effectEnabled = true;
        effect.gameObject.SetActive(true);

        SetTo(target);
    }

    IEnumerator IEAction(float delay, System.Action action)
    {
        yield return new WaitForSeconds(delay);
        action();
        ieAction = null;
    }

    private void Update()
    {
        if (target != null)
        {
            SetTo(target);
        }
        else if (destroyWithTarget && ieAction == null)
        {
            if (effect != null)
            {
                float delay = effect.main.startLifetime.constant;
                effect.Stop();
                ieAction = IEAction(delay, delegate { Destroy(gameObject); });
                StartCoroutine(ieAction);
            }
        }
    }
}
