using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LSNoirCaseEditorWPF.Logger
{
    internal class Logger
    {
        internal static List<Log> Logs = new List<Log>();
        internal static string _file;

        internal static void Initialize()
        {
            _file = @$"Logs\{DateTime.Now.ToString("MM-dd-yyyy hh-mm-ss")}.txt";
            if (!Directory.Exists(@"Logs")) Directory.CreateDirectory(@"Logs");
            var stream = File.Create(_file);
            stream.Close();

            Thread t = new Thread(LogWriteLoop);
            t.Start();
        }

        public static void AddLog(string log, bool isDebug)
        {
            string data = DateTime.Now.ToShortDateString() + " | " + DateTime.Now.ToLongTimeString() + " : " + log;
            Logs.Add(new Log(data, isDebug));
            OnLogAdded(log);
        }

        private static int _lastIndex = 0;

        internal static void LogWriteLoop()
        {
            while (true)
            {
                using (StreamWriter sw = File.AppendText(_file))
                {
                    if (Logs.Count > _lastIndex)
                    {
                        for (int i = _lastIndex + 1; i < Logs.Count; i++)
                        {
                            sw.WriteLine(Logs[i].LogData);
                            _lastIndex = i;
                        }
                    }

                }
                Thread.Sleep(0050);
            }
        }

        public static event LogAdd OnLogAdded;

        public delegate void LogAdd(string text);
    }

    internal class Log
    {
        internal string LogData { get; }
        internal bool IsDebug { get; }

        internal Log(string log, bool isDebug)
        {
            LogData = log;
            IsDebug = isDebug;
        }
    }
}
