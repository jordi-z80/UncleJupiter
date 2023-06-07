using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UncleJupiter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NAudio.Wave;
using Newtonsoft.Json.Linq;
using Vosk;

namespace UncleJupiter;


public class VoskSpeechRecognition : ISpeechRecognitionModule
{
    VoskRecognizer voskRecognizer;


    public event EventHandler<SpeechRecognitionResultEventArgs> SpeechRecognitionResult;
    IInputAudioModule inputAudioModule;

    //=============================================================================
    /// <summary></summary>
    public VoskSpeechRecognition(IInputAudioModule _inputAudioModule)
    {
        inputAudioModule = _inputAudioModule;
        initialize();
    }

    //=============================================================================
    /// <summary></summary>
    public VoskSpeechRecognition initialize()
    {
		Console.WriteLine ("Loading vosk model...");
        Vosk.Vosk.SetLogLevel(-1);

		string speechRecognitionFile = Program.Settings.Vosk.ModelPath;
		if (String.IsNullOrWhiteSpace (speechRecognitionFile)
			|| !Directory.Exists (speechRecognitionFile) )
		{
			ConsoleEx.Error ("Vosk model not found. Please set the path to the model in the settings file.");
			Environment.Exit (1);
			return null;
		}


		// new VoskRecognizer just crashes if the model folder is empty (no exception, just crashes)
		Console.WriteLine ("    note: if a crash happens now, the data in the vosk model folder is probably wrong or empty.");

		// load model
		Model model = new Model (speechRecognitionFile);
		voskRecognizer = new VoskRecognizer (model, inputAudioModule.SampleRate);

		// receive all audio input updates here
		inputAudioModule.AudioInput += AudioInput_AudioInput;

        Console.WriteLine("Vosk model loaded.");
        return this;
    }

    //=============================================================================
    /// <summary></summary>
    private void AudioInput_AudioInput(object sender, AudioInputAvailableEventArgs e)
    {
        processAudio(e);
    }

	//=============================================================================
	/// <summary></summary>
	public void reset ()
	{
		voskRecognizer.Reset ();
	}

    //=============================================================================
    /// <summary></summary>
    void processAudio (AudioInputAvailableEventArgs e)
    {
        string res, rv;
        bool finished = false;

        if (voskRecognizer == null) return;

        try
        {
            if (voskRecognizer.AcceptWaveform(e.AudioData, e.AudioDataLength))
            {
                res = voskRecognizer.Result();
                rv = extractData(res, "text");
                finished = true;

                if (Program.Settings.Vosk.VerboseLevel > 0)
				if (!string.IsNullOrWhiteSpace(rv))
                {
                    Console.WriteLine("Vosk : " + rv);
                }
            }
            else
            {
                res = voskRecognizer.PartialResult();
                rv = extractData(res, "partial");
            }

            if (!string.IsNullOrWhiteSpace(rv))
            {
                var arg = new SpeechRecognitionResultEventArgs(rv, finished);
                SpeechRecognitionResult?.Invoke(this, arg);
            }

        }
        catch
        {
        }
    }


    //=============================================================================
    /// <summary></summary>
    string extractData(string json, string key)
    {
        // read as Json
		JObject jsonDoc = JObject.Parse(json);
		return (string)jsonDoc[key];
    }


}
