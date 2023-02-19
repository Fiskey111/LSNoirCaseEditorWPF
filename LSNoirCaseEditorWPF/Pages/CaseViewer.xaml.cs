using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using CaseManager.NewData;
using CaseManager.Resources;
using LSNoirCaseEditorWPF.Logic;
using LSNoirCaseEditorWPF.Windows;
using Microsoft.Win32;
using ModernWpf.Controls;
using Newtonsoft.Json;
using Xceed.Wpf.Toolkit;
using static LSNoirCaseEditorWPF.Logger.Logger;
using Color = System.Windows.Media.Color;
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
            _button?.Remove();
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
                        var searchPhrase = "Scene Item - ";
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
                if (node.Header.ToString().Contains("Scene Item -"))
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
                if (node.Header.ToString().Contains("Question -"))
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
            Logger.Logger.AddLog($"GetRootNodeFromID({GetIDNodeNameFromEnum(nodeType)})", true);
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
                    if (tempNode == null || tempNode.Parent == null) break;
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

        internal List<string> PullAllIDs()
        {
            var list = MainScreen.CaseHandler.AllNodes.Where(node => node.Header.ToString().Contains("ID")).Cast<CustomTreeViewItem>().ToList();
            return list.Select(item => item.Value).ToList();
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

        private async void Box_ItemInvoked(object sender, RoutedPropertyChangedEventArgs<object> args)
        {
            if (sender != null)
            {
                if (args.NewValue is TreeViewItem)
                {
                    _lastItemSelected = args.NewValue as TreeViewItem;
                    Logger.Logger.AddLog($"Selected item: {_lastItemSelected.Header}", true);
                }
                else
                {
                    _lastItemSelected = null;
                    Logger.Logger.AddLog($"No item selected", true);
                }

                if (args.NewValue is CustomTreeViewItem)
                {
                    CustomTreeViewItem myNode = (CustomTreeViewItem)args.NewValue;
                    if (myNode != null)
                    {
                        CreateEditingBoxes(myNode);
                    }
                    else
                    {
                        _button?.Remove();
                    }
                }
                else if (MainScreen.CaseHandler.UnselectableNodes.Any(n => n == (TreeViewItem)args.NewValue))
                {
                    _button?.Remove();
                }
                else
                {
                    _button?.Remove();
                }
            }
        }

        private InteractiveButton _button;

        private void CreateEditingBoxes(CustomTreeViewItem item)
        {
            _button?.Remove();
            var caseViewer = this;
            if (item.Type == typeof(string)) // textbox
            {
                _button = new InteractiveButton(editStackPanel, InteractiveButton.BoxType.TextBox, item, ref caseViewer);
            }
            else if (item.Type == typeof(List<string>)) // listofstrings
            {
                _button = new InteractiveButton(editStackPanel, InteractiveButton.BoxType.ComboBoxStringList, item, ref caseViewer);
            }
            else if (item.Type == typeof(TimeSpan)) // time
            {
                _button = new InteractiveButton(editStackPanel, InteractiveButton.BoxType.TimePicker, item, ref caseViewer);
            }
            else if (item.Type == typeof(SceneItem.EItemType)) // EItemType
            {
                _button = new InteractiveButton(editStackPanel, InteractiveButton.BoxType.EItemType, item, ref caseViewer);
            }
            else if (item.Type == typeof(InterrogationLine.EResponseType)) // EResponseType
            {
                _button = new InteractiveButton(editStackPanel, InteractiveButton.BoxType.InterrogationAnswerType, item, ref caseViewer);
            }
            else if (item.Type == typeof(CaseManager.Resources.SpawnPoint)) // spawnpoint
            {
                _button = new InteractiveButton(editStackPanel, InteractiveButton.BoxType.SpawnPoint, item, ref caseViewer);
            }
            else if (item.Type == typeof(bool)) // bool
            {
                _button = new InteractiveButton(editStackPanel, InteractiveButton.BoxType.Boolean, item, ref caseViewer);
            }
            else if (item.Type == typeof(float)) // float
            {
                _button = new InteractiveButton(editStackPanel, InteractiveButton.BoxType.Float, item, ref caseViewer);
            }
            else if (item.Type == typeof(int)) // int
            {
                _button = new InteractiveButton(editStackPanel, InteractiveButton.BoxType.Integer, item, ref caseViewer);
            }
            else if (item.Type == typeof(System.Drawing.Color)) // Color
            {
                _button = new InteractiveButton(editStackPanel, InteractiveButton.BoxType.Color, item, ref caseViewer);
            }
        }

        internal void GetItemAndUpdate(CustomTreeViewItem item, object data)
        {
            try
            {
                AddLog($"GetItemAndUpdate({item.Name}|{data})", true);
                item.PropertyRef.SetValue(item.Object, data);
                Save();
                CaseContent.Items.Refresh();
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
    }

    public class InteractiveButton : Button
    {
        private CustomTreeViewItem Item { get; set; }
        private BoxType EBoxType { get; set; }
        private UIElement DataBox { get; set; }
        private Button SaveButton { get; set; }
        private Button AddButton { get; set; }
        private Button RemoveButton { get; set; }

        private List<TextBox> SpawnPointBoxes { get; set; } = new List<TextBox>();
        private List<TextBox> RotationBoxes { get; set; } = new List<TextBox>();

        private StackPanel Panel { get; set; }
        private CaseViewer PageRef;

        public InteractiveButton(StackPanel panel, BoxType boxType, CustomTreeViewItem item, ref CaseViewer pageRef)
        {
            try
            {
                Item = item;
                EBoxType = boxType;

                Panel = panel;
                Panel.Children.Clear();

                PageRef = pageRef;

                CreateLabels();

                CreateBox();

                CreateButton();
            }
            catch (Exception ex)
            {
                Logger.Logger.AddLog(ex.ToString(), true);
            }
        }

        private void CreateLabels()
        {
            var label = new TextBlock()
            {
                Text = Item.Name
            };
            var description = new TextBlock()
            {
                Text = Item.Description,
                Margin = new Thickness(0d, 5d, 0d, 0d),
                FontSize = 10,
            };

            Panel.Children.Add(label);
            Panel.Children.Add(description);
        }

        private void CreateBox()
        {
            if (EBoxType == BoxType.TextBox)
            {
                DataBox = new TextBox()
                {
                    Text = Item.DeserializeObject<string>(),
                    IsEnabled = Item.Editable,
                    MinWidth = 400,
                    Margin = new Thickness(0d, 5d, 0d, 0d),
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                Panel.Children.Add(DataBox);
            }
            else if (EBoxType == BoxType.ComboBoxStringList)
            {
                var box = new ComboBox()
                {
                    IsEnabled = Item.Editable,
                    MinWidth = 400,
                    Margin = new Thickness(0d, 5d, 0d, 0d),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    IsEditable = Item.Editable
                };
                var data = Item.DeserializeObject<List<string>>();
                if (data == null)
                {
                    data = new List<string>();
                }
                foreach (var d in data)
                {
                    box.Items.Add(d);
                }
                if (box.Items.Count > 0)
                {
                    box.SelectedIndex = 0;
                }
                AddButton = new Button()
                {
                    Content = "Add Item",
                    Margin = new Thickness(0d, 5d, 5d, 0d),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    MinWidth = 100
                };
                RemoveButton = new Button()
                {
                    Content = "Remove Item",
                    Margin = new Thickness(0d, 5d, 0d, 0d),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    MinWidth = 100
                };

                AddButton.Click += ButtonClick;
                RemoveButton.Click += ButtonClick;

                DataBox = box;

                var secondaryPanel = new StackPanel()
                {
                    Orientation = Orientation.Horizontal
                };

                secondaryPanel.Children.Add(AddButton);
                secondaryPanel.Children.Add(RemoveButton);
                Panel.Children.Add(DataBox);
                Panel.Children.Add(secondaryPanel);
            }
            else if (EBoxType == BoxType.TimePicker)
            {
                DataBox = new TimePicker()
                {
                    Text = Item.DeserializeObject<TimeSpan>().ToString(),
                    IsEnabled = Item.Editable,
                    MinWidth = 400,
                    Margin = new Thickness(0d, 5d, 0d, 0d),
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                Panel.Children.Add(DataBox);
            }
            else if (EBoxType == BoxType.EItemType)
            {
                var box = new ComboBox()
                {
                    IsEnabled = Item.Editable,
                    MinWidth = 400,
                    Margin = new Thickness(0d, 5d, 0d, 0d),
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                foreach (var item in Enum.GetValues(typeof(SceneItem.EItemType)))
                {
                    box.Items.Add(item);
                }
                box.SelectedIndex = (int)Item.DeserializeObject<SceneItem.EItemType>();
                DataBox = box;
                Panel.Children.Add(DataBox);
            }
            else if (EBoxType == BoxType.InterrogationAnswerType)
            {
                var box = new ComboBox()
                {
                    IsEnabled = Item.Editable,
                    MinWidth = 400,
                    Margin = new Thickness(0d, 5d, 0d, 0d),
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                foreach (var item in Enum.GetValues(typeof(InterrogationLine.EResponseType)))
                {
                    box.Items.Add(item);
                }
                box.SelectedIndex = (int) Item.DeserializeObject<InterrogationLine.EResponseType>();
                DataBox = box;
                Panel.Children.Add(DataBox);
            }
            else if (EBoxType == BoxType.Boolean)
            {
                var checkLabel = new TextBlock()
                {
                    Text = "Enabled:",
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                DataBox = new CheckBox()
                {
                    IsChecked = Item.DeserializeObject<bool?>(),
                    IsEnabled = Item.Editable,
                    MinWidth = 400,
                    Margin = new Thickness(0d, 5d, 0d, 0d),
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                var subPanel = new StackPanel()
                {
                    Orientation = Orientation.Horizontal
                };
                subPanel.Children.Add(checkLabel);
                subPanel.Children.Add(DataBox);
                Panel.Children.Add(subPanel);
            }
            else if (EBoxType == BoxType.Float)
            {
                DataBox = CreateNumberBox("Value", Convert.ToSingle(Item.Value), out var subPanel);
                Panel.Children.Add(subPanel);
            }
            else if (EBoxType == BoxType.Integer)
            {
                DataBox = CreateNumberBox("Value", Convert.ToInt32(Item.Value), out var subPanel);
                Panel.Children.Add(subPanel);
            }
            else if (EBoxType == BoxType.DateTime)
            {
                DataBox = new DateTimePicker()
                {
                    Format = DateTimeFormat.ShortDate,
                    Value = Item.DeserializeObject<DateTime>()
                };
                Panel.Children.Add(DataBox);
            }
            else if (EBoxType == BoxType.Color)
            {
                DataBox = new ColorCanvas()
                {
                    SelectedColor = ConvertToWindowsColor(Item.DeserializeObject<System.Drawing.Color>()),
                    UsingAlphaChannel = true,
                    IsEnabled = Item.Editable,
                    MinWidth = 400,
                    Margin = new Thickness(0d, 5d, 0d, 0d),
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                Panel.Children.Add(DataBox);
            }
            else if (EBoxType == BoxType.SpawnPoint)
            {
                var subPanel = new StackPanel();

                var grid = new Grid()
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(75) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(250) });

                var spawnpoint = Item.DeserializeObject<SpawnPoint>();
                if (spawnpoint == null) spawnpoint = SpawnPoint.Zero;

                string[] axes = new[] { "x", "y", "z" };
                string[] rotation = new[] { "heading", "roll", "pitch", "yaw" };
                foreach (var axis in axes)
                {
                    grid.RowDefinitions.Add(new RowDefinition());
                    var value = 0f;
                    switch (axis)
                    {
                        case "x":
                            value = spawnpoint.Position.X;
                            break;
                        case "y":
                            value = spawnpoint.Position.Y;
                            break;
                        case "z":
                            value = spawnpoint.Position.Z;
                            break;
                    }
                    SpawnPointBoxes.Add(CreateNumberBox(axis, grid, value));
                }
                foreach (var axis in rotation)
                {
                    grid.RowDefinitions.Add(new RowDefinition());
                    var value = 0f;
                    switch (axis)
                    {
                        case "heading":
                            value = spawnpoint.Heading;
                            break;
                        case "roll":
                            value = spawnpoint.Rotation.Roll;
                            break;
                        case "pitch":
                            value = spawnpoint.Rotation.Pitch;
                            break;
                        case "yaw":
                            value = spawnpoint.Rotation.Yaw;
                            break;
                    }
                    RotationBoxes.Add(CreateNumberBox(axis, grid, value));
                    if (axis == "heading")
                    {
                        grid.RowDefinitions.Add(new RowDefinition());
                        var orLabel = new TextBlock();

                        orLabel.Text = "OR";
                        orLabel.Margin = new Thickness(0, 5, 0, 5);
                        orLabel.HorizontalAlignment = HorizontalAlignment.Center;
                        orLabel.VerticalAlignment = VerticalAlignment.Center;
                        Grid.SetColumn(orLabel, 0);
                        Grid.SetColumnSpan(orLabel, 2);
                        Grid.SetRow(orLabel, grid.RowDefinitions.Count - 1);
                        grid.Children.Add(orLabel);
                    }
                }

                Panel.Children.Add(grid);
            }
        }

        private Color ConvertToWindowsColor(System.Drawing.Color color)
        {
            return new Color()
            {
                A = color.A,
                B = color.B,
                G = color.G,
                R = color.R
            };
        }
        private System.Drawing.Color ConvertToDrawingColor(Color color)
        {
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        private TextBox CreateNumberBox(string positionAxis, float currentValue, out StackPanel panel)
        {
            var label = new TextBlock()
            {
                Text = positionAxis,
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center,

            };
            var numberBox = new TextBox()
            {
                Name = positionAxis,
                Margin = new Thickness(0, 10, 5, 0),
                MinWidth = 200,
                Text = currentValue.ToString()
            };
            numberBox.TextChanged += Float_OnBeforeTextChanging;

            panel = new StackPanel()
            {
                Orientation = Orientation.Horizontal
            };
            panel.Children.Add(label);
            panel.Children.Add(numberBox);
            return numberBox;
        }
        private TextBox CreateNumberBox(string positionAxis, int currentValue, out StackPanel panel)
        {
            var label = new TextBlock()
            {
                Text = positionAxis,
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center,

            };
            var numberBox = new TextBox()
            {
                Name = positionAxis,
                Margin = new Thickness(0, 10, 5, 0),
                MinWidth = 200,
                Text = currentValue.ToString()
            };
            numberBox.TextChanged += Integer_OnBeforeTextChanging;

            panel = new StackPanel()
            {
                Orientation = Orientation.Horizontal
            };
            panel.Children.Add(label);
            panel.Children.Add(numberBox); ;
            return numberBox;
        }

        private TextBox CreateNumberBox(string positionAxis, Grid grid, float currentValue)
        {
            var rect = new Rectangle();
            if (positionAxis == "heading")
            {
                var color = new SolidColorBrush(Colors.Blue);
                rect.Margin = new Thickness(0, 10, 5, 0);
                color.Opacity = 0.2;
                rect.Fill = color;
            }
            else if (positionAxis == "roll" || positionAxis == "pitch" || positionAxis == "yaw")
            {
                var color = new SolidColorBrush(Colors.Green);
                color.Opacity = 0.2;
                rect.Fill = color;
                if (positionAxis == "roll") rect.Margin = new Thickness(0, 10, 5, 0);
            }
            var label = new TextBlock()
            {
                Text = positionAxis,
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center,

            };
            var numberBox = new TextBox()
            {
                Name = positionAxis,
                Margin = new Thickness(0, 10, 5, 0),
                MinWidth = 200,
                Text = currentValue.ToString()
            };
            numberBox.TextChanged += Float_OnBeforeTextChanging;

            Grid.SetColumn(label, 0);
            Grid.SetColumn(numberBox, 1);
            Grid.SetColumn(rect, 0);
            Grid.SetColumnSpan(rect, 2);
            Grid.SetRow(rect, grid.RowDefinitions.Count - 1);
            Grid.SetRow(label, grid.RowDefinitions.Count - 1);
            Grid.SetRow(numberBox, grid.RowDefinitions.Count - 1);
            grid.Children.Add(rect);
            grid.Children.Add(label);
            grid.Children.Add(numberBox);
            return numberBox;
        }
        private TextBox CreateNumberBox(string positionAxis, Grid grid, int currentValue)
        {
            var label = new TextBlock()
            {
                Text = positionAxis,
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center,

            };
            var numberBox = new TextBox()
            {
                Name = positionAxis,
                Margin = new Thickness(0, 10, 5, 0),
                MinWidth = 200,
                Text = currentValue.ToString()
            };
            numberBox.TextChanged += Integer_OnBeforeTextChanging; ;

            Grid.SetColumn(label, 0);
            Grid.SetColumn(numberBox, 1);
            Grid.SetRow(label, grid.RowDefinitions.Count - 1);
            Grid.SetRow(numberBox, grid.RowDefinitions.Count - 1);
            grid.Children.Add(label);
            grid.Children.Add(numberBox);
            return numberBox;
        }

        private void Integer_OnBeforeTextChanging(object sender, TextChangedEventArgs  args)
        {
            var text = (sender as TextBox).Text;
            if (!int.TryParse(text, out _) && text != "-")
            {
                args.Handled = true;
                (sender as TextBox).Undo();
            }
        }

        private void Float_OnBeforeTextChanging(object sender, TextChangedEventArgs  args)
        {
            var text = (sender as TextBox).Text;
            if (!Regex.Match(text, @"-?[0-9]*\.?[0-9]+").Success)
            {
                if (text != "-")
                {
                    args.Handled = true;
                    (sender as TextBox).Undo();
                }
                else if (!float.TryParse(text, out _))
                {
                    args.Handled = true;
                    (sender as TextBox).Undo();
                }
            }
        }

        public void Remove()
        {
            Panel?.Children.Clear();
            if (AddButton != null) AddButton.Click -= ButtonClick;
            if (RemoveButton != null) RemoveButton.Click -= ButtonClick;
            if (SaveButton != null) SaveButton.Click -= UIButton_Click;
        }

        private void ButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender == AddButton)
                {
                    var comboBox = DataBox as ComboBox;
                    var selectedItem = comboBox.SelectedItem.ToString();
                    comboBox.Items.Add(selectedItem);
                }
                else if (sender == RemoveButton)
                {
                    var comboBox = DataBox as ComboBox;
                    comboBox.Items.RemoveAt(comboBox.SelectedIndex);
                }
            }
            catch (Exception ex)
            {
                CaseViewer.CreateFlyout(sender as FrameworkElement, ex.ToString());
            }
        }

        private void CreateButton()
        {
            SaveButton = new Button()
            {
                Content = "Save Changes",
                Margin = new Thickness(0d, 5d, 0d, 0d),
                HorizontalAlignment = HorizontalAlignment.Left,
                MinWidth = 150
            };
            SaveButton.Click += UIButton_Click;

            Panel.Children.Add(SaveButton);
        }

        private void UIButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender != SaveButton) return;

            string serializedData = string.Empty;
            object unserializedData = null;
            bool validType = false;

            if (EBoxType == BoxType.TextBox)
            {
                var box = DataBox as TextBox;
                serializedData = JsonConvert.SerializeObject(box.Text);
                unserializedData = box.Text;
                validType = Convert.ChangeType(unserializedData, Item.Type) != null;
            }
            else if (EBoxType == BoxType.ComboBoxStringList)
            {
                var box = DataBox as ComboBox;
                List<string> saveData = (from object? item in box.Items select item.ToString()).ToList();
                serializedData = JsonConvert.SerializeObject(saveData);
                unserializedData = saveData;
                validType = Convert.ChangeType(unserializedData, Item.Type) != null;
            }
            else if (EBoxType == BoxType.EItemType)
            {
                var box = DataBox as ComboBox;
                Enum.TryParse(typeof(SceneItem.EItemType), box.SelectedValue.ToString(), out var saveData);
                serializedData = JsonConvert.SerializeObject(saveData);
                unserializedData = saveData;
                validType = Convert.ChangeType(unserializedData, Item.Type) != null;
            }
            else if (EBoxType == BoxType.InterrogationAnswerType)
            {
                var box = DataBox as ComboBox;
                Enum.TryParse(typeof(InterrogationLine.EResponseType), box.SelectedValue.ToString(), out var saveData);
                serializedData = JsonConvert.SerializeObject(saveData);
                unserializedData = saveData;
                validType = Convert.ChangeType(unserializedData, Item.Type) != null;
            }
            else if (EBoxType == BoxType.TimePicker)
            {
                var box = DataBox as TimePicker;
                serializedData = JsonConvert.SerializeObject(box.Value);
                unserializedData = box.TimeInterval;
                validType = Convert.ChangeType(unserializedData, Item.Type) != null;
            }
            else if (EBoxType == BoxType.Float)
            {
                var data = GetFloatDataFromBox(DataBox as TextBox);

                serializedData = JsonConvert.SerializeObject(data);
                unserializedData = data;
                validType = Convert.ChangeType(unserializedData, Item.Type) != null;
            }
            else if (EBoxType == BoxType.Integer)
            {
                var data = GetIntDataFromBox(DataBox as TextBox);

                serializedData = JsonConvert.SerializeObject(data);
                unserializedData = data;
                validType = Convert.ChangeType(unserializedData, Item.Type) != null;
            }
            else if (EBoxType == BoxType.Color)
            {
                var box = DataBox as ColorCanvas;
                var data = ConvertToDrawingColor(box.SelectedColor.Value);

                serializedData = JsonConvert.SerializeObject(data);
                unserializedData = data;
                validType = Convert.ChangeType(unserializedData, Item.Type) != null;
            }
            else if (EBoxType == BoxType.DateTime)
            {
                var box = DataBox as DateTimePicker;
                DateTime data = (DateTime) box.Value;

                serializedData = JsonConvert.SerializeObject(data);
                unserializedData = data;
                validType = Convert.ChangeType(unserializedData, Item.Type) != null;
            }
            else if (EBoxType == BoxType.SpawnPoint)
            {
                string[] axes = new[] { "x", "y", "z" };
                string[] rotation = new[] { "heading", "roll", "pitch", "yaw" };

                SpawnPoint point = new SpawnPoint()
                {
                    Position = new VectorTemp(GetFloatDataFromBox(SpawnPointBoxes[0]), GetFloatDataFromBox(SpawnPointBoxes[1]), GetFloatDataFromBox(SpawnPointBoxes[2])),
                    Heading = GetFloatDataFromBox(RotationBoxes[0]),
                    Rotation = new RotatorTemp(GetFloatDataFromBox(RotationBoxes[1]), GetFloatDataFromBox(RotationBoxes[2]), GetFloatDataFromBox(RotationBoxes[3]))
                };

                serializedData = JsonConvert.SerializeObject(point);
                unserializedData = point;
                validType = Convert.ChangeType(unserializedData, Item.Type) != null;
            }
            else if (EBoxType == BoxType.Boolean)
            {
                var box = DataBox as CheckBox;
                serializedData = JsonConvert.SerializeObject(box.IsChecked);
                unserializedData = box.IsChecked;
                validType = Convert.ChangeType(unserializedData, Item.Type) != null;
            }

            if (!validType)
            {
                CaseViewer.CreateFlyout(sender as FrameworkElement, $"Invalid type\nMust be type: {Item.Type}");
            }

            if (Item.Name.Contains("ID"))
            {
                var okay = CaseViewer.CheckIDs(PageRef, Item, serializedData);
                if (!okay)
                {
                    CaseViewer.CreateFlyout(sender as FrameworkElement, "Duplicate ID value");
                    return;
                }
            }

            Item.SerializeObject(serializedData);
            PageRef.GetItemAndUpdate(Item, unserializedData);
        }

        private float GetFloatDataFromBox(TextBox box) => Convert.ToSingle(box.Text);
        private int GetIntDataFromBox(TextBox box) => Convert.ToInt32(box.Text);

        public enum BoxType { ComboBoxStringList, TextBox, TimePicker, SpawnPoint, Boolean, Float, Color, Integer, InterrogationAnswerType, EItemType, DateTime }
    }
}
