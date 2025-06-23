using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using Newtonsoft.Json;

public class ElevenLabsTTS : MonoBehaviour
{
    [Header("ElevenLabs Settings")]
    public string apiKey = "YOUR_ELEVENLABS_API_KEY";
    public string voiceId = "YOUR_VOICE_ID"; // e.g., EXAVITQu4vr4xnSDxMaL

    [Header("Unity Audio")]
    public AudioSource audioSource;

    // Main public function
    public IEnumerator Speak(string text)
    {
        string url = $"https://api.elevenlabs.io/v1/text-to-speech/{voiceId}";

        // Create the request body
        var requestBody = new TextToSpeechRequest
        {
            text = text,
            model_id = "eleven_multilingual_v2",
            voice_settings = new VoiceSettings
            {
                stability = 0,
                similarity_boost = 0,
                style = 0.5f,
                use_speaker_boost = true
            }
        };

        string json = JsonConvert.SerializeObject(requestBody);

        // Build the UnityWebRequest
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);

        // MP3 handling
        var downloadHandler = new DownloadHandlerAudioClip(url, AudioType.MPEG);
        request.downloadHandler = downloadHandler;

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("xi-api-key", apiKey);
        request.SetRequestHeader("Accept", "audio/mpeg");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("ElevenLabs Error: " + request.error);
        }
        else
        {
            AudioClip clip = downloadHandler.audioClip;
            if (clip != null)
            {
                audioSource.clip = clip;
                audioSource.Play();
                ChatManager.CM.AvatarAnimator.SetBool("answering", true);
                StartCoroutine(StopAudio(clip.length));
            }
            else
            {
                Debug.LogError("Failed to load audio clip.");
            }
        }
    }

    IEnumerator StopAudio(float time)
    {
        ChatManager.CM.loadingObjects[1].SetActive(false);
        yield return new WaitForSeconds(time);
        ChatManager.CM.AvatarAnimator.SetBool("answering", false);
    }

    // Classes for JSON serialization
    [System.Serializable]
    public class TextToSpeechRequest
    {
        public string text;
        public string model_id;
        public VoiceSettings voice_settings;
    }

    [System.Serializable]
    public class VoiceSettings
    {
        public int stability;
        public int similarity_boost;
        public float style;
        public bool use_speaker_boost;
    }
}
