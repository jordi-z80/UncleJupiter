using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncleJupiter.Config;


public class InputAudioSettings
{
    public int Bitrate { get; set; }
    public int BufferInMilliseconds { get; set; }
}

public class OutputAudioSettings
{
    public string ComputerCue { get; set; }
    public string ComputerAccept { get; set; }
    public string ComputerFinished { get; set; }
}

public class LanguageSettings
{
    public string Code { get; set; }
    public string Name { get; set; }
}

public class GoogleSpeechSettings
{
    public string Code { get; set; }
	public int VerboseLevel { get; set; }
}

public class VoskSettings
{
    public string ModelPath { get; set; }
    public int VerboseLevel { get; set; }
}

public class RecognitionSettings
{
    public string[] ComputerName { get; set; }
}

public class OpenAISettings
{
	public string Model { get; set; }
	public string ApiKey { get; set; }
}

public class Config
{
	public int QuickCommandLevel { get; set; }
}

public class AssistantSettings
{
    public InputAudioSettings InputAudio { get; set; }
    public LanguageSettings Language { get; set; }
    public OutputAudioSettings OutputAudio { get; set; }

    public RecognitionSettings Recognition { get; set; }
	public Config Config { get; set; }

    // implementation settings
    public GoogleSpeechSettings GoogleSpeech { get; set; }
    public VoskSettings Vosk { get; set; }
	public OpenAISettings OpenAI { get; set; }

	public string GOOGLE_APPLICATION_CREDENTIALS { get; set; }
}


