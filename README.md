

# Uncle, I want to see planet Jupiter. 

My 4-years old niece loves to see planets on the computer. I thought it'd be fun to make a computer assistant just to see her face when the computer did what she said.
At the end I added a few more bells and whistles to help me with my daily tasks and easily add new commands. 

I know there are many PC assistants out there, but most of them won't work well (or not at all) on our maiden language.

Still, it's quite fun to play with and very flexible.


# A GPT-powered computer assistant.

This is a very naive computer assistant. At a glace, it works as follows:

* It listens from your microphone until a keyword ("computer") is detected.
* A sound cue is played. THEN you can issue your order.
* The speech is recognized (single utterance).
* It uses RegEx to try to understand the order. If the order is short and can be understood, it tries to run the order directly.
* If not, a prompt is created and sent to OpenAI-GPT.
* With the GPT answer, tries to run the order again.

As it is now, it could be configured to use any language with Vosk and Google Speech support, as long as you configure it correctly and rewrite the commands for the new language.


# Warning 

This program as is uses **paid services**. It's important to keep a close eye on your OpenAI API and Google Speech expenses.


The assistant **doesn't check** for any kind of **security**. The main prompt instructs the LLM to refuse running any command that can harm the computer,
but as you probably know there are many ways to harm a computer that the LLM may not be able to detect (or just ignore). 

Be careful with the commands you add.

It is also advisable to run the assistant in a virtual machine, at least until you're confident with the code and commands running.


# Requirements

Some of the dependencies make the tool runnable only on Windows. It also uses a Microphone, and some paid services (with trial available).


# Dependencies

The design of the program is modular, so it should be easy to replace any of these technologies (bar dotnet) with any other speech recognizer, language model or audio library.


* .Net Core 7.0 : https://dotnet.microsoft.com/download/dotnet/7.0
* NAudio : https://github.com/naudio/NAudio Used for audio input and output. 
* Vosk : https://alphacephei.com/vosk/ is used for offline speech recognition. It's a good speech recognizer, but it's not perfect. It's used as a first step to reduce bandwidth and costs, as well as improve privacy.
* Google Speech : https://cloud.google.com/speech-to-text is used for online speech recognition. Requires a key. The monthly free tier may be enough for most uses.
* OpenAI GPT : https://openai.com/blog/openai-api/ is used to understand the order. Requires a key. There's a free trial, but requires a paid account after some time or usage.
  * Personally I'm locked to GPT-3.5 and works quite well, but the few tests I've made through ChatGPT-4 work incredibly better.


# Building and running

* Install .NET Core 7.0	https://dotnet.microsoft.com/en-us/download
* Clone the repository `git clone https://github.com/jordi-z80/UncleJupiter`
* Run `dotnet build` in the root folder
* Run `dotnet run` in the root folder

## Installing extra files

I've added **installDependencies.bat** to install the English vosk version, FSCmd and a few cool sounds.

Vosk is mandatory, the other ones are optional.

The batch file requires git, 7zip and curl. The easiest way to install them is via chocolatey.

* Install chocolatey : https://chocolatey.org/install
* Run `choco install git 7zip curl` in a command prompt with admin privileges.

Finally,
* run installDependencies.bat


You can also install the dependencies manually, check the appSettings files to see where the files are expected to be.
# Settings

The Settings folder contains the following files:
* **appSettings.json** : General settings for the program.
* **macros.json** : Values for macro expansion for actions defined by the commands. Basically, all the programs and URLs you want to use.
* **secret.json** : Place for your secret keys. You have to create it yourself renaming **secret.json.example** and filling it.

# Settings per language

For each language, there's a folder that contains the language specific configuration:
* **appSettings-LANGUAGE.json** : Contains the configuration for the language. Each speech recognizer has its own configuration, as well as a list of 'names' for the computer.
* **prompt.txt** : Contains the prompt to be sent to the language model. The results are quite good with the current one, but feel free to experiment.
* **quickCommandSettings.json**: Configuration to improve the accuracy of the RegEx based commands (quick-commands).

# Commands

Every command is defined in a json file in the Commands folder. Commands must be configured per language.

Every json file in the Commands folder is considered a list of commands and loaded at startup. If there's a problem with the JSON file,
the command can either be ignored or the program will stop.


## Quick-commands

Every command has the chance to be run as a quick-command. The idea is that simple orders do not need to be sent to the LLM, and can be run directly.
In order to achieve this, Regular Expressions are used. Obviously they're very limited, but for some simple commands they work quite well. (e.g. "play next song")

## Command structure 

The command format is quite straightforward to understand, it is recommended to just check the commands in the project :

The file must contain a JSON array of objects, where each object is a command. The command has the following fields:

* **name** : The name of the command. Must be unique (used as index in the embeddings dictionary).
* **flags** : An array of flags. 
  * **requiresDate** : The command requires the current date to work correctly (it is injected into the LLM prompt).
  * **disableQuickCommand** : The command cannot be run as a quick-command, even if it matches and has the data to do so.
* **llmText** : Text sent to the LLM if the embedding is selected when compared against the user input.
* **isProcessRunning** : In order for this command to be run, the specified process must be running. (e.g. if you want to run a command only if VLC is running, you can specify it here)

If the command can run as a quick-command, it must have the following fields:
* **regExp** : An array of regular expressions to match with quick run. If the command is run as a quick-command, the first regexp that matches is used. The regexp can have a named group called 'param' that can be used later in the action or the param.
* **action** : The action to be run if the command is run as a quick-command.
* **param** : The parameter to be used with the action.

If the command can run as a LLM command, it can have the following optional fields:
* **invalidWords** : Array of words that, if present, should discard the command. (e.g. if you want to search a video, "search" will trigger %WEBSEARCH%. However you really will want to use %VIDEO%)
* **allowedWords** : Some words are globally in the list of invalid words to avoid problems with multiple commands considered as simple ones. This array of words allows you to use the listed words, even if they're invalid. 

## llmText
**llmText** is by far the most important field. It must be descriptive to the LLM so that it knows what command to return. It also must be 
expressive because this text is used to create an embedding to compare the user input against. It is recommended to try several texts and see which one works best.



## Command sample (no quick-command)

Note: %FSCMD% is a macro defined in macros.json. You could use NirSoft svlc.exe instead.

```
[
	{
		// the name of the action. Must be unique.
		"name":"volumeControl",

		// descriptive text for the LLM
		"llmText":"If the user wants to set an absolute value for the volume, use action='%FSCMD%' and param='volume --set=<volume from 0 to 100>'."
	},
]
```


## Command sample (with quick-command)

```
[
	
	{
		// the name of the action. Must be unique.
		"name":"alarm",
		
		// Note: this action requires the current date to work correctly.
		"flags": ["requiresDate"],
		
		// The regexps to match with quick run
		"regExp": 
		[
			"Set (an )?alarm (in |to |for )?(?<timeDeltaParam>.*)( from now)?"
		],
		
		// action and param translation of the regEx if it matches.
		"action": "%FSCMD%",
		"param": "setAlarm --deltaTime=%timeDeltaParam%",
		
	
		// descriptive text for the LLM
		"llmText":"If the user wants to set an alarm for a certain date, use action = '%FSCMD%' and param = 'setAlarm --date=\"[YYYY/]MM/dd HH:mm\" --text=\"<reason for the alarm>\"' Otherwise, if he wants to set the alarm with a relative time, use action = '%FSCMD%' and param = 'setAlarm --deltaTime=\"HH:mm:ss\" --text=\"<reason for the alarm>\"'"
		

		
	},

]


```



# How to add new commands

Just create a new JSON file in the Commands folder, and add the commands to it. The program will load it at startup.


# Tips 

You can specify the language, as well as most parameters in the settings files, as a commandline parameter, in example: dotnet run --Language:Code=en-US


# Google Speech to Text API help.

In order to use Google Speech to Text API you need a credentials JSON file. You can follow this easy 264 steps guide to get it: https://www.youtube.com/watch?v=izdDHVLc_Z0&t=217s

Once you have the JSON file, place it anywhere in your HDD and adjust the secrets.json file accordingly.

# Adding more languages

In order to add another language just clone the folder of an existing one and change the configuration and command files. 
The name of the folder doesn't need to be the same as the language code.

There's one important point though: The **llmText** of every command is injected into the prompt.txt if needed. I've found that the LLM works way better
in my maiden language if I have the prompt in English and I ask it to translate the user input to English. However, this may not
be the case for other languages.

The point is that if your prompt.txt is in English, then your **llmText** fields must be in English too (this makes it easier to create a new language).

If you want to translate the prompt.txt to your maiden language, then you must translate the **llmText** fields too of each command you want to use.

# FSCmd
I didn't want this project to have unneeded dependencies, so I created a external helper tool to reduce them, named FSCmd.
It's located here : https://github.com/jordi-z80/FSCmd . 

As it is now, it is used to control the audio volume, setting alarms, controlling VLC and hibernating the computer.

It is completely optional, and can be replaced with any other tool that can be called from the command line.

# Sample orders

Every order must be preceded by the wake word. The default is "computer".

Once you hear the "beep", you can issue your order.

* I want to see Planet Jupiter.
* Search today's news about AI.
* I want to watch a StarTalk video podcast.
* Play a song from Genesis that talks about houses.

Note: Saying to "play a song" doesn't really play a song right now. It maps the order into a youtube search.
I haven't found a way to directly play a song in youtube, so if you have one, you can easily change the Commands/builtIn/watch.json file to play it directly.

* When was the Sinclair Spectrum released?
Note: Questions are converted into searches (change prompt.txt to change this behavior)

Some commands (search, listen, watch) use "specify if possible" in their embedding. This means that if the LLM is intelligent enough
(GPT-3.5 seems to do very rarely, but I suspect GPT-4 is much better at this job, based from my tests in ChatGPT-4)

In example
* I want to watch an interview to the actor who played the main character of Star Trek: The Next Generation.

sometimes is interpreted and returned as 
* "watch Patrick Stewart interview"

## Multiple orders

You can issue multiple orders in the same sentence. The LLM will try to find the best match for each order although
the results are worse than issuing one order at a time, specially if orders are short (the embedding search seems to work better on longer sentences).

* Search image of puppies and play a Tears for Fears song.
* Run Excel and Word and the calculator.

# Potential improvements

Other than improving the command actions (like automatizing the audio and video search autoplay), and some implementation details (the run function is so lame) 
the following improvements could be made:

* Open a port to allow remote voice input. (safety measures must be taken)
* Using keywords other than "computer" to route the order to a specific program. (e.g. "VLC: next" instead of "Computer: next video"). 
* Voice control of standard windows menus navigating the application's menu (e.g. Visual Studio: (menu) Git, (menu)Commit) or through keybindings.
* Dictation.
* Using Semantic Kernel to allow easy swap for other LLMs.
* No need to wait between the computer keyword and the order.

# Potential uses

* Voice assistant for languages not supported by the big players.
* Domotics
* Controlling the computer with your voice from a distance (through a mobile companion).
* Adding voice commands to games (e.g. creating a mod to RimWorld and respond to things like "select granite wall")

