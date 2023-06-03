using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncleJupiter;

internal static class ConsoleEx
{
	public static void Warning (string message)
	{
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine (message);
		Console.ResetColor ();
	}

	public static void Error (string message)
	{
		Console.ForegroundColor = ConsoleColor.Red;
		Console.WriteLine (message);
		Console.ResetColor ();
	}

}
