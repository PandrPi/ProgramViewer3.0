using System;
using System.IO;

namespace ProgramViewer3.Managers
{
	public static class LogManager
	{
		private static readonly string logFileName = Path.Combine(ItemManager.ApplicationPath, "logout.txt");
			    
		private static TextWriter defaultWriter;
		private static StreamWriter newWriter;

		public static void Initiallize(bool redirectLogging)
		{
			defaultWriter = Console.Out;
			if (redirectLogging)
			{
				try
				{
					if (!File.Exists(logFileName)) File.Create(logFileName).Close();
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

		public static void Close()
		{
			Console.SetOut(defaultWriter);
			newWriter?.Close();
		}

		public static void Write(string message)
		{
			string date = DateTime.Now.ToString();
			Console.WriteLine($"{date} : {message}");
		}
	}
}
