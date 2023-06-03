namespace UncleJupiter;

internal partial class Main
{
	class LLMCommandList
	{
		public List<LLMCommand> commandList { get; set;}
		public class LLMCommand
		{
			public string action { get; set; }
			public string param { get; set; }
			public string explanation { get; set; }
		}
	}


}
