using ArabicSupport;
using Newtonsoft.Json;
using ReadyPlayerMe.Core;
using ReadyPlayerMe.Samples.QuickStart;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ChatManager : MonoBehaviour
{
    public static ChatManager CM; 

    public string openAI_APIKey = "your-api-key";
    public TMP_InputField inputField;
    public TextMeshProUGUI chatLog;
    public ScrollRect scrollRect;
    [Space]
    [Space]
    public GameObject RPMPlayer;
    public Animator AvatarAnimator;
    float timer = 2;
    public Button[] PromptButtons;
    public GameObject[] loadingObjects;
    [Space]
    [Space]
    public ElevenLabsTTS tts;
    public LipSyncController lsc;
    [Space]
    [Space]
    public MicRecorder recorder;
    public Image recorderImage;
    public Sprite[] recorderSprites;

    [System.Serializable]
    public class Message
    {
        public string role;
        public string content;
    }

    [System.Serializable]
    public class RequestData
    {
        public string model;
        public List<Message> messages;
    }

    [System.Serializable]
    public class Choice
    {
        public Message message;
    }

    [System.Serializable]
    public class ResponseData
    {
        public List<Choice> choices;
    }

    private void Awake()
    {
        CM = this;
    }

    private void Update()
    {
        if (RPMPlayer.transform.childCount == 3 && AvatarAnimator == null)
        {
            AvatarAnimator = RPMPlayer.transform.GetChild(2).GetComponent<Animator>();
            lsc.faceRenderer = RPMPlayer.transform.GetChild(2).Find("Renderer_Head").GetComponent<SkinnedMeshRenderer>();
            lsc.enabled = true;
        }
        if (AvatarAnimator != null)
        {
            if (inputField.text.Length > 0)
            {
                PromptButtons[1].interactable = false;
                AvatarAnimator.SetBool("listening", true);
            }
            else if (AvatarAnimator.GetBool("listening"))
            {
                timer -= Time.deltaTime;
                if (timer < 0)
                {
                    AvatarAnimator.SetBool("listening", false);
                    PromptButtons[1].interactable = true;
                    timer = 2;
                }
            }
        }

        if (inputField.text.Length > 0)
        {
            if (Input.GetKey(KeyCode.Return))
            {
                OnSendMessage("");
            }
        }
    }

    public void OnSendMessage(string text)
    {
        string userMessage = inputField.text;
        if (text != "")
        {
            userMessage = text;
        }
        inputField.text = "";
        AvatarAnimator.SetBool("listening", false);
        PromptButtons[1].interactable = true;
        loadingObjects[0].SetActive(true);
        AppendMessage("You", userMessage);
        //chatLog.text += $"\n<b>You:</b> {userMessage}";
        StartCoroutine(SendToOpenAI(userMessage));
    }
    
    IEnumerator SendToOpenAI(string message)
    {
        string url = "https://api.openai.com/v1/chat/completions";

        var requestData = new RequestData
        {
            //model = "gpt-3.5-turbo",
            model = "gpt-4o",
            messages = new List<Message>
            {
                new Message { role = "system", content = "You are a helpful AI assistant." },
                new Message { role = "user", content = message }
            }
        };

        string json = JsonConvert.SerializeObject(requestData);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] body = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {openAI_APIKey}");

        yield return request.SendWebRequest();
        
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("OpenAI error: " + request.error);
            AppendMessage("AI", "Error.");
            //chatLog.text += $"\n<b>AI:</b> Error.";
            AvatarAnimator.SetBool("answering", false);
        }
        else
        {
            string result = request.downloadHandler.text;
            var response = JsonConvert.DeserializeObject<ResponseData>(result);
            string reply = response.choices[0].message.content;
            loadingObjects[1].SetActive(true);
            StartCoroutine(tts.Speak(reply));
            AppendMessage("AI", reply);
            //chatLog.text += $"\n<b>AI:</b> {reply}";
        }
    }

    private void AppendMessage(string sender, string message)
    {
        loadingObjects[0].SetActive(false);
        if (ContainsArabic(message))
        {
            message = ArabicFixer.Fix(message, showTashkeel: false, useHinduNumbers: false);
        }
        chatLog.text += $"\n<b>{sender}:</b> {message}";
        Canvas.ForceUpdateCanvases(); // Force layout rebuild
        scrollRect.verticalNormalizedPosition = 0; // Scroll to bottom
    }

    bool ContainsArabic(string text)
    {
        foreach (char c in text)
        {
            if ((c >= 0x0600 && c <= 0x06FF) || (c >= 0x0750 && c <= 0x077F)) // Arabic & Urdu
                return true;
        }
        return false;
    }

    public void VoiceInputButton()
    {
        if (!recorder.isRecording)
        {
            BeginVoiceInput();
        }
        else if (recorder.isRecording)
        {
            EndVoiceInput();
        }
    }

    public void BeginVoiceInput()
    {
        recorderImage.sprite = recorderSprites[1];
        PromptButtons[0].interactable = false;
        recorder.StartRecording();
    }

    public void EndVoiceInput()
    {
        recorderImage.sprite = recorderSprites[0];
        PromptButtons[0].interactable = true;
        recorder.StopRecordingAndSendToWhisper((wavData) =>
        {
            StartCoroutine(TranscribeWithWhisper(wavData, (text) =>
            {
                Debug.Log("You said: " + text);
                OnSendMessage(text); // pass to OpenAI like typed message
            }));
        });
    }

    public IEnumerator TranscribeWithWhisper(byte[] wavData, System.Action<string> onTextReady)
    {
        loadingObjects[0].SetActive(true);
        string url = "https://api.openai.com/v1/audio/transcriptions";

        List<IMultipartFormSection> form = new List<IMultipartFormSection>
    {
        new MultipartFormFileSection("file", wavData, "speech.wav", "audio/wav"),
        new MultipartFormDataSection("model", "whisper-1")
    };

        UnityWebRequest www = UnityWebRequest.Post(url, form);
        www.SetRequestHeader("Authorization", "Bearer " + openAI_APIKey);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Whisper Error: " + www.error);
        }
        else
        {
            string json = www.downloadHandler.text;
            var response = JsonConvert.DeserializeObject<WhisperResponse>(json);
            onTextReady?.Invoke(response.text);
            loadingObjects[0].SetActive(false);
        }
    }

    [System.Serializable]
    public class WhisperResponse
    {
        public string text;
    }

}
