using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace UncleJupiter;

internal partial class CommandManager
{

	//=============================================================================
	/// <summary></summary>
	void initialize ()
	{
		loadJsonCommands ();

		ignorableLeadingTexts = Program._RootConfiguration
										.GetSection (nameof (ignorableLeadingTexts))
										.Get<List<string>> ();
		if (ignorableLeadingTexts == null) ignorableLeadingTexts = new List<string> ();

		quickCommandDiscardWords = Program._RootConfiguration
										.GetSection (nameof (quickCommandDiscardWords))
										.Get<List<string>> ();
		if (quickCommandDiscardWords == null) quickCommandDiscardWords = new List<string> ();
	}

	//=============================================================================
	/// <summary></summary>
	private void loadJsonCommands ()
	{
		commands = new List<CommandInfo> ();

		// Iterate all json files in Settings/Commands/<language> folder
		string path = Path.Combine (Program.WorkingDirectory, "Settings", Program.Settings.Language.Code, "Commands");

		if (!Directory.Exists (path))
		{
			ConsoleEx.Error ($"Folder {path} does not exist. No commands loaded. Program will probably do nothing.");
			return;
		}

		try
		{
			// files are sorted by file name (not by folder name), so that we can give priority by the name of the file
			var fileList = Directory.GetFiles (path, "*.json", SearchOption.AllDirectories).ToList ();
			fileList.Sort ((p1, p2) => Path.GetFileName (p1).CompareTo (Path.GetFileName (p2)));

			foreach (var file in fileList)
			{
				Console.WriteLine ($"Processing file {file}.");

				// load the json file
				string jsonText = File.ReadAllText (file);

				// get the main array of commands
				var list = JsonConvert.DeserializeObject<List<CommandInfo>> (jsonText);

				foreach (var cmd in list)
				{
					// fill empty parameters
					processEmptyParameters (cmd);

					// check for mandatory parameters, ignore command if not present
					if (!checkMandatoryParameters (cmd)) continue;

					// check for identifier duplicates
					if (commands.Where (p => p.name == cmd.name).Count () > 0)
					{
						ConsoleEx.Error ($"Command id is duplicate : {cmd.name}. Ignoring command.");
						continue;
					}

					cmd.file = file;

					Console.WriteLine ($"Command {cmd.name} loaded.");
					commands.Add (cmd);
				}
			}

		}
		catch (Exception ex)
		{
			ConsoleEx.Error ("Error loading commands : " + ex);
		}
	}

	//=============================================================================
	/// <summary>Fills empty parameters</summary>
	private void processEmptyParameters (CommandInfo cmd)
	{
		if (cmd.regExp == null) cmd.regExp = new List<string> ();
		if (cmd.invalidWords == null) cmd.invalidWords = new List<string> ();
		if (cmd.flags == null) cmd.flags = new List<string> ();
		if (cmd.allowedWords == null) cmd.allowedWords = new List<string> ();
	}

	//=============================================================================
	/// <summary>Check mandatory parameter presence. Returns true if configuration is valid, false otherwise.</summary>
	bool checkMandatoryParameters (CommandInfo cmd)
	{
		bool hasRegExp = cmd.regExp.Count > 0;
		bool hasAction = !String.IsNullOrWhiteSpace (cmd.action);
		bool hasParam = !String.IsNullOrWhiteSpace (cmd.param);

		// we must have either all or none of them
		if (hasRegExp || hasAction || hasParam)
		{
			if (!hasRegExp)
			{
				ConsoleEx.Error ($"Command {cmd.name} has no regular expression. Ignoring command.");
				return false;
			}
			if (!hasAction)
			{
				ConsoleEx.Error ($"Command {cmd.name} has no action. Ignoring command.");
				return false;
			}
			if (!hasParam)
			{
				ConsoleEx.Error ($"Command {cmd.name} has no param. Ignoring command.");
				return false;
			}
		}

		if (String.IsNullOrWhiteSpace (cmd.llmText))
		{
			ConsoleEx.Error ($"Command {cmd.param} has no llmText. Ignoring command.");
			return false;
		}

		return true;
	}


}
