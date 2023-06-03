using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncleJupiter;

public interface IAIModule
{
	Task<string> giveOrder (string userOrder, string llmExtraInstructions = null);

	// List<double> should probably be a struct
	Task<List<double>> createEmbedding (string text);
}
