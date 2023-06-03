using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace UncleJupiter;

internal class OutputAudioModule : IOutputAudioModule
{
    WaveStream computerCue, computerFinished, computerAccept;
	WaveStream default1, default2;

    //=============================================================================
    /// <summary></summary>
    public OutputAudioModule()
    {
        initialize();
    }

    //=============================================================================
    /// <summary></summary>
    void initialize()
    {
		default1 = loadAudio ("Content/audio/default1.mp3");
		default2 = loadAudio ("Content/audio/default2.mp3");

		computerCue = loadAudio (Program.Settings.OutputAudio.ComputerCue);
        computerAccept = loadAudio (Program.Settings.OutputAudio.ComputerAccept);
        computerFinished = loadAudio (Program.Settings.OutputAudio.ComputerFinished);
    }

	AudioFileReader loadAudio (string fn)
	{
		try
		{
			return new AudioFileReader (fn);
		}
		catch 
		{ 
			return null; 
		}

	}

    //=============================================================================
    /// <summary></summary>
    public void playAudio(string type)
    {
        WaveStream snd = null;
        switch (type)
        {
            case "computerCue": snd = computerCue; if (snd == null) snd = default1; break;
            case "computerAccept": snd = computerAccept; if (snd == null) snd = default1; break;
            case "computerFinished": snd = computerFinished; if (snd == null) snd = default2; break;
            default: return;
        }

        WaveOutEvent player = new WaveOutEvent();
        snd.Position = 0;
        player.Init(snd);
        player.Play();

    }


}
