using UnityEngine;
using System.IO;


[RequireComponent(typeof(AudioSource))]
public class AudioSourceRecorder : MonoBehaviour
{
    [SerializeField] private bool useMicPlayback;
    [SerializeField] private int samplingRate = 88200; // You can adjust this based on your needs
    [SerializeField] private string filePath = "Assets/AudioOutput.wav"; // Change the path and filename as needed
    [SerializeField] private int duration = 10;
    private float[] audioData;
    private int audioDataLength = 0;
    private bool isRecording = false;    
    private AudioSource audioSource;
    private int channelNum;


    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (useMicPlayback)
            StartMicPlayback();
        channelNum = audioSource.clip?audioSource.clip.channels:1;
        audioData = new float[samplingRate * duration * channelNum];
    }
    private void StartRecording()
    {
        isRecording = true;
        audioDataLength = 0;
        channelNum = audioSource.clip?audioSource.clip.channels:1;

        Debug.Log("recording started");
    }

    private void StopRecording()
   {
        isRecording = false;

        SaveWav(filePath, audioData, audioDataLength, channelNum, samplingRate);
        Debug.Log("recording completed");
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) {
            StartRecording();
        }
        else if (Input.GetKeyDown(KeyCode.S)) {
            StopRecording();
        }
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {

        if (isRecording) {
            // Check if there's enough space in the audioData array
            if (audioDataLength + data.Length <= audioData.Length) {
                for (int i = 0; i < data.Length; i++) {
                    audioData[audioDataLength] = data[i];
                    audioDataLength += 1;
                }
            }
            else {
                Debug.LogWarning("Not enough space in the audioData array. Increase array size.");
                StopRecording();
            }
        }
    }

    private void SaveWav(string filename, float[] samples, int length, int channels, int sampleRate)
    {

        FileStream fileStream = new (filename, FileMode.Create);
        BinaryWriter writer = new (fileStream);

        // Write headers
        writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
        writer.Write(36 + length * 2);
        writer.Write(new char[4] { 'W', 'A', 'V', 'E' });
        writer.Write(new char[4] { 'f', 'm', 't', ' ' });

        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * 2 * channels);
        writer.Write((short)(2 * channels));
        writer.Write((short)16);

        writer.Write(new char[4] { 'd', 'a', 't', 'a' });
        writer.Write(length * 2);

        // Write data
        foreach (float sample in samples) {
            writer.Write((short)(sample * 32767.0f));
        }

        writer.Close();
        fileStream.Close();
    }

    void StartMicPlayback () {

        // reference
        // - https://support.unity.com/hc/en-us/articles/206485253-How-do-I-get-Unity-to-playback-a-Microphone-input-in-real-time-
        string micName = Microphone.devices[0];
        int minFreq, maxFreq;
        Microphone.GetDeviceCaps(micName, out minFreq, out maxFreq);
        audioSource.clip = Microphone.Start(micName, true, 10, samplingRate);
        audioSource.loop = true;
        while (!(Microphone.GetPosition(micName)>0)){}
        audioSource.Play();
        Debug.Log($"Microphone Use");
        Debug.Log($"\t {Microphone.devices[0]}");
        Debug.Log($"\t {minFreq}, {maxFreq}");
    }
}
