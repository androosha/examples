using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class TextureLoader : MonoBehaviour
{
    public delegate void OnComplete(string fileName, Texture2D texture);
    public event OnComplete onComplete;

    static Dictionary<string, TextureLoader> loaders;

    public static void Load(string fileName, OnComplete onComplete)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            if (onComplete != null)
                onComplete.Invoke(null, null);
        }
        else
        {
            if (fileName.Contains("http://") || fileName.Contains("https:'//"))
                LoadFromURL(fileName, onComplete);
            else
                LoadFromLocal(fileName, onComplete);
        }
    }

    static void LoadFromLocal(string fileName, OnComplete onComplete)
    {
        var texture = LoadTexture(fileName);
        if (onComplete != null)
            onComplete.Invoke(fileName, texture);
    }

    static Texture2D LoadTexture(string filePath)
    {
        Texture2D tex = null;
        byte[] fileData;

        if (File.Exists(filePath))
        {
            fileData = File.ReadAllBytes(filePath);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        }

        Debug.Log("LadTexture: " + filePath + " tex=" + tex);
        return tex;
    }

    static void LoadFromURL(string fileName, OnComplete onComplete)
    {
        CheckLoaders();

        if (loaders != null && loaders.ContainsKey(fileName) && loaders[fileName] != null)
        {
            //если такой файл уже качается, то цеаляемся к лоадеру
            loaders[fileName].onComplete += onComplete;
        }
        else
        {
            //если такой файл ещё не качается, запускаем закачку
            TextureLoader loader = Root.Instance.gameObject.AddComponent<TextureLoader>();
            loader.StartLoad(fileName, onComplete);

            if (loaders == null) loaders = new Dictionary<string, TextureLoader>();
            loaders.Add(fileName, loader);
        }
    }

    static void CheckLoaders()
    {
        //удаляем нулевые элементы из библиотечки
        if (loaders == null) return;
        Dictionary<string, TextureLoader> list = new Dictionary<string, TextureLoader>();
        foreach (var key in loaders.Keys)
            if (loaders[key] != null)
                list.Add(key, loaders[key]);
        loaders.Clear();
        loaders = list;
    }

    public void StartLoad(string fileName, OnComplete onComplete)
    {
        this.onComplete += onComplete;
        StartCoroutine(IEGetTexture(fileName));
    }

    IEnumerator IEGetTexture(string fileName)
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(fileName);
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.LogError("Error loadind from path=" + fileName + " error=" + www.error);
            if (onComplete != null)
            {
                onComplete.Invoke(fileName, null);
                onComplete = null;
            }
        }
        else
        {
            Texture2D texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
            if (onComplete != null)
            {
                onComplete.Invoke(fileName, texture);
                onComplete = null;
            }
        }

        Destroy(this);
    }
}