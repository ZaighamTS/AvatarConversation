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
    [Space]
    [Space]
    public ElevenLabsTTS tts;
    public LipSyncController lsc;

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
                AvatarAnimator.SetBool("listening", true);
            }
            else if (AvatarAnimator.GetBool("listening"))
            {
                timer -= Time.deltaTime;
                if (timer < 0)
                {
                    AvatarAnimator.SetBool("listening", false);
                    timer = 2;
                }
            }
        }
    }

    public void OnSendMessage()
    {
        string userMessage = inputField.text;
        inputField.text = "";
        AvatarAnimator.SetBool("listening", false);
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
            StartCoroutine(tts.Speak(reply));
            AppendMessage("AI", reply);
            //chatLog.text += $"\n<b>AI:</b> {reply}";
        }
    }

    private void AppendMessage(string sender, string message)
    {
        chatLog.text += $"\n<b>{sender}:</b> {message}";
        Canvas.ForceUpdateCanvases(); // Force layout rebuild
        scrollRect.verticalNormalizedPosition = 0; // Scroll to bottom
    }
}
