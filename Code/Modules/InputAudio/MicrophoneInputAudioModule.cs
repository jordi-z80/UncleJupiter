using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using NAudio.Wave;
using Vosk;

namespace UncleJupiter;

//=============================================================================
/// <summary>Reads audio from input and serves it to listeners</summary>
internal class MicrophoneInputAudioModule : IInputAudioModule
{

    public bool InputAvailable { get; private set; }

    int sampleRate;
    public int SampleRate => sampleRate;

    public event EventHandler<AudioInputAvailableEventArgs> AudioInput;


    public MicrophoneInputAudioModule()
    {
        startListening();
    }

    //=============================================================================
    /// <summary></summary>
    public void startListening()
    {
        try
        {
            int bitRate = Program.Settings.InputAudio.Bitrate;
            int bufferInMilliseconds = Program.Settings.InputAudio.BufferInMilliseconds;

            // microphone
            WaveInEvent waveIn = new WaveInEvent();
            waveIn.WaveFormat = new WaveFormat(bitRate, 16, 1);
            waveIn.DataAvailable += WaveIn_DataAvailable;
            waveIn.BufferMilliseconds = bufferInMilliseconds;
            waveIn.RecordingStopped += WaveIn_RecordingStopped;
            waveIn.StartRecording();

            sampleRate = waveIn.WaveFormat.SampleRate;

            InputAvailable = true;
        }
        catch
        {
            InputAvailable = false;

            ConsoleEx.Error("Couldn't initialize audio input.");
        }
    }

    //=============================================================================
    /// <summary></summary>
    private void WaveIn_RecordingStopped(object sender, StoppedEventArgs e)
    {
        InputAvailable = false;
    }

    //=============================================================================
    /// <summary></summary>
    private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
    {
        var arg = new AudioInputAvailableEventArgs(e.Buffer, e.BytesRecorded);
        AudioInput?.Invoke(this, arg);
    }

}
