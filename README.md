

# Uncle, I want to see the planet Jupiter.

My four-year-old niece loves to view planets on the computer. I thought it would be fun to create a computer assistant to see her reaction when the computer did as she commanded. Ultimately, I added a few additional features to help me with my daily tasks and to easily add new commands.

I am aware there are numerous PC assistants available, but most of them do not function efficiently, if at all, in our native language.
I could have used one of the existing ones to bring my language to the assistant, but we all know that's not how programmers think. \:\)

Nonetheless, it's quite enjoyable to experiment with and is highly adaptable.


# A GPT-powered computer assistant.

This is a very basic computer assistant. At a glace, it works as follows:

* It listens via your microphone until a keyword ("computer") is detected.
* A sound cue is played. THEN you can issue your command.
* The speech is recognized (single utterance).
* It uses RegEx to try to comprehend the command. If the command is concise and understandable, it attempts to execute the command directly.
* If not, a prompt is created and sent to OpenAI-GPT.
* With the GPT's response, it tries to execute the command again.

Currently, it can be configured to use any language supported by Vosk and Google Speech, as long as you set it up correctly and rewrite the commands in the new language.

# Warning 

This program, as is, utilizes **paid services**. It's crucial to closely monitor your OpenAI API and Google Speech expenditures.

The assistant **doesn't check** for any kind of **security**. The main prompt instructs the LLM to reject any command that could harm the computer, but as you probably know, there are numerous ways to damage a computer that the LLM might not detect or might simply ignore.

Exercise caution with the commands you add.

It's also recommended to run the assistant in a virtual machine, at least until you're comfortable with the code and the commands being executed.

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
* Enter the folder `cd UncleJupiter`
* Run `dotnet build`
* Run `installDependencies.bat` (optional, but strongly recommended, read below.)
* Run `dotnet run`

## Installing extra files

I've added **installDependencies.bat** to install the English Vosk version, FSCmd and a few cool sounds.

Vosk is mandatory, the other ones are optional.

The batch file requires git, 7zip and curl. The easiest way to install them is via chocolatey.

* Install chocolatey : https://chocolatey.org/install
* Run `choco install git 7zip curl` in a command prompt with admin privileges.

Finally,
* run installDependencies.bat

You can also install the dependencies manually, check the appSettings files to see where the files are expected to be.

### What's installed
* Vosk, an offline speech recognizer. ( https://alphacephei.com/vosk/models )
* FSCmd, a small command-line tool created to help UncleJupiter ( https://github.com/jordi-z80/FSCmd )
* 3 ST:TNG sounds, way cooler than my speccy beeps. ( https://www.trekcore.com/audio/ )

# Settings

The Settings folder contains the following files:

* **appSettings.json**: General settings for the program.
* **macros.json**: Values for macro expansion for actions defined by the commands. Basically, these are all the programs and URLs you want to use.
* **secret.json**: A place for your secret keys. You need to create it yourself by renaming **secret.json.example** and filling it out.

Note: Edit appSettings.json to change the default OpenAI model (gpt-3.5-turbo) to GPT-4.

# Settings per Language

For each language, there's a folder that contains the language-specific configuration:

* **appSettings-LANGUAGE.json**: Contains the configuration for the language. Each speech recognizer has its own configuration, as well as a list of 'names' for the computer.
* **prompt.txt**: Contains the prompt to be sent to the language model. The results are quite good with the current one, but feel free to experiment.
* **quickCommandSettings.json**: Configuration to improve the accuracy of the RegEx based commands (quick-commands).

# Commands

Every command is defined in a JSON file in the Commands folder. Commands must be configured per language.

Every JSON file in the Commands folder is considered a list of commands and loaded at startup. If there's a problem with the JSON file, the command can either be ignored or the program will stop.

## Quick-Commands

Commands can be executed as a quick-command. The concept here is that simple orders do not need to be sent to the LLM and can be run directly. In order to achieve this, Regular Expressions are utilized. While they are somewhat limited, they work quite well for simple commands, e.g., "play next song."

## Command Structure

The command format is quite straightforward. It's easier to understand it by just editing the existing ones.

The file must contain a JSON array of objects, where each object is a command. The command includes the following fields:

* **name**: The name of the command. It must be unique (used as an index in the embeddings dictionary).
* **flags**: An array of flags. 
  * **requiresDate**: The command requires the current date to work correctly (it is injected into the LLM prompt).
  * **disableQuickCommand**: The command cannot be executed as a quick-command, even if it matches and has the data to do so.
  * **injectAlways**: The command **llmText** is always injected into the LLM prompt. Use it sparingly.
* **llmText**: Text sent to the LLM if the embedding is selected when compared against the user input.
* **isProcessRunning**: For this command to be executed, the specified process must be running (e.g., if you want to execute a command only if VLC is running, you can specify it here).

If the command can be run as a quick-command, it must include ALL the following fields:

* **regExp**: An array of regular expressions to match for a quick run. If the command is executed as a quick-command, the first regex that matches is used. The regex can have a named group called 'param' that can be used later in the action or the param.
* **action**: The action to be executed if the command is run as a quick-command.
* **param**: The parameter to be used with the action.

If the command can be run as an quick-command, it can include the following optional fields:

* **invalidWords**: Array of words that, if present, should discard the command (e.g., if you want to search a video, "search" will trigger %WEBSEARCH%. However, you would really want to use %VIDEO%).
* **allowedWords**: Some words are globally in the list of invalid words to avoid problems with multiple commands considered as simple ones. This array of words allows you to use the listed words, even if they're invalid. 

## llmText

**llmText** is by far the most critical field. It must be descriptive to the LLM so that it knows what command to return. It also must be expressive because this text is used to create an embedding to compare against the user input. It is recommended to try several texts and see which one works best. (longer descriptive texts work better)

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

You can specify the language, as well as most parameters in the settings files, as a commandline parameter, in example: ``dotnet run --Language:Code=en-US`` 


# Google Speech to Text API help.

In order to use Google Speech to Text API you need a credentials JSON file. You can follow this easy 264 steps guide to get it: https://www.youtube.com/watch?v=izdDHVLc_Z0&t=217s

Once you have the JSON file, place it anywhere in your HDD and adjust the secrets.json file accordingly.
# Adding More Languages

To add another language, simply clone the folder of an existing one and modify the configuration and command files. The name of the folder doesn't need to match the language code.

There's one important point to note, though: The **llmText** of every command is injected into the prompt.txt if needed. I've found that the LLM functions much better in my native language if I have the prompt in English, and I ask it to translate the user input to English. However, this may not be the case for other languages.

The key point is that if your **prompt.txt** is in English, then your **llmText** fields must also be in English (this makes it easier to create a new language).

If you want to translate the **prompt.txt** into your language of choice, then you must translate the **llmText** fields of each command you want to use as well.

# FSCmd

I wanted to avoid unnecessary dependencies in this project, so I created an external helper tool named FSCmd to reduce them. It's located here: https://github.com/jordi-z80/FSCmd.

As it currently stands, it is used to control audio volume, set alarms, control VLC, and hibernate the computer.

It is completely optional and can be replaced with any other tool that can be called from the command line.

# Sample Orders

Every order must be preceded by the wake word, with the default being "computer."

Once you hear the "beep," you can issue your order.

* **I want to see Planet Jupiter.**
* **Search today's news about AI.**
* **I want to watch a StarTalk video podcast.**
* **Play a song from Genesis that talks about houses.**

Note: Saying "play a song" doesn't actually play a song right now. It maps the order into a YouTube search. I haven't found a way to directly play a song on YouTube. So, if you have an alternative url, you can easily modify the Commands/builtIn/watch.json file to play it directly (or just replace it with Spotify or similar).

* **When was the Sinclair Spectrum released?**

Note: Questions are converted into searches (change prompt.txt to modify this behavior).

Some commands (search, listen, watch) use "specify if possible" in their embedding. This implies that if the LLM is intelligent enough (GPT-3.5 seems to do it very rarely, but based on my tests, I suspect GPT-4 is much better at this), it might work as expected.

For example, 
* **I want to watch an interview with the actor who played the main character in Star Trek: The Next Generation.**

sometimes is interpreted by the LLM and returned as 
* **watch Patrick Stewart interview**

## Multiple Orders

You can issue multiple orders in the same sentence. The LLM will attempt to find the best match for each order, though the results are less accurate than issuing one order at a time, especially if orders are short (the embedding search seems to work better with longer sentences).

* **Search for images of puppies and play a Tears for Fears song.**
* **Launch Excel, Word, and the calculator.**

# Potential Improvements

Other than improving command actions (like automating the audio and video search autoplay) and some implementation details (the run function could be better), the following improvements could be made:

* Open a port to allow remote voice input (safety measures must be taken).
* Use keywords other than "computer" to route the order to a specific program (e.g., "VLC: next" instead of "Computer: next video").
* Voice control of standard Windows menus, navigating the application's menu (e.g., "Visual Studio: (menu) Git, (menu)Commit") or through keybindings.
* Dictation.
* Use Semantic Kernel to allow an easy swap for other LLMs.
* Remove the need to wait between the computer keyword and the order.

# Potential Uses

* Voice assistant for languages not supported by major players.
* Domotics.
* Controlling the computer with your voice from a distance (through a mobile companion).
* Adding voice commands to games (e.g., creating a mod for RimWorld that responds to commands like "select granite wall").
