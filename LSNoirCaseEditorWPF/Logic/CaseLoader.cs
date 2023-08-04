using CaseManager.NewData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Shapes;
using System.Windows.Threading;
using CaseManager.Resources;
using LSNoirCaseEditorWPF.Windows;
using Xceed.Wpf.AvalonDock.Controls;
using static System.Net.Mime.MediaTypeNames;

namespace LSNoirCaseEditorWPF.Logic
{

    internal class StageLoader
    {
        internal StageLoader()
        {
        }

        internal List<TreeViewItem> NodeList = new List<TreeViewItem>();

        internal async Task<TreeViewItem> LoadCase(Case _case)
        {
            TreeViewItem returnedNode = null;
            try
            {
                var start = DateTime.Now;
                
                returnedNode = CreateRootNode("Case");
                
                Logger.Logger.AddLog($"Total stages: {_case.Stages.Count}", false);
                foreach (var stage in _case.Stages)
                {
                    Logger.Logger.AddLog($"Starting to load stage: {stage.ID}", false);
                    var stageNode = await GetTreeviewItem_Stage(stage);
                    if (stageNode == null) continue;
                    returnedNode.Items.Add(stageNode);
                }
                
                NodeList.Add(returnedNode);
                
                Logger.Logger.AddLog($"Time to load case: {(DateTime.Now - start).TotalMilliseconds}ms", false);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }

            return returnedNode;
        }
        
        private async Task<TreeViewItem> GetTreeviewItem_Stage(Stage stage)
        {
            TreeViewItem returnedNode = null;
            try
            {
                var start = DateTime.Now;
                Logger.Logger.AddLog($"GetTreeviewItem_Stage {stage.ID}", true);
                returnedNode = CreateRootNode($"Stage - {stage.ID}");

                var rootProperties = typeof(Stage).GetProperties();
                var taskList = new List<Task<TreeViewItem>>();
                foreach (var property in rootProperties)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        var task = new PropertyLoader().LoadPropertyInfo(property, stage);
                        taskList.Add(task);
                    });
                }
                
                var completed = Task.WaitAll(taskList.Cast<Task>().ToArray(), new TimeSpan(0,0,1,0));

                foreach (var result in taskList.Select(t => t.Result).Where(result => result != null))
                {
                    returnedNode.Items.Add(result);
                }
                
                Logger.Logger.AddLog($"Time to load stage: {(DateTime.Now - start).TotalMilliseconds}ms - success={completed} ", false);
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }

            return returnedNode;
        }

        private TreeViewItem CreateRootNode(string text)
        {
                TreeViewItem node = null;
                node = new TreeViewItem
                {
                    Header = text,
                    IsExpanded = true
                };
                return node;
        }

        private void ShowError(Exception exception, bool displayMessage = true)
        {
            if (displayMessage) MessageBox.Show(exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Logger.Logger.AddLog($"!! ERROR !! {exception.Message}", false);
            Logger.Logger.AddLog($"!! ERROR !! {exception.StackTrace}", false);
        }
    }
}
