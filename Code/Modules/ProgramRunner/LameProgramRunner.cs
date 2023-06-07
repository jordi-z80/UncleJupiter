using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace UncleJupiter;

//=============================================================================
/// <summary>This class is a lame provisional implementation. It runs the Windows Run Box and pastes the name
/// of the program there. Let Windows do its job.
/// 
/// I'm sure there's a better way to do this, I just don't know it. :)
/// </summary>
public class LameProgramRunner : IProgramRunner
{
    #region DLL access
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);
    private const int KEYEVENTF_EXTENDEDKEY = 1;
    private const int KEYEVENTF_KEYUP = 2;

    public static void KeyDown(Keys vKey)
    {
        keybd_event((byte)vKey, 0, KEYEVENTF_EXTENDEDKEY, 0);
    }

    public static void KeyUp(Keys vKey)
    {
        keybd_event((byte)vKey, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
    }
	#endregion

	bool beingUsed = false;

    //=============================================================================
    /// <summary></summary>
    void wait (int ms)
    {
        Task.Delay(ms).Wait();
    }

    //=============================================================================
    /// <summary></summary>
    public void runProgramByShortName(string programName)
    {
		// If we try to run 2 programs at the same time (e.g. run notepad and then run chrome), both will fail
		// because we'll be outputting keys at the same time. Delay the second one until the first one is done.
		if (!beingUsed)
		{
			// We have to run this on the main thread.
			var main = Program.ServiceProvider.GetRequiredService<Main> ();
			beingUsed = true;
			main.addMainThreadAction (() => _runProgramByShortName (programName));
		}
		else
		{
			// retry after a while, until status is no longer true
			Task.Delay (50).Wait ();
			runProgramByShortName (programName);
		}
	}

    //=============================================================================
    /// <summary>It presses the Windows key, pastes the text and presses return. Lets windows find the program.</summary>
    /// <remarks>This must be run on the main thread.</remarks>
    void _runProgramByShortName(string programName)
    {
        // we use the clipboard for fast copy/paste onto the run box
        Clipboard.SetText(programName);

        KeyDown(Keys.LWin);
        KeyUp(Keys.LWin);

        // wait for the run box to appear
        wait(50);

        KeyDown(Keys.LControlKey);
        KeyDown(Keys.V);
        KeyUp(Keys.V);
        KeyUp(Keys.LControlKey);

        // wait for the program name to be pasted
        wait(20);

        KeyDown(Keys.Enter);
        KeyUp(Keys.Enter);

		beingUsed = false;
    }
}
