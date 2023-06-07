using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace UncleJupiter;

//=============================================================================
/// <summary></summary>
internal class EmbeddingDB
{
	const int DBSaveVersion = 0;

	// each language has its own embedding database
	string EmbeddingsFile => $"./embeddingDB/embeddingData-{Program.Settings.Language.Code}.bin";

	Dictionary<string, EmbeddingItem> embeddingDict;


	//=============================================================================
	/// <summary></summary>
	public EmbeddingDB ()
	{
		embeddingDict = new Dictionary<string, EmbeddingItem>();

		try
		{
			loadDB ();
		}
		catch (Exception e)
		{
			ConsoleEx.Error (e.Message);
		}
	}



	//=============================================================================
	/// <summary></summary>
	public async Task calculateEmbeddings (IAIModule ai,List<CommandInfo> commands)
	{
		bool embeddingsModified = false;

		// calculate the embeddings for all commands that need it (this could be done in parallel)
		foreach (var cmd in commands)
		{
			bool requiresRecalculation = false;

			if (!embeddingDict.TryGetValue (cmd.name, out EmbeddingItem embedding)) requiresRecalculation = true;
			else
			{
				DateTime fileStamp = File.GetLastWriteTime (cmd.file);
				if (fileStamp > embedding.Timestamp) requiresRecalculation = true;
				if (embedding.Embedding == null) requiresRecalculation = true;
			}


			if (requiresRecalculation)
			{
				embeddingsModified = true;
				await recalculateEmbedding (ai, cmd);
			}
		}

		// save embeddingDict for next time
		if (embeddingsModified)
		{
			saveDB ();
		}

		
	}



	//=============================================================================
	/// <summary></summary>
	private async Task recalculateEmbedding (IAIModule ai, CommandInfo cmd)
	{
		Console.WriteLine ($"Calculating embedding for '{cmd.name}'.");

		List<double> embeddingData = await ai.createEmbedding (cmd.llmText);
		if (embeddingData == null) return;

		EmbeddingItem embedding = new EmbeddingItem()
		{
			EmbeddingId = cmd.name,
			Embedding = embeddingData.ToArray(),
			Timestamp = DateTime.Now
		};

		// replace if present
		embeddingDict.Remove (cmd.name);
		embeddingDict.Add (cmd.name,embedding);
	}

	//=============================================================================
	/// <summary>Returns a list of embeddings, sorted by distance to the user order.</summary>
	internal List<(double distance, EmbeddingItem embeddingItem)> getNearestEmbeddings (List<double> _userOrderEmbedding)
	{
		var userOrderEmbedding = _userOrderEmbedding.ToArray();
		var allEmbeddingDistances = new List<(double distance, EmbeddingItem embeddingItem)> ();

		foreach (var val in embeddingDict.Values) 
		{
			double dst = cosineSimilarityBetweenEmbeddings (val.Embedding, userOrderEmbedding);

			allEmbeddingDistances.Add ((dst, val));
		}

		allEmbeddingDistances.Sort ( (a,b) => b.Item1.CompareTo (a.Item1));

		return allEmbeddingDistances;
	}

	//=============================================================================
	/// <summary></summary>
	double cosineSimilarityBetweenEmbeddings (double []e1,double[] e2)
	{
		if (e1.Length!= e2.Length) throw new Exception ("Embeddings should all have the same dimension.");

		var dotProduct = e1.Zip (e2, (a,b) => a * b).Sum();
		var magnitude1 = Math.Sqrt (e1.Sum (x => x * x));
		var magnitude2 = Math.Sqrt (e2.Sum (x => x * x));

		return dotProduct / (magnitude1 * magnitude2);
	}

	//=============================================================================
	/// <summary></summary>
	internal EmbeddingItem getEmbeddingById (string id)
	{
		if (embeddingDict.TryGetValue (id, out EmbeddingItem embedding)) return embedding;
		return null;
	}


#region Save/load DB

	//=============================================================================
	/// <summary></summary>
	void saveDB ()
	{
		// I prefer writing in binary format than using json or xml for double arrays.
		var ms = new MemoryStream ();
		BinaryWriter bw = new BinaryWriter (ms);

		int version = DBSaveVersion;
		bw.Write (version);

		foreach (var it in embeddingDict.Values)
		{
			bw.Write (it.EmbeddingId);
			bw.Write (it.Timestamp.Ticks);


			byte[] bData = new byte[it.Embedding.Length * sizeof (double)];
			Buffer.BlockCopy (it.Embedding.ToArray (), 0, bData, 0, bData.Length);

			bw.Write (bData.Length);
			bw.Write (bData);
		}

		Directory.CreateDirectory (Path.GetDirectoryName (EmbeddingsFile));
		File.WriteAllBytes (EmbeddingsFile, ms.ToArray ());
	}

	//=============================================================================
	/// <summary></summary>
	private void loadDB ()
	{
		byte[] bytes = File.ReadAllBytes (EmbeddingsFile);

		MemoryStream ms = new MemoryStream (bytes);
		BinaryReader br = new BinaryReader (ms);

		// fixme: instead of throwing on wrong version, I should provide different version loaders. Will do when needed.
		int version = br.ReadInt32 ();
		if (version != DBSaveVersion) throw new Exception ("Wrong version of the embeddings file.");

		while (ms.Position < ms.Length)
		{
			// read the embedding id
			string id = br.ReadString ();
			if (id == null) break;

			EmbeddingItem item = new EmbeddingItem ();
			item.EmbeddingId = id;

			// read the timestamp
			item.Timestamp = new DateTime (br.ReadInt64 ());

			// read the embedding
			int dataLength = br.ReadInt32 ();
			byte[]bData = br.ReadBytes (dataLength);

			double[]dData=new double[dataLength / sizeof (double)];
			Buffer.BlockCopy (bData, 0, dData, 0, dataLength);

			item.Embedding = dData;

			// add to the dictionary
			embeddingDict.Add (id, item);
		}
	}
#endregion

}
