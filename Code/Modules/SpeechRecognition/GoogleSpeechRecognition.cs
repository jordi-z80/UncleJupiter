using System;
using System.Collections.Generic;
using System.Diagnostics;
using UncleJupiter.Config;
using Google.Cloud.Speech.V1;
using Google.Protobuf;

namespace UncleJupiter;

public class GoogleSpeechRecognition : ISpeechRecognitionModule
{
    public event EventHandler<SpeechRecognitionResultEventArgs> SpeechRecognitionResult;
    IInputAudioModule inputAudioModule;
    IOutputAudioModule outputAudioModule;

    SpeechClient speechClient;

	GoogleSpeechSettings settings => Program.Settings.GoogleSpeech;
	RecognitionConfig config;
    StreamingRecognitionConfig streamingRecognitionConfig;

    bool recognizing;

    SpeechClient.StreamingRecognizeStream streamingAudio;

	SemaphoreSlim _mutex = new SemaphoreSlim (1);

	//=============================================================================
	/// <summary></summary>
	public GoogleSpeechRecognition(IInputAudioModule _inputAudioModule,
                                    IOutputAudioModule _outputAudioModule)
    {
        inputAudioModule = _inputAudioModule;
        outputAudioModule = _outputAudioModule;

		Environment.SetEnvironmentVariable ("GOOGLE_APPLICATION_CREDENTIALS", Program.Settings.GOOGLE_APPLICATION_CREDENTIALS);

		initialize ();
    }

    //=============================================================================
    /// <summary></summary>
    public GoogleSpeechRecognition initialize()
    {
		try
		{
			speechClient = SpeechClient.Create ();
		}
		catch (Exception e)
		{
			ConsoleEx.Warning ("Speech recognition failed to init. The GOOGLE_APPLICATION_CREDENTIALS in secret.json is probably wrong.");
			ConsoleEx.Error (e.Message);
			Environment.Exit (1);
		}
        
        config = new RecognitionConfig
        {
            Encoding = RecognitionConfig.Types.AudioEncoding.Linear16,
            SampleRateHertz = Program.Settings.InputAudio.Bitrate,
            LanguageCode = Program.Settings.GoogleSpeech.Code,
        };

        streamingRecognitionConfig = new StreamingRecognitionConfig
        {
            Config = config,
            SingleUtterance = true,
            InterimResults = true,
        };

        // receive all audio input updates here
        inputAudioModule.AudioInput += AudioInput_AudioInput;

        return this;
    }

	
    //=============================================================================
    /// <summary></summary>
    private async void AudioInput_AudioInput(object sender, AudioInputAvailableEventArgs e)
    {
		// start recognition only when we receive the signal
        if (!g.RecognitionStarted) return;

        if (!recognizing)
        {
            recognizing = true;
            startRecognition();
        }

        var request = new StreamingRecognizeRequest()
        {
            AudioContent = ByteString.CopyFrom(e.AudioData, 0, e.AudioDataLength)
        };

		// check again recognitionStarted just in case it was turned off before the start of the function
		await _mutex.WaitAsync();
		try
		{
			if (g.RecognitionStarted) streamingAudio.WriteAsync (request).Wait ();
		}
		finally
		{
			_mutex.Release();
		}

    }

    //=============================================================================
    /// <summary></summary>
    void startRecognition()
    {
        streamingAudio = speechClient.StreamingRecognize();


        // Write the initial request with the config.
        streamingAudio.WriteAsync(new StreamingRecognizeRequest()
        {
            StreamingConfig = streamingRecognitionConfig,
        });


        // Setup a Task so that it prints responses as they arrive.
        Task printResponses = Task.Run(OnSpeechResponse);

    }

	static int count = 0;

    //=============================================================================
    /// <summary>This is called whenever google speech has results (definitive or provisional, if Interim is set to true)</summary>
    async Task OnSpeechResponse()
    {
        List<string> results = new List<string>();

        var responseStream = streamingAudio.GetResponseStream();
        while (await responseStream.MoveNextAsync())
        {
			count++;

			StreamingRecognizeResponse response = responseStream.Current;
            foreach (StreamingRecognitionResult result in response.Results)
            {
				bool isFinal = result.IsFinal;
				string finalString = isFinal? "final" : "interim";

                foreach (SpeechRecognitionAlternative alternative in result.Alternatives)
                {
					int level = isFinal ? 1 : 2;
					if (settings.VerboseLevel >= level)
					{
						Console.WriteLine ($"{count} Google ({finalString}): {alternative.Transcript}");
					}

					if (isFinal) results.Add(alternative.Transcript);
                }
            }

            if (results.Count > 0)
            {
                Debug.Assert(results.Count == 1);

				await _mutex.WaitAsync();
				try
				{
					g.RecognitionStarted = false;
					recognizing = false;
					await streamingAudio.WriteCompleteAsync ();
				}
				finally
				{
					_mutex.Release ();
				}


                outputAudioModule.playAudio("computerAccept");


                SpeechRecognitionResult?.Invoke(this, new SpeechRecognitionResultEventArgs(results[0], true));

                break;
            }
        }

    }


}



