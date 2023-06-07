using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NAudio.Gui;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UncleJupiter;

internal partial class Main
{
	VoskSpeechRecognition voskRecognition;
	IOutputAudioModule audioOut;
	IAIModule aiModule;
	EmbeddingDB embeddingDB;

	CommandManager commandManager;

	List<Action> actionList = new List<Action> ();

	// we have to check if a process is running to avoid adding unneeded embeddings.
	Dictionary<string, bool> runningProcesses = new Dictionary<string, bool> ();

	//=============================================================================
	/// <summary></summary>
	public Main (
			CommandManager _commandManager,
			VoskSpeechRecognition rec,
			GoogleSpeechRecognition googleSpeech,
			EmbeddingDB embeddingDB,
			IOutputAudioModule _audioOut,
			IAIModule _aiAgent
			)
	{

		voskRecognition = rec;
		audioOut = _audioOut;
		aiModule = _aiAgent;
		commandManager = _commandManager;
		this.embeddingDB = embeddingDB;

		// the whole program is controlled by the output events of the speech recognition modules
		// and the LLM output events.
		voskRecognition.SpeechRecognitionResult += VoskRecognition_SpeechRecognitionResult;
		googleSpeech.SpeechRecognitionResult += GoogleSpeech_SpeechRecognitionResult;

		ConsoleEx.Warning ($"Ready for commands. Language is {Program.Settings.Language.Name}.");

	}


	//=============================================================================
	/// <summary>This function is called each time Vosk returns a recognized speech as text.
	/// It checks for the computer name (in appSettings-LANGUAGE.json , and if the text is there, set
	/// the "RecognitionStarted" flag so that we can send the data to Google for better recognition.</summary>
	private void VoskRecognition_SpeechRecognitionResult (object sender, SpeechRecognitionResultEventArgs e)
	{
		if (!g.RecognitionStarted)
		{
			foreach (var computerName in Program.Settings.Recognition.ComputerName)
			{
				if (e.Text.Contains (computerName, StringComparison.InvariantCultureIgnoreCase))
				{
					g.RecognitionStarted = true;
					audioOut.playAudio ("computerCue");

					// this avoids getting two "computer" orders (because we're using the computer word without needing to be 'final')
					voskRecognition.reset ();
					break;
				}
			}
		}
	}

	//=============================================================================
	/// <summary>This is called when GoogleSpeech returns a valid recognized speech.
	/// </summary>
	private void GoogleSpeech_SpeechRecognitionResult (object sender, SpeechRecognitionResultEventArgs e)
	{
		// pass the results of the google speech recognition to the main action function.
		Task task = Task.Run (async () => await onValidSpeechReceived (e.Text));
	}


	//=============================================================================
	/// <summary></summary>
	async Task onValidSpeechReceived (string text)
	{
		// First, we check if the text can be automatically parsed via RegEx. This speeds up some orders
		// and reduces GPT usage.
		bool recognized = commandManager.processQuickCommand (text);
		if (recognized) return;

		Console.WriteLine ("Quick-command not identified, asking LLM.");

		// if the command wasn't recognized via RegEX, we send it to OpenAI
		await queryOpenAIAndExecute (text);
	}



	//=============================================================================
	/// <summary>This function calculates the userOrder embedding vector, search the nearest embeddings
	/// and returns a string with instructions for the LLM.</summary>
	async Task<string> calculateCommandInfoInstructions (string userOrder)
	{
		// first we calculate the embeddings for the user order
		var userOrderEmbedding = await aiModule.createEmbedding (userOrder);
		if (userOrderEmbedding == null) return null;

		// now, get a list of the nearest embeddings, sorted
		var embeddingList = embeddingDB.getNearestEmbeddings (userOrderEmbedding);

		bool requiresDate = false;

		// For the moment I'll just add the first 10 embeddings to the prompt.
		// Maybe later we can use a more sophisticated approach
		string embeddingText = "";

		int addedEmbeddings = 0;
		int MaxEmbeddingsToAdd = Program.Settings.Prompt.MaxEmbeddings;
		runningProcesses.Clear ();

		for (int i = 0; i < embeddingList.Count; i++)
		{
			if (addedEmbeddings >= MaxEmbeddingsToAdd) break;

			// get the command for the embedding
			string id = embeddingList[i].embeddingItem.EmbeddingId;
			var commandInfo = commandManager.getCommandById (id);
			if (commandInfo == null) continue;

			// if the command is injected always, we'll add it later
			if (commandInfo.injectAlways) continue;

			addEmbeddingIfNeeded (commandInfo);
		}

		// inject all commands with the "injectAlways" flag
		foreach (var commandInfo in commandManager.Where ( cmd => cmd.injectAlways ))
		{
			addEmbeddingIfNeeded (commandInfo);
		}

		if (requiresDate)
		{
			string dt = DateTime.Now.ToString ("yyyy/MM/dd HH:mm:ss");
			embeddingText += $"\nNow is {dt}\n";
		}

		return embeddingText;

		//=============================================================================
		/// <summary></summary>
		bool addEmbeddingIfNeeded (CommandInfo ci)
		{
			// check if the command requires a program running
			if (!String.IsNullOrWhiteSpace (ci.isProcessRunning))
			{
				string processName = ci.isProcessRunning;

				// checking if a process is running is slow, so we cache the results
				bool present = runningProcesses.TryGetValue (processName, out bool isRunning);
				if (!present)
				{
					isRunning = CommandManager.isProcessRunning (processName);
					runningProcesses.Add (processName, isRunning);
				}

				// if the process is not running, we skip this command
				if (!isRunning) return false;
			}

			embeddingText += ci.llmText + "\n";
			if (ci.requiresDate) requiresDate = true;
			addedEmbeddings++;
			return true;

		}
	}


	//=============================================================================
	/// <summary></summary>
	async Task queryOpenAIAndExecute (string userOrder)
	{
		try
		{
			// calculate the prompt for the LLM
			string llmExtraInstructions = await calculateCommandInfoInstructions (userOrder);
			if (llmExtraInstructions == null) return;

			// ask OpenAI for the answer
			string jsonAnswer = await aiModule.giveOrder (userOrder,llmExtraInstructions);
			if (jsonAnswer == null) return;

			Console.WriteLine ("-------------------\nOpenAI answer :\n" + jsonAnswer);

			audioOut.playAudio ("computerFinished");


			// trap OpenAI errors
			if (jsonAnswer.StartsWith ("Error {")) throw new Exception ("OpenAI error");

			// avoid problems with unwanted leading text
			jsonAnswer = jsonAnswer.Substring (jsonAnswer.IndexOf ("{"));

			// same for the text at the end
			jsonAnswer = jsonAnswer.Substring (0,jsonAnswer.LastIndexOf ("}")+1);

			// deserialize json into LLMCommandList (will fail if the json is not valid)
			LLMCommandList commands = JsonConvert.DeserializeObject<LLMCommandList> (jsonAnswer);


			// execute each order sequentially
			foreach (var cmd in commands.commandList)
			{
				ConsoleEx.Warning (cmd.explanation);
				commandManager.runCommand (cmd.action,cmd.param);
			}
		}
		catch (Exception ex)
		{
			ConsoleEx.Error ("Error : "+ex.Message);
		}
	}


	

	//=============================================================================
	/// <summary></summary>
	public void Run ()
	{
		Action action;

		// Some functions must be run in the main thread. This Run function only acts as a dispatcher.
		while (true)
		{
			Task.Delay (100).Wait ();

			while (actionList.Count > 0)
			{
				lock (actionList)
				{
					action = actionList[0];
					actionList.RemoveAt (0);
				}

				action ();
			}
		}
	}

	//=============================================================================
	/// <summary></summary>
	internal void addMainThreadAction (Action value)
	{
		lock (actionList)
		{
			actionList.Add (value);
		}
	}


}
