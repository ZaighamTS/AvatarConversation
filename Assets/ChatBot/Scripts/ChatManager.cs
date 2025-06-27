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

    [HideInInspector]
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
        TextAsset keyFile = Resources.Load<TextAsset>("openai_key"); // No .txt extension

        if (keyFile != null)
        {
            openAI_APIKey = keyFile.text.Trim();
            Debug.Log("✅ OpenAI key loaded from Resources");
        }
        else
        {
            Debug.LogError("❌ openai_key.txt not found in Resources folder!");
        }
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
        string threadId = null;
        string apiKey = openAI_APIKey; // Your secret key

        // STEP 1: Create a thread
        UnityWebRequest createThread = new UnityWebRequest("https://api.openai.com/v1/threads", "POST");
        createThread.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes("{}")); // empty JSON
        createThread.downloadHandler = new DownloadHandlerBuffer();
        createThread.SetRequestHeader("Authorization", "Bearer " + apiKey);
        createThread.SetRequestHeader("Content-Type", "application/json");
        createThread.SetRequestHeader("OpenAI-Beta", "assistants=v2"); // ✅ Required for Assistants API v2

        yield return createThread.SendWebRequest();

        // Debug the raw response
        Debug.Log("Thread Response Code: " + createThread.responseCode);
        Debug.Log("Thread Response Text: " + createThread.downloadHandler.text);

        if (createThread.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Thread creation failed: " + createThread.error);
            AvatarAnimator.SetBool("answering", false);
            yield break;
        }

        threadId = JsonUtility.FromJson<ThreadResponse>(createThread.downloadHandler.text).id;

        // STEP 2: Add user message to thread
        ThreadMessage userMessage = new ThreadMessage
        {
            role = "user",
            content = message
        };

        string messageJson = JsonUtility.ToJson(userMessage);
        var addMessage = new UnityWebRequest($"https://api.openai.com/v1/threads/{threadId}/messages", "POST");
        addMessage.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(messageJson));
        addMessage.downloadHandler = new DownloadHandlerBuffer();
        addMessage.SetRequestHeader("Authorization", "Bearer " + apiKey);
        addMessage.SetRequestHeader("Content-Type", "application/json");
        addMessage.SetRequestHeader("OpenAI-Beta", "assistants=v2");

        yield return addMessage.SendWebRequest();

        if (addMessage.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Message adding failed: " + addMessage.error);
            Debug.Log("Message Body Sent: " + messageJson); // 🧪 Optional debug log
            AvatarAnimator.SetBool("answering", false);
            yield break;
        }

        // STEP 3: Run the Assistant
        RunRequest runRequest = new RunRequest
        {
            assistant_id = "asst_opgp81D5CblZ2pPajdEi03aC"
        };

        string runJson = JsonUtility.ToJson(runRequest);
        UnityWebRequest runWebRequest = new UnityWebRequest($"https://api.openai.com/v1/threads/{threadId}/runs", "POST");
        runWebRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(runJson));
        runWebRequest.downloadHandler = new DownloadHandlerBuffer();
        runWebRequest.SetRequestHeader("Authorization", "Bearer " + apiKey);
        runWebRequest.SetRequestHeader("Content-Type", "application/json");
        runWebRequest.SetRequestHeader("OpenAI-Beta", "assistants=v2");

        yield return runWebRequest.SendWebRequest();

        if (runWebRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Run request failed: " + runWebRequest.error);
            Debug.Log("Run JSON Sent: " + runJson);
            AvatarAnimator.SetBool("answering", false);
            yield break;
        }

        RunResponse runResponse = JsonUtility.FromJson<RunResponse>(runWebRequest.downloadHandler.text);
        string runId = runResponse.id;

        // STEP 4: Poll the run until it's complete
        string runStatus = "queued";
        while (runStatus != "completed")
        {
            UnityWebRequest checkRun = UnityWebRequest.Get($"https://api.openai.com/v1/threads/{threadId}/runs/{runId}");
            checkRun.SetRequestHeader("Authorization", "Bearer " + apiKey);
            checkRun.SetRequestHeader("OpenAI-Beta", "assistants=v2");
            checkRun.downloadHandler = new DownloadHandlerBuffer();
            yield return checkRun.SendWebRequest();

            if (checkRun.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Run status check failed: " + checkRun.error);
                AvatarAnimator.SetBool("answering", false);
                yield break;
            }

            runStatus = JsonUtility.FromJson<RunResponse>(checkRun.downloadHandler.text).status;
            yield return new WaitForSeconds(1f);
        }

        // STEP 5: Retrieve assistant's response
        UnityWebRequest getMessages = UnityWebRequest.Get($"https://api.openai.com/v1/threads/{threadId}/messages");
        getMessages.SetRequestHeader("Authorization", "Bearer " + apiKey);
        getMessages.SetRequestHeader("OpenAI-Beta", "assistants=v2");
        getMessages.downloadHandler = new DownloadHandlerBuffer();

        yield return getMessages.SendWebRequest();

        if (getMessages.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Get messages failed: " + getMessages.error);
            Debug.Log("Response: " + getMessages.downloadHandler.text); // 🧪 see actual error
            AvatarAnimator.SetBool("answering", false);
            yield break;
        }

        MessagesResponse messagesResponse = JsonUtility.FromJson<MessagesResponse>(getMessages.downloadHandler.text);
        string reply = messagesResponse.data[0].content[0].text.value;

        AvatarAnimator.SetBool("answering", false);
        AppendMessage("AI", reply);
        loadingObjects[1].SetActive(true);
        StartCoroutine(tts.Speak(reply));
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

    [System.Serializable]
    public class ThreadResponse { public string id; }

    [System.Serializable]
    public class ThreadMessage
    {
        public string role;
        public string content;
    }

    [System.Serializable]
    public class RunResponse { public string id; public string status; }

    [System.Serializable]
    public class MessagesResponse
    {
        public MessageData[] data;
    }

    [System.Serializable]
    public class MessageData
    {
        public MessageContent[] content;
    }

    [System.Serializable]
    public class MessageContent
    {
        public TextContent text;
    }

    [System.Serializable]
    public class TextContent
    {
        public string value;
    }

    [System.Serializable]
    public class RunRequest
    {
        public string assistant_id;
    }
}
