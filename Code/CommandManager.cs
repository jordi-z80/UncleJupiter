using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using NAudio.Wave;
using Newtonsoft.Json;

namespace UncleJupiter;

//=============================================================================
/// <summary></summary>
internal partial class CommandManager
{
    IProgramRunner programRunner;
	IOutputAudioModule audioOut;
	List<CommandInfo> commands;

	List<string> ignorableLeadingTexts;                     // list of texts that can be ignored at the beginning of the command (for quick-command)
	List<string> quickCommandDiscardWords;					// list of words that, if present, must call the LLM to decide the command to follow.

	EmbeddingDB embeddingDB;

    //=============================================================================
    /// <summary></summary>
    public CommandManager (IAIModule ai,IProgramRunner programRunner, IOutputAudioModule audioOut,EmbeddingDB embeddingDB)
    {
        this.programRunner = programRunner;
		this.audioOut = audioOut;
		this.embeddingDB = embeddingDB;

		// load commands
        initialize();

		// calculate embeddings if needed
		embeddingDB.calculateEmbeddings (ai,commands).Wait();
	}


	//=============================================================================
	/// <summary>Scans all available commands, uses RegEx to detect parameters, and runs the command if possible.</summary>
	public bool processQuickCommand (string command)
    {
		// never allow quick commands if the level is 0
		if (Program.Settings.Config.QuickCommandLevel == 0) return false;

		// remove some words from the start of the command
		command = prefilterCommand (command);

		// check all commands to see if the command matches any RegEx
        foreach (var cmd in commands)
        {
			if (cmd.flags.Contains (g.DisableQuickCommand)) continue;

			// Check if the command can be used as quick-command by looking at the valid and invalid words
			if (commandHasInvalidWords (command, cmd)) continue;

			foreach (var regExp in cmd.regExp)
			{
				Match match = Regex.Match (command, regExp, RegexOptions.IgnoreCase);
				if (match.Success)
				{
					if (!String.IsNullOrWhiteSpace (cmd.isProcessRunning))
					{
						if (!isProcessRunning (cmd.isProcessRunning)) continue;
					}

					string finalParam = expandParam (cmd.param, match);
					string action = cmd.action;

					runCommand (action, finalParam);

					audioOut.playAudio ("computerFinished");

					return true;
				}
			}
        }

		// no quick-action available
        return false;
    }

	//=============================================================================
	/// <summary></summary>
	public bool runCommand (string actionName, string mainParameter)
	{
		string expandedActionName = expandParam (actionName, null);

		// To check Path.IsPathRooted, we must remove the quotes if present
		string noQuotesActionName = expandedActionName.Trim ('"');

		// if the action is a path, then it's a program to run
		if (Path.IsPathRooted (noQuotesActionName)) 
		{
			execProgram (expandedActionName, mainParameter);
			return true;
		}

		if (Uri.TryCreate (expandedActionName,UriKind.Absolute, out _)) 
		{
			string url = expandedActionName + Uri.EscapeDataString (mainParameter);
			launchUrl (url);
			return true;
		}

		switch (expandedActionName)
		{
			// run is used to run a program with a loosely defined name. If you have the exact command, use "exec"
			case "run":
				programRunner.runProgramByShortName (mainParameter);
				break;

			case "url":
				launchUrl (mainParameter);
				break;

			default:
				Console.WriteLine ("Unknown command " + actionName);
				return false;
		}

		return true;

	}

	//=============================================================================
	/// <summary>Find the next space not inside quotes</summary>
	int findNextSpace (string text,int index=0)
	{
		bool insideQuotes=false;
		while (index < text.Length)
		{
			if (text[index] == '"')
			{
				insideQuotes = !insideQuotes;
				index++;
				continue;
			}

			if (!insideQuotes)
			{
				if (text[index] == ' ') return index;
			}

			index++;
		}
		return -1;
	}

	//=============================================================================
	/// <summary></summary>
	private void execProgram (string exeName,string parameters)
	{
		try
		{
			// run exeName with paramName params
			Process process = new Process ();
			ProcessStartInfo startInfo = new ProcessStartInfo (exeName, parameters);
			process.StartInfo = startInfo;
			process.Start ();
		}
		catch (Exception ex)
		{
			ConsoleEx.Error (ex.Message);
		}

	}

	//=============================================================================
	/// <summary></summary>
	void launchUrl (string url)
	{
		var psi = new ProcessStartInfo (url)
		{
			FileName = url,
			UseShellExecute = true,
		};
		Process.Start (psi);
	}

	//=============================================================================
	/// <summary></summary>
	internal CommandInfo getCommandById (string id)
	{
		return commands.FirstOrDefault (x => x.name == id);
	}
}
