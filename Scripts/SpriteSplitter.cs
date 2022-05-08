using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using System.IO;

public class SpriteSplitter : MonoBehaviour
{
	string pic_ext = ".jpg";

	public InputField inputUploadPath;
	public GameObject buttonLoadPicture;

	public InputField inputSavePath;
	public Button buttonJpg;
	public Button buttonPng;
	public Button buttonZeros;				//вставлять или нет "0" в название файлов, типа "001", "002" ... "100"
	public InputField inputResultSize;      //окошко ввода максимального размера картинки для NFT
	public Image source;
	public Image marker;
	public InputField inputX;
	public InputField inputY;
	public Text textCellsCount;

	public Image preview;
	public RenderResult renderResult;
	public Text textSize;
	public Text textIndex;
	public Text textCellSize;   //отображает размер разбиения картинки при заданных inputX и inputY
	public Image lineV;
	public Image lineH;

	public InputField inputJsonSavePath;
	public InputField inputIPFSPrefix;
	public InputField inputDescription;

	public List<GameObject> buttonsSplit;  //контейнер для кнопок buttonSplit и buttonSplitAll
	public GameObject controlsContainer;

	int x_count = 10;
	int y_count = 10;
	int result_rize = 512;

	int current_index = 1;
	Vector2 offset = Vector3.zero;
	bool use_zeros = false;

	List<Image> vLines;
	List<Image> hLines;

	bool isPictureLoading = false;

	void Start()
	{
		Debug.Log(DigitsCount(9));

		OutMaxCounts(source.sprite);
		inputX.text = x_count.ToString();
		inputY.text = y_count.ToString();
		inputResultSize.text = result_rize.ToString();

		OnResultSizeChanged();
		ValueChangeCheck();
		ButtonFormatStates(pic_ext);
		ButtonState(buttonZeros, use_zeros);

		SubscribeInputs();
	}

	void SubscribeInputs()
	{
		inputX.onValueChanged.AddListener(delegate { ValueChangeCheck(); });
		inputY.onValueChanged.AddListener(delegate { ValueChangeCheck(); });
		inputResultSize.onValueChanged.AddListener(delegate { OnResultSizeChanged(); });
	}

	void UnSubscribeInputs()
	{
		inputX.onValueChanged.RemoveAllListeners();
		inputY.onValueChanged.RemoveAllListeners();
		inputResultSize.onValueChanged.RemoveAllListeners();
	}

	void OnResultSizeChanged()
	{
		result_rize = Mathf.FloorToInt(float.Parse(inputResultSize.text));
		renderResult.SetMaxRenderSize(result_rize);
	}

	public void OnZeros()
    {
		use_zeros = !use_zeros;
		ButtonState(buttonZeros, use_zeros);
    }

	public void OnPNG()
	{
		pic_ext = ".png";
		ButtonFormatStates(pic_ext);
	}

	public void OnJPG()
	{
		pic_ext = ".jpg";
		ButtonFormatStates(pic_ext);
	}

	void ButtonFormatStates(string pic_ext)
	{
		ButtonState(buttonPng, pic_ext == ".png");
		ButtonState(buttonJpg, pic_ext == ".jpg");
	}

	void ButtonState(Button button, bool enabled)
	{
		Color color = button.image.color;
		color.a = enabled ? 1 : 0.3f;
		button.image.color = color;
	}

	public void LoadPicture()
	{
		if (!isPictureLoading)
		{
			string fileName = inputUploadPath.text;

			if (!string.IsNullOrEmpty(fileName))
			{
				isPictureLoading = true;
				buttonLoadPicture.SetActive(false);
				TextureLoader.Load(fileName, OnLoadPictuteComplete);
			}
		}
	}

	void OnLoadPictuteComplete(string fileName, Texture2D tex)
	{
		Debug.Log("loaded: tex=" + tex);

		buttonLoadPicture.SetActive(true);
		Vector2 size = new Vector2(tex.width, tex.height);
		Rect rect = new Rect(Vector2.zero, size);
		source.sprite = Sprite.Create(tex, rect, Vector2.zero);

		if (!IsCorrectSplit(x_count, y_count))
		{
			UnSubscribeInputs();
			x_count = 1;
			y_count = 1;

			inputX.text = x_count.ToString();
			inputY.text = y_count.ToString();
			SubscribeInputs();
		}

		Debug.Log("picture loaded: w=" + tex.width + " h=" + tex.height);
		StartCoroutine(IEAction(Time.deltaTime, RedrawAll));
	}

	IEnumerator IEAction(float delay, System.Action action)
	{
		yield return new WaitForSeconds(delay);
		if (action != null)
			action.Invoke();
	}

	void RedrawAll()
	{
		isPictureLoading = false;
		buttonLoadPicture.SetActive(true);

		OutMaxCounts(source.sprite);
		ValueChangeCheck();
	}

	int DigitsCount(int n)
	{
		if (n == 0)
			return 1;

		return Mathf.FloorToInt(Mathf.Log10(n)) + 1;
	}

	void ButtonsSplitEnabled(bool enabled)
    {
		if (buttonsSplit != null && buttonsSplit.Count > 0)
			foreach (var button in buttonsSplit)
				if (button != null)
					button.SetActive(enabled);
    }

	void ValueChangeCheck()
	{
		Debug.Log("Value Changed");

		if (!string.IsNullOrEmpty(inputX.text) && !string.IsNullOrEmpty(inputY.text))
		{
			x_count = int.Parse(inputX.text);
			y_count = int.Parse(inputY.text);

			var rect = source.sprite.rect;
			float width = rect.width / x_count;
			float height = rect.height / y_count;
			textCellSize.text = width.ToString() + " x " + height.ToString();
			textCellsCount.text = (x_count * y_count).ToString();

			current_index = 0;
			if (IsCorrectSplit(x_count, y_count))
			{
				ButtonsSplitEnabled(true);
				RedrawSplitMesh(x_count, y_count, source);
				Generate(0, source.sprite, false);
			}
			else
				ButtonsSplitEnabled(false);
		}

		var indexMax = x_count * y_count;
		Debug.Log("x_count=" + x_count + " , y_count=" + y_count);
		Debug.Log(indexMax + " digits=" + DigitsCount(indexMax));
	}

	float RateScale(Image image)
	{
		var rect = image.sprite.rect;
		float rate_x = image.rectTransform.sizeDelta.x / rect.width;
		float rate_y = image.rectTransform.sizeDelta.y / rect.height;
		return Mathf.Min(rate_x, rate_y);
	}

	void PlaceMarker(int index)
	{
		var place = GetPlace(index, source.sprite.texture);
		float rate = RateScale(source);

		Vector2 size = place.size * rate;
		marker.rectTransform.sizeDelta = size;
		marker.rectTransform.localPosition = place.place * rate + size * 0.5f;
	}

	void RedrawSplitMesh(int x_count, int y_count, Image image)
	{
		if (vLines == null)
		{
			vLines = new List<Image>();
			vLines.Add(lineV);
		}

		if (hLines == null)
		{
			hLines = new List<Image>();
			hLines.Add(lineH);
		}

		var rect = image.sprite.rect;
		float rate = RateScale(image);
		float stepX = rate * rect.width / x_count;
		float stepY = rate * rect.height / y_count;

		RedrawLines(vLines, Vector2.right * stepX, x_count);
		RedrawLines(hLines, Vector2.up * stepY, y_count);
	}

	void RedrawLines(List<Image> lines, Vector2 step, int count)
	{
		var rect = source.sprite.rect;
		float rate = RateScale(source);// source.rectTransform.sizeDelta.x / source.sprite.rect.width;

		for (int i = 0; i < Mathf.Max(lines.Count, count); i++)
		{
			Image line;
			if (i < lines.Count)
			{
				line = lines[i];
				line.gameObject.SetActive(i < count);
			}
			else
			{
				line = Instantiate(lines[0], lines[0].transform.parent);
				line.gameObject.SetActive(true);
				lines.Add(line);
			}

			if (step.x != 0)
				line.rectTransform.sizeDelta = new Vector3(1, rate * rect.height);
			else
				line.rectTransform.sizeDelta = new Vector3(rate * rect.width, 1);

			line.transform.localPosition = -step * count / 2 + i * step;
		}
	}

	bool isBusy = false;

	public void StartSplitAll()
	{
		if (IsCorrectSplit(x_count, y_count) && !isBusy)
		{
			isBusy = true;
			controlsContainer.SetActive(false);
			int indexMax = x_count * y_count;
			StepSplit(0, indexMax);
		}
	}

	void StepSplit(int index, int indexMax)
	{
		if (index < indexMax)
		{
			Generate(index, source.sprite, true, delegate { StepSplit(index + 1, indexMax); });
		}
		else
		{
			isBusy = false;
			controlsContainer.SetActive(true);
		}
	}

	public void StartSplitStep()
	{
		if (!isBusy)
		{
			Debug.Log("x_count=" + x_count + " , y_count=" + y_count);

			int indexMax = x_count * y_count;
			if (current_index >= indexMax)
				current_index = 0;

			Generate(current_index, source.sprite, true, StartSplitStepDone);
		}
	}

	void StartSplitStepDone()
    {
		current_index++;
		Generate(current_index, source.sprite, false);
		isBusy = false;
	}

	void OutMaxCounts(Sprite sprite)
	{
		var rect = sprite.rect;
		textSize.text = rect.width.ToString() + " x " + rect.height.ToString();
	}

	bool IsCorrectSplit(int x_count, int y_count)
	{
		if (Mathf.FloorToInt(x_count) != x_count || Mathf.FloorToInt(y_count) != y_count)
		{
			Debug.LogError("x_count or y_count is not int!");
			return false;
		}

		var rect = source.sprite.rect;

		if (rect.width % x_count != 0)
		{
			Debug.LogError("width: " + rect.width.ToString() + " % " + x_count.ToString() + " != 0");
			return false;
		}

		if (rect.height % y_count != 0)
		{
			Debug.LogError("height: " + rect.height.ToString() + " % " + y_count.ToString() + " != 0");
			return false;
		}

		return true;
	}

	bool isGenereBusy = false;

	void Generate(int index, Sprite source, bool save, System.Action onGenerateComplete = null)
	{
		if (IsCorrectSplit(x_count, y_count) && !isGenereBusy)
		{
			textIndex.text = current_index.ToString() + " / " + (x_count * y_count).ToString();

			var sprite = CreateItemSprite(index, source.texture);
			preview.sprite = sprite;
			PlaceMarker(index);
			renderResult.SetSprite(sprite);

			if (save)
			{
				isGenereBusy = true;
				string path = CorrectPathPrefix(inputSavePath.text);

				StartCoroutine(IEAction(Time.deltaTime, delegate
				{
					renderResult.SaveTarget(path + CreateName(index, use_zeros) + pic_ext, pic_ext);	//названия нумеруем от 1
					isGenereBusy = false;
					if (onGenerateComplete != null)
						onGenerateComplete.Invoke();
				}));
			}
			else if (onGenerateComplete != null)
				onGenerateComplete.Invoke();
		}
	}

	(Vector2 place, Vector2 size) GetPlace(int index, Texture2D texture)
	{
		Vector2 size = new Vector2(texture.width / x_count, texture.height / y_count);

		var ij = GetXY(index);
		int i = ij.x;
		int j = ij.y;

		/*int j = Mathf.FloorToInt(index / x_count);
		int i = index - j * x_count;
		j = y_count - 1 - j;*/

		Debug.Log(i + " x " + j);

		float x = i * size.x - x_count * size.x / 2f;
		float y = j * size.y - y_count * size.y / 2f;

		Vector2 place = new Vector2(x, y) + offset;
		return (place, size);
	}

	Sprite CreateItemSprite(int index, Texture2D texture)
	{
		var place = GetPlace(index, texture);
		Sprite spr = CreateItemSprite(place.size, place.place, texture);

		return spr;
	}

	static Sprite CreateItemSprite(Vector2 size, Vector2 place, Texture2D texture)
	{
		//size - размер спрайта
		//place - центр спрайта в исходной текстуре
		Vector2 posFrom = place + (new Vector2(texture.width, texture.height)) / 2f;
		Rect rect = new Rect(posFrom, size);
		Sprite spr = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 1);
		return spr;
	}

	(int x, int y) GetXY(int index)
    {
		int y = Mathf.FloorToInt(index / x_count);
		int x = index - y * x_count;
		y = y_count - 1 - y;
		return (x, y);
	}

	string CreateName(int index, bool use_zeros)	//use_zeros == true - заполняем пустые разряды нулями, например: "0024"
    {
		/*if (use_zeros)
		{
			int digitsMax = DigitsCount(x_count * y_count);
			int digits = DigitsCount(index);

			Debug.Log("digits=" + digits + ", digitsMax=" + digitsMax);

			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < digitsMax - digits; i++)
				sb.Append("0");
			sb.Append(index.ToString());

			return sb.ToString();
		}

		return index.ToString();*/

		var xy = GetXY(index);
		xy.y = y_count - 1 - xy.y;
		return "2-" + xy.x.ToString() + "-" + xy.y.ToString();
    }

	string CorrectPathPrefix(string path)
    {
		if (!string.IsNullOrEmpty(path) && path[path.Length - 1] != Path.AltDirectorySeparatorChar && path[path.Length - 1] != Path.DirectorySeparatorChar)
		{
			if (path.Contains(Path.DirectorySeparatorChar.ToString()))
				path += Path.DirectorySeparatorChar;
			else
				path += Path.AltDirectorySeparatorChar;
		}

		return path;
    }

	RecordMetadata CreateMetadata(int index, bool use_zeros)
    {
		string name = CreateName(index, use_zeros);
		RecordMetadata metadata = new RecordMetadata {
			name = "#" + name,
			description = inputDescription.text,
			uri = CorrectPathPrefix(inputIPFSPrefix.text) + name + pic_ext,
		};

		return metadata;
    }

	void SaveJson(int index)
	{
		string name = CreateName(index, use_zeros);
		var metadata = CreateMetadata(index, use_zeros);
		string json = JsonUtility.ToJson(metadata);
		string path = CorrectPathPrefix(inputJsonSavePath.text) + name + ".json";
		File.WriteAllText(path, json);

		Debug.Log("file: " + path + " metadata: " + json);
    }

	public void SaveJsonOne()
	{
		SaveJson(current_index + 1);
	}

	public void SaveJsonAll()
	{
		controlsContainer.SetActive(false);
		SaveJasonStep(0, x_count * y_count);
	}

	void SaveJasonStep(int index, int indexMax)
    {
		if (index < indexMax)
        {
			StartCoroutine(IEAction(Time.deltaTime, delegate {
				SaveJson(index);
				Generate(index, source.sprite, false, delegate { SaveJasonStep(index + 1, indexMax); });
			}));
		}
		else
			controlsContainer.SetActive(true);
	}
}
