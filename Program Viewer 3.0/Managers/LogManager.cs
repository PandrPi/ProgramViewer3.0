using System;
using System.IO;

namespace ProgramViewer3.Managers
{
	public static class LogManager
	{
		private static readonly string logFileName = Path.Combine(ItemManager.ApplicationPath, "logout.txt");

		private static TextWriter defaultWriter;
		private static StreamWriter newWriter;

		/// <summary>
		/// Initializes the LogManager
		/// </summary>
		/// <param name="redirectLogging">Determines whether to redirect the Console output
		/// to our own log file</param>
		public static void Initiallize(bool redirectLogging)
		{
			defaultWriter = Console.Out;
			if (redirectLogging)
			{
				try
				{
					File.WriteAllText(logFileName, string.Empty);
					newWriter = new StreamWriter(logFileName, false);
					Console.SetOut(newWriter);
				}
				catch (Exception e)
				{
					Console.WriteLine($"Cannot open '{logFileName}' for writing: {e.Message}");
					Close();
				}
			}
		}

		/// <summary>
		/// Sets Console.Out property to the default Console writer stream 
		/// </summary>
		public static void Close()
		{
			Console.SetOut(defaultWriter);
			newWriter?.Close();
		}

		/// <summary>
		/// Writes the specified message object to the Console output stream
		/// </summary>
		/// <param name="message"></param>
		public static void Write(string message)
		{
			const string writeFormat = "{0} : {1}";
			string date = DateTime.Now.ToString();
			Console.WriteLine(writeFormat, date, message);
		}

		public static void Error(Exception exception)
		{
			const string errorFormat = "Message: {0}.\nStack trace: {1}";
			Write(string.Format(errorFormat, exception.Message, exception.StackTrace));
		}
	}
}
