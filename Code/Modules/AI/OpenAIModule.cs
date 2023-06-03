using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenAI.GPT3;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels;
using Microsoft.Extensions.Configuration;

namespace UncleJupiter;

internal class OpenAIModule : IAIModule
{

    OpenAIService api;

	bool warningDisplayed = false;

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
		string query = File.ReadAllText ($"Settings/{languageCode}/query.txt");
		string tquery = query
						.Replace ("%LANGUAGE%", languageName)
						.Replace ("%VOICE_ORDER%",userOrder)
			;

		if (llmExtraInstructions != null) tquery = tquery.Replace ("%LLM_INSTRUCTIONS%", llmExtraInstructions) ;

        var Messages = new List<ChatMessage> { ChatMessage.FromUser(tquery) };

		Console.WriteLine ("OpenAI Query : " + tquery);

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

			if (completionResult.Error.Code == "invalid_api_key" && !warningDisplayed)
			{
				warningDisplayed = true;
				ConsoleEx.Warning ("OpenAI api key inside secrets.json is probably wrong. GPT won't work.");
			}

			return null;
        }

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

			if (results.Error.Code == "invalid_api_key" && !warningDisplayed)
			{
				warningDisplayed = true;
				ConsoleEx.Warning ("OpenAI api key inside secrets.json is probably wrong. GPT won't work.");
			}

			return null;
		}

	}




}
