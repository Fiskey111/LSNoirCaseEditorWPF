using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using CaseManager.NewData;
using CaseManager.Resources;
using LSNoirCaseEditorWPF.Logic;
using LSNoirCaseEditorWPF.Logic.Editor;
using LSNoirCaseEditorWPF.Objects;
using LSNoirCaseEditorWPF.Windows;
using Microsoft.Win32;
using ModernWpf.Controls;
using Newtonsoft.Json;
using Xceed.Wpf.Toolkit;
using static LSNoirCaseEditorWPF.Logger.Logger;
using Color = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;
using Page = System.Windows.Controls.Page;

namespace LSNoirCaseEditorWPF.Pages
{
    public sealed partial class CaseViewer : Page
    {
        public CaseViewer()
        {
            InitializeComponent();

            CaseContent.SelectedItemChanged += Box_ItemInvoked;
            MainScreen.CaseHandler.OnCaseReloaded += CurrentCase_OnCaseReloaded;

            SetAddComboBox();
        }

        private void SetAddComboBox()
        {
            addItemBox.Items.Clear();

            string[] options = new[] { "Stage", "Document", "Scene Item", "Question", "Additional Information" };

            foreach (var o in options)
            {
                addItemBox.Items.Add(o);
            }
            addItemBox.SelectedIndex = 0;
        }

        private void CurrentCase_OnCaseReloaded(TreeViewItem rootNode)
        {
            //_button?.Remove();
            UpdateCaseData(rootNode);
        }

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            AddLog($"AppBarButton_Click.{sender}", true);

            if (sender == refreshItems)
            {
                MainScreen.CaseHandler.GenerateTreeViewData();
                return;
            }

            if (sender == insertItem)
            {
                AddItem(addItemBox.SelectedItem.ToString());
            }
            else if (sender == deleteItem)
            {
                if (_lastItemSelected == null)
                {
                    CreateFlyout(sender as FrameworkElement, "You must select an item first.");
                    return;
                }

                DeleteItem(_lastItemSelected);
            }
            else if (sender == loadStage)
            {
                var result = FilePicker();
                if (string.IsNullOrEmpty(result))
                {
                    AddLog($"File not found", true);
                    return;
                }

                var stageData = JsonConvert.DeserializeObject<Stage>(File.ReadAllText(result));
                if (stageData == null)
                {
                    AddLog($"Not stage data selected", true);
                    return;
                }
                MainScreen.CaseHandler.CurrentCase.Stages.Add(stageData);
                MainScreen.CaseHandler.GenerateTreeViewData();
            }
        }

        private string FilePicker()
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Stage Files|*.json";
            if (openFileDialog.ShowDialog() != true)
            {
                AddLog("No file selected", true);
                return string.Empty;
            }

            return openFileDialog.FileName;
        }

        // { "Stage", "Document", "Scene Item", "Question", "Additional Information" };
        private async void AddItem(string identifier)
        {
            try
            {
                AddLog($"AddItem: {identifier}", true);
                if (identifier == "Stage")
                {
                    var newStage = new Stage();
                    newStage.InitializeNew();
                    MainScreen.CaseHandler.CurrentCase.Stages.Add(newStage);
                }
                else if (identifier == "Document")
                {
                    var newDocument = new CaseDocuments();
                    newDocument.InitializeNew();
                    MainScreen.CaseHandler.CurrentCase.Documents.Add(newDocument);
                }
                else if (identifier == "Additional Information")
                {
                    var newInfo = new DetailedInformation();
                    newInfo.InitializeNew();
                    MainScreen.CaseHandler.CurrentCase.DetailedInformation.Add(newInfo);
                }
                else if (identifier == "Question")
                {
                    try
                    {
                        // Set search phrase and temporary IDs
                        var searchPhrase = "Scene Items - ";
                        var sceneID = string.Empty;
                        var workingNode = _lastItemSelected;
                        var found = false;

                        // Loop through all parent items from starting node
                        for (int i = 0; i < 100; i++)
                        {
                            // Make sure node exists
                            if (workingNode == null || workingNode.Parent == null || workingNode.Header == null || workingNode is not TreeViewItem) continue;
                            // If node header does not equal search phrase, we haven't found the Scene Item, go up one level
                            if (!workingNode.Header.ToString().Contains(searchPhrase))
                            {
                                workingNode = workingNode.Parent as TreeViewItem;
                                continue;
                            }

                            // Item found, break
                            AddLog($"Item found", false);
                            found = true;
                            break;
                        }

                        // Not found or node is null, exit
                        if (!found || workingNode == null)
                        {
                            AddLog($"No Scene Item found", false);
                            return;
                        }

                        // Get the ID so we can find the scene item
                        var result = workingNode.Header.ToString().Split(searchPhrase)[1];
                        SceneItem item = MainScreen.CaseHandler.CurrentCase.Stages.SelectMany(s => s.SceneItems).First(i => i.ID == result);

                        // Create the interrogation line and add it to the question list
                        var newLine = new InterrogationLine();
                        newLine.InitializeNew();
                        item.InteractionSettings.Interrogation.Questions.Add(newLine);
                    }
                    catch (Exception ex)
                    {
                        AddLog($"{ex}", true);
                    }
                }
                else if (identifier == "Scene Item")
                {
                    if (_lastItemSelected == null)
                    {
                        CreateFlyout(insertItem as FrameworkElement, "You must select an item first.");
                        return;
                    }

                    var searchPhrase = "Stage ID";
                    var stageID = string.Empty;
                    Stage stageReference;

                    // Store a temporary variable to make sure we have found our stage
                    var stageFound = false;

                    // selected item is not a stage, we need to go up to find it (assume that we selected the blip or something)
                    if (!_lastItemSelected.Header.ToString().Contains(searchPhrase))
                    {
                        // Make sure item has children
                        if (_lastItemSelected.Items.Count < 0)
                        {
                            CreateFlyout(insertItem as FrameworkElement, "You must select a stage");
                            return;
                        }


                        // Loop through all child elements
                        for (int i = 0; i < _lastItemSelected.Items.Count; i++)
                        {
                            // Get the child reference and check for null
                            var child = (TreeViewItem) _lastItemSelected.ItemContainerGenerator.ContainerFromIndex(i);
                            if (child == null)
                            {
                                AddLog("Scene Item Add: Child == null", true);
                                continue;
                            }

                            // Check if header contains correct information and is a custom item
                            if (!child.Header.ToString().Contains(searchPhrase) || child is CustomTreeViewItem == false) continue;

                            // Header does contain information, this is the stage treeviewitem and we need to pull the ID
                            stageID = (child as CustomTreeViewItem).DeserializeObject<string>();
                            stageFound = true;
                            break;
                        }
                    }
                    else // Correct stage item was selected
                    {
                        // Make sure it is a custom item
                        if (_lastItemSelected is CustomTreeViewItem == false) return;
                        // This is the stage treeviewitem and we need to pull the ID
                        stageID = (_lastItemSelected as CustomTreeViewItem).DeserializeObject<string>();
                        stageFound = true;
                    }

                    // Check to make sure we found the stage
                    if (!stageFound)
                    {
                        AddLog("No stage found", false);
                        return;
                    }

                    // Get the stage reference
                    stageReference = MainScreen.CaseHandler.CurrentCase.Stages.FirstOrDefault(s => s.ID == stageID);
                    if (stageReference == null)
                    {
                        CreateFlyout(insertItem as FrameworkElement, $"No stage found with ID {stageID}");
                        return;
                    }

                    AddLog($"Stage found: {stageID}", false);

                    // Create new scene item
                    var newSceneItem = new SceneItem();
                    newSceneItem.InitializeNew();

                    stageReference.SceneItems.Add(newSceneItem);
                }
                await MainScreen.CaseHandler.GenerateTreeViewData();
            }
            catch (Exception ex)
            {
                AddLog(ex.ToString(), false);
            }
        }

        private async void DeleteItem(TreeViewItem node)
        {
            if (node != null)
            {
                Logger.Logger.AddLog($"CaseViewer.DeleteItem {node.Header} selected", true);
                if (node.Header.ToString().Contains("Scene Items -"))
                {
                    bool dialog = await CreateHeaderDialog($"Permanently Delete {node.Header}?", "Are you sure you would like to delete this item?", "Cancel", "Yes");
                    if (!dialog) return;

                    var foundStage = GetRootNodeFromID(ENodeName.Stage, MainScreen.CaseHandler.CurrentCase.Stages.Cast<IDataBase>().ToList(), node, out var stageIndex, out _);
                    if (!foundStage)
                    {
                        CreateFlyout(deleteItem as FrameworkElement, $"No stage found");
                        return;
                    }
                    var stage = MainScreen.CaseHandler.CurrentCase.Stages[stageIndex];

                    var foundScene = GetRootNodeFromID(ENodeName.Scene, stage.SceneItems.Cast<IDataBase>().ToList(), node, out var sceneIndex, out _);
                    if (!foundScene)
                    {
                        CreateFlyout(deleteItem as FrameworkElement, $"No scene item found");
                        return;
                    }
                    stage.SceneItems.RemoveAt(stageIndex);
                }
                else if (node.Header.ToString().Contains("Question -"))
                {
                    bool dialog = await CreateHeaderDialog($"Permanently Delete {node.Header}?", "Are you sure you would like to delete this item?", "Cancel", "Yes");
                    if (!dialog) return;

                    var foundStage = GetRootNodeFromID(ENodeName.Stage, MainScreen.CaseHandler.CurrentCase.Stages.Cast<IDataBase>().ToList(), node, out var stageIndex, out _);
                    if (!foundStage)
                    {
                        CreateFlyout(deleteItem as FrameworkElement, $"No stage found");
                        return;
                    }
                    var stage = MainScreen.CaseHandler.CurrentCase.Stages[stageIndex];

                    var foundEntity = GetRootNodeFromID(ENodeName.Scene, stage.SceneItems.Cast<IDataBase>().ToList(), node, out var entityIndex, out _);
                    if (!foundEntity)
                    {
                        CreateFlyout(deleteItem as FrameworkElement, $"No entity found");
                        return;
                    }
                    var interrogation = stage.SceneItems[entityIndex].InteractionSettings.Interrogation;
                    var question = interrogation.Questions.FirstOrDefault(s => s.Question == node.Header.ToString().Split(" - ")[1]);
                    interrogation.Questions.RemoveAt(interrogation.Questions.IndexOf(question));
                }
                else if (node.Header.ToString().Contains("Stage -"))
                {
                    bool dialog = await CreateHeaderDialog($"Permanently Delete {node.Header}?", "Are you sure you would like to delete this item?", "Cancel", "Yes");
                    if (!dialog) return;

                    var foundStage = GetRootNodeFromID(ENodeName.Stage, MainScreen.CaseHandler.CurrentCase.Stages.Cast<IDataBase>().ToList(), node, out var index, out _);
                    if (!foundStage)
                    {
                        CreateFlyout(deleteItem as FrameworkElement, $"No stage found");
                        return;
                    }
                    MainScreen.CaseHandler.CurrentCase.Stages.RemoveAt(index);
                }
                else if (node.Header.ToString().Contains("Document -"))
                {
                    bool dialog = await CreateHeaderDialog($"Permanently Delete {node.Header}?", "Are you sure you would like to delete this item?", "Cancel", "Yes");
                    if (!dialog) return;

                    var foundDoc = GetRootNodeFromID(ENodeName.Document, MainScreen.CaseHandler.CurrentCase.Documents.Cast<IDataBase>().ToList(), node, out var index, out _);
                    if (!foundDoc)
                    {
                        CreateFlyout(deleteItem as FrameworkElement, $"No document found");
                        return;
                    }
                    MainScreen.CaseHandler.CurrentCase.Documents.RemoveAt(index);
                }
                else if (node.Header.ToString().Contains("Information -"))
                {
                    bool dialog = await CreateHeaderDialog($"Permanently Delete {node.Header}?", "Are you sure you would like to delete this item?", "Cancel", "Yes");
                    if (!dialog) return;

                    var found = GetRootNodeFromID(ENodeName.Information, MainScreen.CaseHandler.CurrentCase.DetailedInformation.Cast<IDataBase>().ToList(), node, out var index, out _);
                    if (!found)
                    {
                        CreateFlyout(deleteItem as FrameworkElement, $"No detailed information found");
                        return;
                    }
                    MainScreen.CaseHandler.CurrentCase.DetailedInformation.RemoveAt(index);
                }
            }
            await MainScreen.CaseHandler.GenerateTreeViewData();
        }

        private bool GetRootNodeFromID(ENodeName nodeType, List<IDataBase> itemList, TreeViewItem startingNode, out int outBaseIndex, out TreeViewItem outNode)
        {
            try
            {
            Logger.Logger.AddLog($"GetRootNodeFromID({GetIDNodeNameFromEnum(nodeType)}), {itemList.Count}, {startingNode == null}", true);
            bool found = false;
            outNode = null;
            outBaseIndex = -1;
            foreach (var item in itemList)
            {
                if (found) break;
                TreeViewItem tempNode = null;
                tempNode = startingNode;
                var depth = 100;
                for (int i = 0; i < depth; i++)
                {
                    if (tempNode == null || tempNode.Parent == null || (tempNode.Parent as TreeViewItem) == null) break;
                    AddLog($"{i}/{depth} | {found} | Checking node: {tempNode.Header} with parent {(tempNode.Parent as TreeViewItem).Header}", true);
                    if (found || tempNode == null || tempNode.Header == null) break;
                    if (tempNode.Header.ToString().Contains(GetIDNodeNameFromEnum(nodeType)))
                    {
                        var customItem = tempNode as CustomTreeViewItem;
                        if (item.ID != customItem.DeserializeObject<string>()) continue;
                        outBaseIndex = itemList.IndexOf(item);
                        outNode = customItem;
                        found = true;
                        AddLog($"Found node", true);
                        break;
                    }

                    if (tempNode.Items.Count > 0)
                    {
                        foreach (TreeViewItem child in tempNode.Items)
                        {
                            if (child == null || child.Header == null) continue;
                            AddLog($"-----Checking child: {child.Header}", true);
                            if (!child.Header.ToString().Contains(GetIDNodeNameFromEnum(nodeType))) continue;

                            var customItem = child as CustomTreeViewItem;
                            if (item.ID != customItem.DeserializeObject<string>()) continue;
                            outBaseIndex = itemList.IndexOf(item);
                            outNode = customItem;
                            found = true;
                            AddLog($"Found node", true);
                            break;
                        }
                    }

                    tempNode = tempNode.Parent as TreeViewItem;
                }

                if (found) break;
                AddLog($"Checking node: {startingNode.Header} children", true);
                if (startingNode.Items.Count > 0)
                {
                    foreach (TreeViewItem child in tempNode.Items)
                    {
                        if (child == null || child.Header == null) continue;
                        Logger.Logger.AddLog($"-----Checking child: {child.Header}", true);
                        if (!child.Header.ToString().Contains(GetIDNodeNameFromEnum(nodeType))) continue;

                        var customItem = child as CustomTreeViewItem;
                        if (item.ID != customItem.DeserializeObject<string>()) continue;
                        outBaseIndex = itemList.IndexOf(item);
                        outNode = customItem;
                        found = true;
                        AddLog($"Found node", true);
                        break;
                    }
                }
            }
            Logger.Logger.AddLog($"Found: {found} | found node: {outNode.Header} | found index: {outBaseIndex}", true);
            return found;
            }
            catch (Exception e)
            {
                ShowError(e);
                throw;
            }

            return false;
        }

        public enum ENodeName { Stage, Document, Scene, Information }
        public string GetIDNodeNameFromEnum(ENodeName node)
        {
            if (node == ENodeName.Stage) return "Stage ID";
            if (node == ENodeName.Document) return "ID";
            if (node == ENodeName.Scene) return "Scene ID";
            if (node == ENodeName.Information) return "ID";
            else return string.Empty;
        }

        private async Task<bool> CreateHeaderDialog(string title, string Header, string closeButtonText, string primaryButtonText)
        {
            ContentDialog nullHandler = new ContentDialog
            {
                Title = title,
                Content = Header,
                CloseButtonText = closeButtonText,
                PrimaryButtonText = primaryButtonText
            };

            return await nullHandler.ShowAsync() == ContentDialogResult.Primary;
        }

        internal static void CreateFlyout(FrameworkElement sender, string text)
        {
            AddLog($"CreateFlyout: {text}", true);
            Flyout flyout = new Flyout();
            flyout.Content = new TextBlock()
            {
                Text = text
            };
            flyout.ShowAt(sender);
        }

        public void UpdateCaseData(TreeViewItem node)
        {
            AddLog($"UpdateCaseData with new node {node.Header}", true);
            CaseContent.Items.Clear();
            CaseContent.Items.Add(node);
            CaseContent.Items.Refresh();
        }

        internal static bool CheckIDs(CaseViewer page, CustomTreeViewItem currentNode, string id)
        {
            foreach (TreeViewItem node in page.CaseContent.Items)
            {
                foreach (TreeViewItem child in node.Items)
                {
                    if (!child.Header.ToString().Contains("ID")) continue;
                    CustomTreeViewItem myNode = (CustomTreeViewItem)child;
                    if (currentNode == myNode) continue;
                    if (myNode.Value == id) return false;
                }
            }
            return true;
        }

        private TreeViewItem _lastItemSelected;

        private async void Box_ItemInvoked(object? sender, RoutedPropertyChangedEventArgs<object> args)
        {
            if (sender == null) return;
            if (args.NewValue is not TreeViewItem item) return;
            
            _lastItemSelected = item;
            AddLog($"Selected item: {_lastItemSelected.Header} and is deletable {_lastItemSelected.GetType()}", true);
            
            ClearEditingBoxes();

            if (_lastItemSelected is MasterTreeViewItem masterTreeViewItem)
            {
                CreateSaveButton();
                AddLog($"Item {masterTreeViewItem.Name} is master attr {masterTreeViewItem.Object}", true);
                CreateEditingBoxes(masterTreeViewItem.Type, masterTreeViewItem.Object);
            }
            else if (args.NewValue is CustomTreeViewItem customTreeViewItem)
            {
                CreateSaveButton();
                CreateInteractiveButton(customTreeViewItem.Type, customTreeViewItem);
            }
        }

        private void ClearEditingBoxes()
        {
            EditStackPanel.Children.Clear();
            _buttonList.Clear();
        }

        private List<InteractiveMasterItem> _buttonList = new List<InteractiveMasterItem>();

        private void CreateEditingBoxes(Type type, object item)
        {
            var rootProperties = type.GetProperties();
            foreach (var prop in rootProperties)
            {
                if (prop.GetCustomAttribute(typeof(UserEditableAttribute)) is not UserEditableAttribute
                    resultingProperty) continue;
                
                if (resultingProperty.IsMaster)
                {
                    CreateEditingBoxes(resultingProperty.ItemType, prop.GetValue(item));
                }
                
                var node = CreateNodeForSimpleItem(prop, item);

                CreateInteractiveButton(resultingProperty.ItemType, node);
            }
        }

        private void CreateInteractiveButton(Type type, CustomTreeViewItem node)
        {
            var boxType = GetBoxType(type);
            if (boxType == InteractiveMasterItem.BoxType.None) return;

            var caseViewer = this;
            var button = new InteractiveMasterItem(EditStackPanel, boxType, node, ref caseViewer);
            _buttonList.Add(button);
        }

        private InteractiveMasterItem.BoxType GetBoxType(Type type)
        {
            var boxType = InteractiveMasterItem.BoxType.None;
            if (type == typeof(string)) boxType = InteractiveMasterItem.BoxType.TextBox;
            else if (type == typeof(List<string>)) boxType = InteractiveMasterItem.BoxType.ComboBoxStringList;
            else if (type == typeof(TimeSpan)) boxType = InteractiveMasterItem.BoxType.TimePicker;
            else if (type == typeof(SceneItem.EItemType)) boxType = InteractiveMasterItem.BoxType.EItemType;
            else if (type == typeof(InterrogationLine.EResponseType))
                boxType = InteractiveMasterItem.BoxType.InterrogationAnswerType;
            else if (type == typeof(SpawnPointTemp)) boxType = InteractiveMasterItem.BoxType.SpawnPoint;
            else if (type == typeof(bool)) boxType = InteractiveMasterItem.BoxType.Boolean;
            else if (type == typeof(float)) boxType = InteractiveMasterItem.BoxType.Float;
            else if (type == typeof(int)) boxType = InteractiveMasterItem.BoxType.Integer;
            else if (type == typeof(System.Drawing.Color)) boxType = InteractiveMasterItem.BoxType.Color;
            return boxType;
        }
        
        private Button SaveButton { get; set; }
        private void CreateSaveButton()
        {
            SaveButton = new Button()
            {
                Content = "Save Changes",
                Margin = new Thickness(0d, 5d, 0d, 5d),
                HorizontalAlignment = HorizontalAlignment.Center,
                MinWidth = 150
            };
            
            SaveButton.Click += UIButton_Click;
            
            EditStackPanel.Children.Add(SaveButton);
            AddSeparator();
        }

        private void AddSection(string name)
        {
            var label = new Label()
            {
                Margin = new Thickness(0d, 2d, 0d, 2d),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Content = name
            };

            EditStackPanel.Children.Add(label);
        }
        
        private void AddSeparator()
        {
            var border = new Separator()
            {
                Margin = new Thickness(0d, 2d, 0d, 2d),
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

            EditStackPanel.Children.Add(border);
        }
       
        private void UIButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender != SaveButton) return;

            foreach (var item in _buttonList)
            {
                Logger.Logger.AddLog($"Saving {item.Name}", true);
                var result = item.SaveItem(sender);
                this.GetItemAndUpdate(item.Node, result);
            }
            Save();
            CaseContent.Items.Refresh();
            ClearEditingBoxes();
        }

        private CustomTreeViewItem CreateNodeForSimpleItem(PropertyInfo property, object obj)
        {
            var editable = GetAttributeData.IsEditable(property, out var name, out var desc, out Type type);
            return new CustomTreeViewItem(name, desc, editable, type, ref property, ref obj);
        }
        
        private void CreateEditingBoxes(CustomTreeViewItem item)
        {
            ClearEditingBoxes();
            var caseViewer = this;
            
            var boxType = GetBoxType(item.Type);
            if (boxType == InteractiveMasterItem.BoxType.None) return;
                
            //_button = new InteractiveItems(EditStackPanel, boxType, item, ref caseViewer);
        }

        internal void GetItemAndUpdate(CustomTreeViewItem item, object data)
        {
            try
            {
                item.PropertyRef.SetValue(item.Object, data);
            }
            catch (Exception ex)
            {
                AddLog($"Error: {ex}", false);
            }
        }

        public static string SplitCamelCase(string input)
        {
            return Regex.Replace(input, "([A-Z])", " $1", System.Text.RegularExpressions.RegexOptions.Compiled).Trim();
        }

        public void Save()
        {
            MainScreen.CaseHandler.Save();
        }
        
        private void ShowError(Exception exception, bool displayMessage = true)
        {
            if (displayMessage) MessageBox.Show(exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            if (displayMessage) MessageBox.Show(exception.StackTrace, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Logger.Logger.AddLog($"!! ERROR !! {exception.Message}", false);
            Logger.Logger.AddLog($"!! ERROR !! {exception.StackTrace}", false);
            
        }
    }

}
