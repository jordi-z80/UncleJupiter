using System.Diagnostics;

namespace UncleJupiter;



[DebuggerDisplay ("id:{EmbeddingId} ts:{Timestamp}")]
class EmbeddingItem
{
	public string EmbeddingId { get; set; }
	public double[] Embedding;
	public DateTime Timestamp { get; set; }
}
