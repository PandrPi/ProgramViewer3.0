using System;
using System.IO;

namespace ProgramViewer3
{
	public static class LogManager
	{
		private static string logFileName = "logout.txt";
			    
		private static TextWriter defaultWriter;
		private static FileStream fileStream;
		private static StreamWriter newWriter;

		public static void Initiallize(bool redirectLogging)
		{
			defaultWriter = Console.Out;
			if (redirectLogging)
			{
				try
				{
					fileStream = new FileStream(logFileName, FileMode.OpenOrCreate | FileMode.Truncate, FileAccess.Write);
					newWriter = new StreamWriter(fileStream);
					
				}
				catch (Exception e)
				{
					Console.WriteLine("Cannot open Redirect.txt for writing");
					Console.WriteLine(e.Message);
					return;
				}
				Console.SetOut(newWriter);
			}
		}

		public static void Close()
		{
			Console.SetOut(defaultWriter);
			newWriter?.Close();
			fileStream?.Close();
			newWriter?.Dispose();
			fileStream?.Dispose();
		}

		public static void Write(string message)
		{
			string date = DateTime.Now.ToString();
			Console.WriteLine($"{date} : {message}");
		}
	}
}
