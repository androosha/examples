using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuDialog : Menu
{
    public static MenuDialog Instance;

    public AudioClip onTapSound;
    public GameObject onTapSoundSource;

    public MenuDialogItem itemQuaestion;
    public MenuDialogItem itemAnswer;

    public MenuDialogItem itemAnswerSelect;
    public MenuDialogItem itemAnswerIAP;
    public RectTransform itemsSlicer;

    List<MenuDialogItem> items = null;
    List<MenuDialogItem> itemsAnswers = null;
    System.Action<int> onComplete;
    int result = 0;
    RecordDialog currentDialog;

    protected override void Init()
    {
        base.Init();
        if (Instance == null)
        {
            Instance = this;

            items = new List<MenuDialogItem>();
            itemsAnswers = new List<MenuDialogItem>();

            itemQuaestion.gameObject.SetActive(false);
            itemAnswer.gameObject.SetActive(false);

            itemAnswerSelect.gameObject.SetActive(false);
            itemAnswerIAP.gameObject.SetActive(false);
            itemsSlicer.gameObject.SetActive(false);
        }
    }

    public void Set(string dialog_id, System.Action<int> onComplete)
    {
        this.onComplete = onComplete;
        ClearDialog();
        AddDialog(dialog_id);
    }

    public void ClearDialog()
    {
        if (items != null)
        {
            foreach (var i in items)
                Destroy(i.gameObject);
            items.Clear();
        }

        if (itemsAnswers != null)
        {
            foreach (var i in itemsAnswers)
                Destroy(i.gameObject);
            itemsAnswers.Clear();
        }
    }

    string IAPText(string dialog_id)
    {
        string product_id = IAP.GetMinProductForDialogId(dialog_id);
        var iapMoney = IAP.GetMoney(product_id);

        if (iapMoney != null)
        {
            var strMoney = Tools.IngameToString(iapMoney.ingame, true);

            string message =
                RecordLocalization.GetStringLoc("txt_buy").getString() + " " +
                strMoney + "\n" +
                RecordLocalization.GetStringLoc("txt_for").getString() + " " +
                IAP.GetProductPrice(product_id);

            return message;
        }

        return null;
    }

    void AddDialog(string dialog_id)
    {
        if (dialog_id == null) return;

        var dialog = Dialogs.GetDialog(dialog_id);
        if (dialog != null)
        {
            AddDialogItem(items, itemQuaestion, dialog.text_key, -1, null);
            itemsSlicer.gameObject.SetActive(true);
            itemsSlicer.SetAsLastSibling();

            if (dialog.answers != null)
                for (int i = 0; i < dialog.answers.Count; i++)
                {
                    int index = i;
                    bool iap = dialog.answers[i].result == -2;
                    AddDialogItem(
                        itemsAnswers, 
                        iap ? itemAnswerIAP : itemAnswerSelect, 
                        iap ? IAPText(dialog_id) : dialog.answers[i].text_key,
                        dialog.answers[i].delay, 
                        delegate { OnAnswerTap(index); }, 
                        i + 1,
                        !iap
                        );
                }
            currentDialog = dialog;
        }
        else Debug.LogError("no dialog found for dialog_id=" + dialog_id);
    }

    void AddDialogItem(List<MenuDialogItem> items, MenuDialogItem template, string text_key, float delay, System.Action onTapAction, int index = -1, bool is_key = true)
    {
        var item = Instantiate(template, template.transform.parent);
        item.gameObject.SetActive(true);
        item.transform.localScale = template.transform.localScale;
        string text = is_key ? RecordLocalization.getStringLoc(text_key).getString() : text_key;
        item.Set(text, delay, onTapAction);
        if (index >= 0) item.SetIndex(index.ToString());

        items.Add(item);
    }

    void OnAnswerTap(int index)
    {
        var answer = currentDialog.answers[index];
        //Debug.Log(index + " -> " + answer.text_key);

        itemsSlicer.gameObject.SetActive(false);
        if (itemsAnswers != null)
        {
            foreach (var i in itemsAnswers)
                Destroy(i.gameObject);
            itemsAnswers.Clear();
        }

        if (onTapSoundSource != null)
            Tools.PlayAudio(onTapSoundSource, onTapSound);
        else
            PlayAudio(onTapSound);

        AddDialogItem(items, itemAnswer, answer.text_key, -1, null);
        if (answer.next_dialog_id != null)
        {
            foreach (var i in items) i.SetDone(true);
            AddDialog(answer.next_dialog_id);
        }
        else
        {
            result = answer.result;
            SwitchOff();
        }
    }

    protected override void OnCloseComplete()
    {
        base.OnCloseComplete();
        if (onComplete != null) onComplete.Invoke(result);
    }
}
