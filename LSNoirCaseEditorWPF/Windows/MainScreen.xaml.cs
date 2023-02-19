using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using CaseManager.NewData;
using LSNoirCaseEditorWPF.Logic;
using LSNoirCaseEditorWPF.Pages;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using ModernWpf.Controls;
using ModernWpf.Controls.Primitives;
using Newtonsoft.Json;
using static LSNoirCaseEditorWPF.Logger.Logger;

namespace LSNoirCaseEditorWPF.Windows
{
    public sealed partial class MainScreen : Window
    {
        public static CaseHandler CaseHandler = new CaseHandler();
        private bool _isNewCase = false;
        private string _caseDataFile;

        public MainScreen()
        {
            this.InitializeComponent();
            Logger.Logger.Initialize();
        }
        
        private void TabView_Loaded(object sender, RoutedEventArgs e)
        {
            (sender as TabControl).Items.Add(CreateTab("Case", typeof(CaseViewer), "page-text.png"));
            (sender as TabControl).Items.Add(CreateTab("Flowchart", typeof(Flowchart), "graph.png"));
            (sender as TabControl).Items.Add(CreateTab("Logger", typeof(LogScreen), "window-text.png"));
        }

        private async void OpenWebView()
        {
            ContentDialog confirmNavigate = new ContentDialog
            {
                Title = "Confirm Navigation",
                Content = $"Continue navigating to https://github.com/Fiskey111/LSNCaseManager",
                CloseButtonText = "No",
                PrimaryButtonText = "Yes",
                DefaultButton = ContentDialogButton.Primary
            };

            ContentDialogResult result = await confirmNavigate.ShowAsync();

            AddLog($"OpenWebView.confirmNavigate | result={result}", true);

            if (result != ContentDialogResult.Primary)
            {
                // Cancelled
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/Fiskey111/LSNCaseManager",
                UseShellExecute = true
            });
        }

        private async void OpenCase()
        {
            AddLog($"OpenCase()", true);

            bool cont = await CheckSave();
            if (!cont)
            {
                AddLog($"CheckSave false", true);
            }

            _isNewCase = false;

            var result = FilePicker();
            if (string.IsNullOrEmpty(result))
            {
                AddLog($"File not found", true);
            }
            _caseDataFile = result;

            CaseHandler.Load(_caseDataFile);
        }

        private async void NewCase()
        {
            AddLog($"NewCase()", true);

            _isNewCase = true;

            var result = FolderPicker();
            if (string.IsNullOrEmpty(result)) return;

            var newFileCreator = new NewFileCreator();
            newFileCreator.ShowDialog();

            AddLog($"{result} + {newFileCreator.FileName}", true);

            var path = Path.Combine(result, newFileCreator.FileName + ".caseData");
            if (string.IsNullOrEmpty(path)) return;

            _caseDataFile = path;

            var tempCase = new Case();
            tempCase.InitializeNew();

            File.WriteAllText(_caseDataFile, JsonConvert.SerializeObject(tempCase, Formatting.Indented));

            CaseHandler.Load(_caseDataFile);
        }

        private string FilePicker()
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Case Files|*.casedata";
            if (openFileDialog.ShowDialog() != true)
            {
                AddLog("No file selected", true);
                return string.Empty;
            }

            return openFileDialog.FileName;
        }

        private string FolderPicker()
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.Multiselect = false;
            var result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok) return dialog.FileName;
            else return string.Empty;
        }

        private async Task<bool> CheckSave()
        {
            if (CaseHandler.HasBeenChangedSinceUpdate())
            {
                ContentDialog confirm = new ContentDialog
                {
                    Title = "Continue Without Saving?",
                    Content = $"You have not saved your changes.  Are you sure you would like to continue without saving them?",
                    CloseButtonText = "Cancel",
                    PrimaryButtonText = "Continue",
                    SecondaryButtonText = "Save Changes and Continue",
                    DefaultButton = ContentDialogButton.Close
                };

                ContentDialogResult result = await confirm.ShowAsync();

                AddLog($"CheckSave.confirm | result={result}", true);

                switch (result)
                {
                    case ContentDialogResult.Primary:
                        return true;
                    case ContentDialogResult.Secondary:
                        SaveCase();
                        return true;
                }
            }
            return false;
        }
        
        private Flyout CreateFlyout(UIElement elementToAttach, string text)
        {
            Flyout flyout = new Flyout();
            flyout.Content = new TextBlock()
            {
                Text = text
            };
            flyout.AreOpenCloseAnimationsEnabled = true;
            flyout.ShowAt(elementToAttach as FrameworkElement);
            return flyout;
        }
        
        private async Task SaveCase()
        {
            AddLog($"SaveCase()", false);
            if (CaseHandler.CurrentCase == null)
            {
                AddLog($"SaveCase.Handler == null", true);
                var nullHandler = new ContentDialog
                {
                    Title = "No Case Found",
                    Content = "Please load/create a case before trying to save",
                    CloseButtonText = "Ok"
                };

                await nullHandler.ShowAsync();
                return;
            }

            CaseHandler.Save();
        }


        private TabItem CreateTab(string name, Type page, string imageName)
        {
            var newItem = new TabItem
            {
                Header = name
            };

            var image = new Image();
            BitmapImage bitmapImage = new BitmapImage(new Uri($"/Images/{imageName}", UriKind.Relative));
            image.Source = bitmapImage;
            TabItemHelper.SetIcon(newItem, image);

            var frame = new ModernWpf.Controls.Frame();
            frame.Navigated += (s, e) =>
            {
                ((FrameworkElement)frame.Content).Margin = new Thickness(5, 5, 5, 5);
            };
            frame.Navigate(page);
            newItem.Content = frame;
            return newItem;
        }

        private void Menu_Clicked(object sender, System.Windows.RoutedEventArgs e)
        {
            var item = sender as MenuItem;
            if (item == null) return;
            if (item == navViewSource)
            {

            }
            else if (item == navNewCase)
            {
                NewCase();
            }
            else if (item == navOpenCase)
            {
                OpenCase();
            }
            else if (item == navViewSource)
            {
                OpenWebView();
            }
            else if (item == navInfo)
            {
                // todo -- go to github page
            }
            else if (item == navSaveCase)
            {
                SaveCase();
            }
        }
    }
}
