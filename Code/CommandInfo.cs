using System.Diagnostics;

namespace UncleJupiter;

//=============================================================================
/// <summary></summary>
[DebuggerDisplay ("Command: {name} Action: {action},{param}")]
class CommandInfo
{
	public string name { get; set; }						// name of the command, used for indexing embeddings
	public List<string> regExp { get; set; }				// list of regular expressions to match the quick - command
	public List<string> invalidWords { get; set; }          // list of invalid words that, if present, must discard the quick - command
	public List<string> allowedWords { get; set; }			// list of words that, even if they're set as quick-command cancellators, are accepted in this quick-command.

	public string isProcessRunning { get; set; }			// if not empty, the command can only be issued if the specified program is running.

	public string action { get; set; }						// action to perform
	public string param { get; set; }						// parameter to pass to the action

	public string llmText { get; set; }						// descriptive text for the LLM

	// Flags
	public List<string> flags { get; set; }                 // list of optional flags for the command
	public bool isQuickCommandDisabled => flags.Contains (g.FlagDisableQuickCommand);
	public bool requiresDate => flags.Contains (g.FlagRequiresDate);
	public bool injectAlways => flags.Contains (g.FlagInjectAlways);


	// -- Runtime variables (not in the JSON file)

	// Where this command was defined 
	public string file { get; set; }
}
