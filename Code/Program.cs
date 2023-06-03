using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace UncleJupiter;


internal static class Program
{

	public static IServiceProvider ServiceProvider { get; private set; }
	public static Config.AssistantSettings Settings { get; private set; }
	public static IConfigurationRoot _RootConfiguration { get; private set; }
	public static string WorkingDirectory { get; internal set; }

	//=============================================================================
	/// <summary></summary>
	[STAThread]
	static void Main(string []args)
	{
		WorkingDirectory = Environment.CurrentDirectory;

		Console.WriteLine ("Loading location : "+ WorkingDirectory);

		if (!loadSettings (args)) return;



		// setup the dependency injection
		var builder = Host.CreateDefaultBuilder();
		builder.ConfigureServices((context, services) =>
		{
			//services.AddSingleton<MainForm>();
			services.AddSingleton<Main> ();

			services.AddSingleton<IInputAudioModule, MicrophoneInputAudioModule>();
			services.AddSingleton<IOutputAudioModule, OutputAudioModule>();
			services.AddSingleton<VoskSpeechRecognition>();
			services.AddSingleton<GoogleSpeechRecognition>();
			services.AddSingleton<IAIModule,OpenAIModule>();
			services.AddSingleton<IProgramRunner, LameProgramRunner>();
			services.AddSingleton<CommandManager> ();
			services.AddSingleton<EmbeddingDB> ();

		});

		var host = builder.Build();
		ServiceProvider = host.Services;

		// microphone is required
		var inputAudio = ServiceProvider.GetRequiredService<IInputAudioModule>();
		if (!inputAudio.InputAvailable) return;

		ServiceProvider.GetRequiredService<Main> ().Run();
	}

	//=============================================================================
	/// <summary></summary>
	static bool loadSettings (string []args)
	{
		// build a configuration object
		var cfgBuilder = new ConfigurationBuilder ()
			.SetBasePath (WorkingDirectory)
			.AddJsonFile ("Settings/appSettings.json", optional: false, reloadOnChange: true)
			.AddJsonFile ("Settings/secrets.json", optional: false, reloadOnChange: true)
			.AddJsonFile ("Settings/macros.json", optional: true, reloadOnChange: true)
			.AddCommandLine (args)
			.AddEnvironmentVariables ()
			;

		// build the initial configuration
		try
		{
			_RootConfiguration = cfgBuilder.Build ();
		}
		catch (Exception e)
		{
			ConsoleEx.Error (e.Message);
			ConsoleEx.Warning ("Remember to rename and edit secret.json");
			return false;
		}

		// get the language code from the configuration
		string languageCode = _RootConfiguration["Language:Code"];
		if (languageCode == null) { throw new Exception ("Invalid language code "); }

		Console.WriteLine ($"Loading configuration for language {languageCode}.");

		// add the language specific configuration file and rebuild the configuration
		cfgBuilder
				.AddJsonFile ($"Settings/{languageCode}/appSettings-{languageCode}.json", optional: false, reloadOnChange: true)
				.AddJsonFile ($"Settings/{languageCode}/quickCommandSettings.json", optional: true, reloadOnChange: true)
			;


		try
		{
			_RootConfiguration = cfgBuilder.Build ();
		}
		catch (Exception e)
		{
			ConsoleEx.Error (e.Message);
			return false;
		}

		// bind the configuration to the settings object
		Settings = new Config.AssistantSettings ();
		_RootConfiguration.Bind (Settings);

		return true;
	}


}