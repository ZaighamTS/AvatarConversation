using UnityEngine;
using System.IO;

public class MicRecorder : MonoBehaviour
{
    public AudioSource audioSource;
    private AudioClip recordedClip;
    private string micDevice;
    public bool isRecording = false;

    public GameObject recordingSign;

    public void StartRecording()
    {
        micDevice = Microphone.devices[0];
        recordedClip = Microphone.Start(micDevice, false, 10, 16000);
        isRecording = true;
        recordingSign.SetActive(true);
    }

    public void StopRecordingAndSendToWhisper(System.Action<byte[]> onComplete)
    {
        if (!isRecording) return;
        Microphone.End(micDevice);
        isRecording = false;

        byte[] wavData = WavUtility.FromAudioClip(recordedClip);
        onComplete?.Invoke(wavData);
        recordingSign.SetActive(false);
    }
}
