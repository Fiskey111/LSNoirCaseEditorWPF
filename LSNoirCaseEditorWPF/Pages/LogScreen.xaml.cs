using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace LSNoirCaseEditorWPF.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LogScreen : Page
    {
        public LogScreen()
        {
            this.InitializeComponent();
            
            debugCheck.IsChecked = false;
            #if DEBUG
            debugCheck.IsChecked = true;
            #endif

            UpdateLogs("");

            Logger.Logger.OnLogAdded += Logger_OnLogAdded;

            logLink.Content = Logger.Logger._file;
        }

        private void Logger_OnLogAdded(string text)
        {
            UpdateLogs(text);
        }

        public void UpdateLogs(string text)
        {
            if (debugCheck.IsChecked == true)
                LogBox.Text = LogBox.Text + Environment.NewLine + text;
                else
                {
                    LogBox.Text = Logger.Logger.Logs.ToList().Where(
                            line => debugCheck.IsChecked != false || !line.IsDebug)
                        .Aggregate(string.Empty, (current, line)
                            => current + line.LogData + Environment.NewLine);
                }
                ScrollView.ScrollToEnd();
        }

        private void DebugCheck_Click(object sender, RoutedEventArgs e)
        {
            if (sender != debugCheck) return;

            Logger.Logger.AddLog($"DebugCheck_Click {debugCheck.IsChecked}", true);

            UpdateLogs(Logger.Logger.Logs.ToList().Where(
                    line => debugCheck.IsChecked != false || !line.IsDebug)
                .Aggregate(string.Empty, (current, line)
                    => current + line.LogData + Environment.NewLine));
        }

        private void logLink_Click(object sender, RoutedEventArgs e)
        {
            DefaultLaunch();
        }
        
        private void DefaultLaunch()
        {
            //Process.Start($"notepad {Path.Combine(Environment.CurrentDirectory, Logger.Logger._file)}");
        }
    }
}
