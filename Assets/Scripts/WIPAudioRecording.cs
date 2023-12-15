using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;


public class WIPAudioRecording : MonoBehaviour
{
    [SerializeField]
    public int lengthSec = 10; // Length of recording in seconds
    
    [SerializeField]
    int frequency = 44100;

    [SerializeField]
    string fileName = "";

    AudioClip recordedClip;
    AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            StartRecording();
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            StopRecording();
        }
    }

    void StartRecording()
    {
        // Define the microphone and recording settings
        string microphone = Microphone.devices[0]; // Change index if you have multiple microphones
        
        // Start recording
        recordedClip = Microphone.Start(microphone, true, lengthSec, frequency);
    }

    void StopRecording()
    {
        // Stop recording and get the recorded data
        Microphone.End(null);

        // Save the recorded audio to a WAV file
        string filePath = Path.Combine(Application.persistentDataPath, $"{fileName}.wav");
        SaveWav(filePath, recordedClip);

        // Play the recorded audio (optional)
        audioSource.clip = recordedClip;
        audioSource.Play();
    }

    // Save AudioClip data to a WAV file
    void SaveWav(string filePath, AudioClip audioClip)
    {
        WavUtility.ToWav(filePath, audioClip);
        Debug.Log("Recording saved to: " + filePath);
    }



}

public static class WavUtility
{
    // Converts AudioClip data to WAV format and saves it to a file
    public static void ToWav(string filePath, AudioClip audioClip)
    {
        byte[] wavData = ConvertAudioClipToWav(audioClip);
        File.WriteAllBytes(filePath, wavData);
    }

    // Converts AudioClip data to WAV format
    private static byte[] ConvertAudioClipToWav(AudioClip audioClip)
    {
        float[] samples = new float[audioClip.samples];
        audioClip.GetData(samples, 0);

        int channels = audioClip.channels;
        int sampleRate = audioClip.frequency;

        byte[] wavData = WavUtility.ConvertToWav(samples, channels, sampleRate);
        return wavData;
    }

    // Converts float audio data to WAV format
    private static byte[] ConvertToWav(float[] samples, int channels, int sampleRate)
    {
        MemoryStream stream = new MemoryStream();

        // WAV header
        BinaryWriter writer = new BinaryWriter(stream);

        writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
        writer.Write(36 + samples.Length * 4);
        writer.Write(new char[4] { 'W', 'A', 'V', 'E' });
        writer.Write(new char[4] { 'f', 'm', 't', ' ' });
        writer.Write(16);
        writer.Write((ushort)1);
        writer.Write((ushort)channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * 4);
        writer.Write((ushort)(channels * 4));
        writer.Write((ushort)32);

        // Data chunk
        writer.Write(new char[4] { 'd', 'a', 't', 'a' });
        writer.Write(samples.Length * 4);

        foreach (float sample in samples)
        {
            writer.Write(Convert.ToInt32(sample * 32767.0f));
        }

        byte[] wavData = stream.ToArray();
        writer.Close();
        stream.Close();

        return wavData;
    }
}