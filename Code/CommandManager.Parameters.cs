using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UncleJupiter;

internal partial class CommandManager
{

	//=============================================================================
	/// <summary></summary>
	public static bool isProcessRunning (string processName)
	{
		// Check if the processExe .exe is run by the system
		var namedProcess = Process.GetProcessesByName (processName);
		if (namedProcess.Length > 0) return true;
		return false;
	}


	//=============================================================================
	/// <summary></summary>
	string[] tokenizeString (string command)
	{
		string[] words = command.Split (' ', ',', '.', '\t', '\r');

		return words;
	}

	//=============================================================================
	/// <summary></summary>
	private bool commandHasInvalidWords (string[] tokenizedCommand, CommandInfo cmd)
	{
		// first check global invalid words that can be overriden in the command
		// note: check is done word by word, otherwise "then" may match "authentic"
		bool invalidWordFound = false;
		foreach (var word in quickCommandDiscardWords)
		{
			var found = tokenizedCommand.FirstOrDefault (p => p.Equals (word, StringComparison.OrdinalIgnoreCase));
			if (found == null) continue;

			var wordIsAllowed = cmd.allowedWords.Find ( w => w.Equals (word, StringComparison.OrdinalIgnoreCase));
			if (wordIsAllowed == null)
			{
				invalidWordFound = true;
				break;
			}
		}

		if (invalidWordFound) return true;

		// now for the invalid words of the command
		foreach (var word in cmd.invalidWords)
		{
			var found = tokenizedCommand.FirstOrDefault (p => p.Equals (word, StringComparison.OrdinalIgnoreCase));
			if (found != null) return true;
		}

		return false;
	}

	//=============================================================================
	/// <summary>Remove leading texts that mean nothing.</summary>
	string prefilterCommand (string command)
	{
		while (true)
		{
			foreach (var ign in ignorableLeadingTexts)
			{
				if (command.StartsWith (ign))
				{
					command = command.Substring (ign.Length).Trim ();
					break;
				}
			};

			return command;
		}
	}

	//=============================================================================
	/// <summary>Tries all possibilites to transform a parameter into something usable.</summary>
	string getParamValue (string paramName, Match match)
	{
		string rv = null;

		if (match!=null && match.Groups.TryGetValue (paramName, out var value))
		{
			rv = value.Value;
		}
		else
		{
			string settingsString = Program._RootConfiguration[paramName];
			if (settingsString != null) rv = settingsString;
		}

		if (rv == null) return null;

		// special cases
		switch (paramName)
		{
			case "timeDeltaParam":
				rv = tryToTranslateToTime (rv);
				break;
		}
		return rv;
	}

	//=============================================================================
	/// <summary>Lame way to try to convert a text string into time. This is only for very common cases using quick-commands,
	/// this should be let to the LLM to fix the string.</summary>
	string tryToTranslateToTime (string str)
	{
		// regex to match 1 minute or 30 minutes
		string pattern = @"(\d+) minute(s)?";
		var match = Regex.Match (str, pattern);
		if (match.Success)
		{
			int minutes = int.Parse (match.Groups[1].Value);
			return String.Format ("{0:00}:{1:00}:{2:00}", 0, minutes, 0);
		}

		// regex to match 1 second or 30 seconds
		pattern = @"(\d+) second(s)?";
		match = Regex.Match (str, pattern);
		if (match.Success)
		{
			int seconds = int.Parse (match.Groups[1].Value);
			return String.Format ("{0:00}:{1:00}:{2:00}", 0, 0, seconds);
		}

		// regex to match 1 hour or 30 hours
		pattern = @"(\d+) hour(s)?";
		match = Regex.Match (str, pattern);
		if (match.Success)
		{
			int hours = int.Parse (match.Groups[1].Value);
			return String.Format ("{0:00}:{1:00}:{2:00}", hours, 0, 0);
		}

		if (str == "an hour") return "01:00:00";

		return str;
	}

	//=============================================================================
	/// <summary></summary>
	private string expandParam (string param, Match match)
	{
		if (param == null) return param;

		int start = param.Length;


		while (true)
		{
			int end = start;
			if (end <= 0) break;

			// search the last string that is between two %, e.g. %mainParam%, %FSCMD% or just %%
			end = param.LastIndexOf ('%', end - 1);
			if (end < 0) break;

			start = param.LastIndexOf ('%', end - 1);
			if (start < 0) break;

			string paramName = param.Substring (start + 1, end - start - 1);

			// If paramName is empty, replace it with just one %
			if (String.IsNullOrWhiteSpace (paramName))
			{
				param = param.Substring (0, start) + "%" + param.Substring (end + 1);
				continue;
			}


			string tVal = getParamValue (paramName, match);


			if (tVal != null)
			{
				param = param.Substring (0, start) + escape (tVal) + param.Substring (end + 1);
				continue;
			}


			// if this is reached, either there's a bug in the json, or there's a missing configuration 
			// assignment (which may be an Environment Macro, a commandline parameter, ...)
			ConsoleEx.Error ($"Error: The macro %{paramName}% was not correctly resolved.");
			end--;
			if (end < 0) break;
		}


		return param;


		//=============================================================================
		/// <summary></summary>
		string escape (string text)
		{
			// Fixme: This is not a good way to decide this...
			bool needsScape = false;
			if (param.StartsWith ("https://")) needsScape = true;
			if (param.StartsWith ("http://")) needsScape = true;

			if (needsScape)
			{
				return Uri.EscapeDataString (text);

			}
			return text;

		}
	}


}
