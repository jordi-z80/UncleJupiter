using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;

namespace UncleJupiter;

internal class OpenAIModule : IAIModule
{

    OpenAIService api;

	//=============================================================================
	/// <summary></summary>
	public OpenAIModule ()
	{
		initialize ();
	}

    //=============================================================================
    /// <summary></summary>
    void initialize()
    {
		if (String.IsNullOrWhiteSpace (Program.Settings.OpenAI?.ApiKey))
		{
			ConsoleEx.Error ("OpenAI ApiKey not found");
			Environment.Exit (1);
			return;
		}
        api = new OpenAIService(new OpenAiOptions()
        {
            ApiKey = Program.Settings.OpenAI.ApiKey
        });
    }




    //=============================================================================
    /// <summary></summary>
    public async Task<string> giveOrder(string userOrder, string llmExtraInstructions = null)
    {
		string languageCode = Program.Settings.Language.Code;
		string languageName = Program.Settings.Language.Name;

		// if lots of orders must be used, we could use embeddings here to expand the available commands
		string prompt = File.ReadAllText ($"Settings/{languageCode}/prompt.txt");
		string tPrompt = RemoveComments (prompt);
		tPrompt = tPrompt
						.Replace ("%LANGUAGE%", languageName)
						.Replace ("%VOICE_ORDER%",userOrder)
			;

		if (llmExtraInstructions != null) tPrompt = tPrompt.Replace ("%LLM_INSTRUCTIONS%", llmExtraInstructions) ;

        var Messages = new List<ChatMessage> { ChatMessage.FromUser(tPrompt) };

		Console.WriteLine ("OpenAI Prompt : " + tPrompt);

		var completionResult = await api.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
        {
            Messages = Messages,
            Model = Program.Settings.OpenAI.Model,			// Models.ChatGpt3_5Turbo,
            //MaxTokens = 120								// optional
        });


        if (completionResult.Successful)
        {
            return completionResult.Choices.First().Message.Content;
        }
        else
        {
			ConsoleEx.Error (completionResult.Error.ToString());

			if (completionResult.Error.Code == "invalid_api_key")
			{
				ConsoleEx.Warning ("OpenAI api key inside secrets.json is probably wrong. GPT won't work.");
			}

			return null;
        }

    }

	//=============================================================================
	/// <summary></summary>
	private string RemoveComments (string prompt)
	{
		string[] split = prompt.Split ('\n');

		string rv = "";
		foreach (string line in split) if (!line.StartsWith ("//")) rv += line + "\n";

		return rv;
	}


	//=============================================================================
	/// <summary></summary>
	public async Task<List<double>> createEmbedding (string text)
	{
		var request = new EmbeddingCreateRequest ()
		{
			InputAsList = new List<string> { text },
			Model = Models.TextEmbeddingAdaV2,
		};

		var results = await api.CreateEmbedding (request);

		if (results.Successful)
		{
			var doubleList = results.Data.FirstOrDefault ().Embedding;
			return doubleList;
		}
		else
		{
			ConsoleEx.Error (results.Error.ToString());

			if (results.Error.Code == "invalid_api_key")
			{
				ConsoleEx.Warning ("OpenAI api key inside secrets.json is probably wrong. GPT won't work.");
			}

			return null;
		}

	}




}
