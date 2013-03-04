using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Program.Properties;
using System.Globalization;
using System.IO;

namespace Program
{
	class Program
	{
        public static string logName = "LOG.log";
		public static void Log(string format, params object[] args)
		{
            string logMessage = String.Format("[{0}] {1}\n", DateTime.Now.ToString (Settings.Default.DateTimeFormat, CultureInfo.InvariantCulture), string.Format (CultureInfo.InvariantCulture, format, args));
			Console.Write(logMessage);

            StreamWriter Log;
            if (!File.Exists(logName))
                Log = new StreamWriter(logName);
            else
                Log = File.AppendText(logName);

            Log.Write(logMessage);
            Log.Close();
		}

		static void Main(string[] args)
		{
            if (File.Exists(logName)) File.Delete(logName);

			try
			{
                App app = new App();
                app.Run(args);
			}
			catch (Exception ex)
			{
				Console.ForegroundColor = ConsoleColor.Yellow;
				Program.Log ("{0}", ex);
				Console.ResetColor ();
			}

            Console.Read();
		}
	}
}
