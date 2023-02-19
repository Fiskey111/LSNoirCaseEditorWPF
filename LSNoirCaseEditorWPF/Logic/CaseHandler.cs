using CaseManager.NewData;
using LSNoirCaseEditorWPF.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using static LSNoirCaseEditorWPF.Logger.Logger;

namespace LSNoirCaseEditorWPF.Logic
{
    public class CaseHandler
    {
        internal string CurrentFile { get; private set; }
        internal Case CurrentCase { get; private set; }
        internal List<string> Files = null;
        internal bool IsAutosaveEnabled { get; set; }

        internal DateTime LastUpdatedTime = DateTime.MinValue;

        internal bool AutomaticExpand = true;

        public delegate void CaseLoaded(TreeViewItem rootNode);
        public event CaseLoaded OnCaseReloaded;
        public void CaseLoadedFireEvent(TreeViewItem node) => OnCaseReloaded?.Invoke(node);

        internal void Load(string fileLocation)
        {
            CurrentFile = fileLocation;
            LoadCase();
        }

        internal void Save()
        {
            if (CurrentCase == null) return;

            Logger.Logger.AddLog($"Saving case from {CurrentFile}", false);

            File.WriteAllText(CurrentFile, JsonConvert.SerializeObject(CurrentCase, Formatting.Indented));

            LoadCase();
        }

        internal bool HasBeenChangedSinceUpdate()
        {
            return false;
        }

        private void LoadCase()
        {
            Logger.Logger.AddLog($"Loading case from {CurrentFile}", false);

            if (string.IsNullOrEmpty(CurrentFile)) return;

            var text = File.ReadAllText(CurrentFile);
            CurrentCase = JsonConvert.DeserializeObject<Case>(text);

            if (CurrentCase == null)
            {
                CurrentCase = new Case();
            }

            GenerateTreeViewData();
        }

        internal async Task GenerateTreeViewData()
        {
            Logger.Logger.AddLog($"Clearing nodes", false);
            AllNodes.Clear();
            UnselectableNodes.Clear();
            Application.Current.Dispatcher.Invoke(GetTreeViewData);
        }

        internal TreeViewItem RootNode { get; private set; }

        internal List<TreeViewItem> AllNodes { get; private set; } = new List<TreeViewItem>();
        internal List<TreeViewItem> UnselectableNodes { get; private set; } = new List<TreeViewItem>();
        internal List<CustomTreeViewItem> AllCustomNodes { get; private set; } = new List<CustomTreeViewItem>();

        private TreeViewItem CreateRootNode(string text)
        {
            TreeViewItem node = null;
            node = new TreeViewItem
            {
                Header = text,
                IsExpanded = AutomaticExpand
            };
            UnselectableNodes.Add(node);
            AllNodes.Add(node);
            Logger.Logger.AddLog($"CreateRootNode returned null: {node == null}", false);
            return node;
        }

        private async Task GetTreeViewData()
        {
            Logger.Logger.AddLog($"GetTreeViewData", false);
            DateTime startTime = DateTime.Now;
            RootNode = null;
            RootNode = CreateRootNode("Case Data");
            var rootDocumentNode = CreateRootNode("Documents");
            var rootInfoNode = CreateRootNode("Additional Information");
            var rootStageNode = CreateRootNode("Stages");

            foreach (var caseProperty in typeof(Case).GetProperties())
            {
                Logger.Logger.AddLog($"Checking property: {caseProperty.Name}", true);
                if (caseProperty.Name == nameof(CurrentCase.ID)) AddItemToTreeView(RootNode, caseProperty, CurrentCase);
                if (caseProperty.Name == nameof(CurrentCase.Name)) AddItemToTreeView(RootNode, caseProperty, CurrentCase);
                if (caseProperty.Name == nameof(CurrentCase.Description)) AddItemToTreeView(RootNode, caseProperty, CurrentCase);

                if (caseProperty.Name == nameof(CurrentCase.Documents))
                {
                    foreach (var doc in CurrentCase.Documents)
                    {
                        foreach (var attribute in caseProperty.GetCustomAttributes())
                        {
                            if (!(attribute is UserEditableAttribute a)) continue;
                            var docNode = CreateRootNode($"Document - {doc.ID}");

                            foreach (var property in typeof(CaseDocuments).GetProperties())
                            {
                                Logger.Logger.AddLog($"Checking property: {property.Name}", true);
                                if (property.Name == nameof(doc.ID)) AddItemToTreeView(docNode, property, doc);
                                if (property.Name == nameof(doc.RequiredCompletionsRequest)) AddItemToTreeView(docNode, property, doc);
                                if (property.Name == nameof(doc.RequiredCompletionsAccept)) AddItemToTreeView(docNode, property, doc);
                            }
                            rootDocumentNode.Items.Add(docNode);
                            Logger.Logger.AddLog($"rootDocumentNode Items = {rootDocumentNode.Items.Count}", true);
                        }
                    }
                }

                if (caseProperty.Name == nameof(CurrentCase.DetailedInformation))
                {
                    foreach (var info in CurrentCase.DetailedInformation)
                    {
                        foreach (var attribute in caseProperty.GetCustomAttributes())
                        {
                            if (!(attribute is UserEditableAttribute a)) continue;
                            var infoNode = CreateRootNode($"Information - {info.ID}");

                            foreach (var property in typeof(DetailedInformation).GetProperties())
                            {
                                Logger.Logger.AddLog($"Checking property: {property.Name}", true);
                                if (property.Name == nameof(info.ID)) AddItemToTreeView(infoNode, property, info);
                                if (property.Name == nameof(info.FirstName)) AddItemToTreeView(infoNode, property, info);
                                if (property.Name == nameof(info.LastName)) AddItemToTreeView(infoNode, property, info);
                                if (property.Name == nameof(info.Description)) AddItemToTreeView(infoNode, property, info);
                                if (property.Name == nameof(info.Model)) AddItemToTreeView(infoNode, property, info);
                                if (property.Name == nameof(info.Address)) AddItemToTreeView(infoNode, property, info);
                                if (property.Name == nameof(info.Birthday)) AddItemToTreeView(infoNode, property, info);
                            }
                            rootInfoNode.Items.Add(infoNode);
                            Logger.Logger.AddLog($"rootInfoNode Items = {rootInfoNode.Items.Count}", true);
                        }
                    }
                }

                if (caseProperty.Name == nameof(CurrentCase.Stages))
                {
                    foreach (var stage in CurrentCase.Stages)
                    {
                        var stageStartTime = DateTime.Now;
                        var stageNode = CreateRootNode($"Stage - {stage.ID}");
                        PropertyInfo[] stageProperties = typeof(Stage).GetProperties();
                        for (int i = 0; i < stageProperties.Length; i++)
                        {
                            PropertyInfo property = stageProperties[i];
                            Logger.Logger.AddLog($"Checking property: {property.Name}", true);
                            var sceneItemRoot = CreateRootNode($"Scene Items");
                            foreach (var at in property.GetCustomAttributes())
                            {
                                if (!(at is UserEditableAttribute _)) continue;

                                if (property.Name == nameof(stage.SceneItems)) stageNode.Items.Add(AddSceneItems(stage));

                                if (property.Name == nameof(stage.CallBlip))
                                {
                                    var node = CreateRootNode($"Blip");
                                    AddItemsForCustomObject(node, property, stage.CallBlip);
                                    stageNode.Items.Add(node);
                                }
                                else if (property.Name == nameof(stage.CallNotification))
                                {
                                    var node = CreateRootNode($"Notification");
                                    AddItemsForCustomObject(node, property, stage.CallNotification);
                                    stageNode.Items.Add(node);
                                }
                                else AddItemToTreeView(stageNode, property, stage);
                            }
                            Logger.Logger.AddLog($"stageNode Items = {stageNode.Items.Count}", true);
                        }
                        rootStageNode.Items.Add(stageNode);
                        Logger.Logger.AddLog($"Total time to load stage {stage.ID}: {(DateTime.Now - stageStartTime).TotalMilliseconds}ms", true);
                    }
                }
                Logger.Logger.AddLog($"rootStageNode Items = {rootStageNode.Items.Count}", true);
            }
            RootNode.Items.Add(rootStageNode);
            RootNode.Items.Add(rootDocumentNode);
            RootNode.Items.Add(rootInfoNode);

            CaseLoadedFireEvent(RootNode);
            DateTime endTime = DateTime.Now;
            Logger.Logger.AddLog($"Total time to load case: {(endTime - startTime).TotalMilliseconds}ms", true);
        }

        private TreeViewItem AddSceneItems(Stage stage)
        {
            TreeViewItem rootNode = null;
            AddLog($"AddSceneItems: {stage.ID}", true);
            rootNode = CreateRootNode($"Scene Items");
            /*
            DateTime start = DateTime.Now;
            AddLog($"Existing method start...", true);
            foreach (var item in stage.SceneItems)
            {
                GetSceneNode(item);
            }

            DateTime multi = DateTime.Now;
            AddLog($"Multicore method start...", true);
            foreach (var item in stage.SceneItems)
            {
                MulticoreSceneNode(item);
            }
            */
            var taskList = new List<Task<MulticoreTreeViewBase>>();

            DateTime tasks = DateTime.Now;
            foreach (var item in stage.SceneItems)
            {
                AddLog($"Creating task for: {item.ID}. # of tasks in list: {taskList.Count}", true);
                taskList.Add(Task.Factory.StartNew(() => MulticoreSceneNode(item)));
            }
            AddLog($"Total of {taskList.Count} tasks created", true);
            Task.WaitAll(taskList.ToArray(), new TimeSpan(0, 1, 0));
            AddLog($"Multicore method (tasks) took {(DateTime.Now - tasks).TotalSeconds} seconds", true);

            foreach (var task in taskList)
            {
                rootNode.Items.Add(GetNodeFromMulticore(task.Result));
            }
            Logger.Logger.AddLog($"AddSceneItems returned null: {rootNode == null}", false);

            return rootNode;
        }

        private TreeViewItem GetNodeFromMulticore(MulticoreTreeViewBase item)
        {
            var node = CreateRootNode(item.Name);
            foreach (var child in item.Children)
            {
                node.Items.Add(GetChildren(child, node));
            }

            return node;
        }

        private TreeViewItem GetChildren(MulticoreTreeViewBase child, TreeViewItem parentNode)
        {
            TreeViewItem node = null;
            if (child.Children.Count > 0) // has children
            {
                node = CreateRootNode(child.Name);
                foreach (var child2 in child.Children)
                {
                    var result = GetChildren(child2, node);
                    node.Items.Add(result);
                }
            }
            else // is final node
            {
                node = CreateRootNode(parentNode.Name);
                if (child.IsCustomObject)
                {
                    AddItemsForCustomObject(node, child.Property, child.ObjectRef);
                }
                else
                {
                    AddItemToTreeView(node, child.Property, child.ObjectRef);
                }
            }

            return node;
        }


        private MulticoreTreeViewBase MulticoreSceneNode(SceneItem item)
        {
            var sceneNode = new MulticoreTreeViewBase($"Scene Item - {item.ID}", AutomaticExpand);
            PropertyInfo[] sceneProperties = typeof(SceneItem).GetProperties();
            for (int i1 = 0; i1 < sceneProperties.Length; i1++)
            {
                var startTime_SceneItem = DateTime.Now;
                PropertyInfo sceneProperty = sceneProperties[i1];
                foreach (var scenePropertyAttribute in
                         sceneProperty.GetCustomAttributes())
                {
                    var startTime_SceneProperty = DateTime.Now;
                    if (!(scenePropertyAttribute is UserEditableAttribute s)) continue;
                    if (sceneProperty.Name == nameof(item.InteractionSettings))
                    {
                        var interaction = item.InteractionSettings;
                        var interactionNode = new MulticoreTreeViewBase($"Interaction", AutomaticExpand);
                        PropertyInfo[] interactionProperties =
                            typeof(InteractionOptions).GetProperties();
                        for (int i2 = 0; i2 < interactionProperties.Length; i2++)
                        {
                            var startTime_InteractionSettings = DateTime.Now;
                            PropertyInfo interactionProperty = interactionProperties[i2];
                            foreach (var interactionPropertyAttribute in interactionProperty
                                         .GetCustomAttributes())
                            {
                                if (!(interactionPropertyAttribute is UserEditableAttribute
                                        att)) continue;
                                if (!att.IsUserEditable) continue;

                                if (interactionProperty.Name == nameof(interaction.Evidence))
                                {
                                    var evidenceDataNode = new MulticoreTreeViewBase($"Evidence", AutomaticExpand);
                                    var evidence = interaction.Evidence;
                                    PropertyInfo[] evidenceProperties = typeof(EvidenceData).GetProperties();
                                    for (int i3 = 0; i3 < evidenceProperties.Length; i3++)
                                    {
                                        PropertyInfo evidenceProperty = evidenceProperties[i3];
                                        foreach (var evidencePropertyAttribute in evidenceProperty.GetCustomAttributes())
                                        {
                                            if (!(evidencePropertyAttribute is UserEditableAttribute _)) continue;

                                            if (evidenceProperty.Name == nameof(evidence.Traces))
                                                evidenceDataNode.AddChild(evidenceProperty, evidence);
                                        }
                                    }

                                    interactionNode.AddChild(evidenceDataNode);
                                }
                                else if (interactionProperty.Name == nameof(interaction.EntityDialog))
                                {
                                    var dialogNode = new MulticoreTreeViewBase($"Dialogue", AutomaticExpand);

                                    dialogNode.AddChild(interactionProperty, interaction.EntityDialog, true);
                                    interactionNode.AddChild(dialogNode);
                                }
                                else if (interactionProperty.Name == nameof(interaction.Report))
                                {
                                    var reportNode = new MulticoreTreeViewBase($"Report", AutomaticExpand);
                                    reportNode.AddChild(interactionProperty, interaction.Report, true);
                                    interactionNode.AddChild(reportNode);
                                }
                                else if (interactionProperty.Name == nameof(interaction.Interrogation))
                                {
                                    interactionNode.AddChild(AddInterrogationItemsMulticore(interaction));

                                }
                                else if (interactionProperty.Name == nameof(interaction.SecondaryPosition))
                                {
                                    interactionNode.AddChild(interactionProperty, interaction, true);
                                }
                                else
                                {
                                    interactionNode.AddChild(interactionProperty, interaction, false);
                                }
                            }
                        }

                        sceneNode.AddChild(interactionNode);
                    }
                    else
                    {
                        sceneNode.AddChild(sceneProperty, item, false);
                    }
                }
            }
            return sceneNode;
        }

        private TreeViewItem GetSceneNode(SceneItem item)
        {
            //AddLog($"Creating scene item: {item.ID}", true);
            var sceneNode = CreateRootNode($"Scene Item - {item.ID}");
            PropertyInfo[] sceneProperties = typeof(SceneItem).GetProperties();
            for (int i1 = 0; i1 < sceneProperties.Length; i1++)
            {
                var startTime_SceneItem = DateTime.Now;
                PropertyInfo sceneProperty = sceneProperties[i1];
                foreach (var scenePropertyAttribute in
                         sceneProperty.GetCustomAttributes())
                {
                    var startTime_SceneProperty = DateTime.Now;
                    if (!(scenePropertyAttribute is UserEditableAttribute s)) continue;
                    if (sceneProperty.Name == nameof(item.InteractionSettings))
                    {
                        var interaction = item.InteractionSettings;
                        var interactionNode = CreateRootNode($"Interaction");
                        PropertyInfo[] interactionProperties =
                            typeof(InteractionOptions).GetProperties();
                        for (int i2 = 0; i2 < interactionProperties.Length; i2++)
                        {
                            var startTime_InteractionSettings = DateTime.Now;
                            PropertyInfo interactionProperty = interactionProperties[i2];
                            foreach (var interactionPropertyAttribute in interactionProperty
                                         .GetCustomAttributes())
                            {
                                if (!(interactionPropertyAttribute is UserEditableAttribute
                                        att)) continue;
                                if (!att.IsUserEditable) continue;

                                if (interactionProperty.Name == nameof(interaction.Evidence))
                                {
                                    var evidenceDataNode = CreateRootNode($"Evidence");
                                    var evidence = interaction.Evidence;
                                    PropertyInfo[] evidenceProperties = typeof(EvidenceData).GetProperties();
                                    for (int i3 = 0; i3 < evidenceProperties.Length; i3++)
                                    {
                                        PropertyInfo evidenceProperty = evidenceProperties[i3];
                                        foreach (var evidencePropertyAttribute in evidenceProperty.GetCustomAttributes())
                                        {
                                            if (!(evidencePropertyAttribute is UserEditableAttribute _)) continue;

                                            if (evidenceProperty.Name == nameof(evidence.Traces))
                                                AddItemToTreeView(evidenceDataNode, evidenceProperty, evidence);
                                        }
                                    }

                                    interactionNode.Items.Add(evidenceDataNode);
                                }
                                else if (interactionProperty.Name == nameof(interaction.EntityDialog))
                                {
                                    var dialogNode = CreateRootNode($"Dialog");
                                    AddItemsForCustomObject(dialogNode, interactionProperty, interaction.EntityDialog);
                                    interactionNode.Items.Add(dialogNode);
                                }
                                else if (interactionProperty.Name == nameof(interaction.Report))
                                {
                                    var reportNode = CreateRootNode($"Report");
                                    AddItemsForCustomObject(reportNode, interactionProperty, interaction.Report);
                                    interactionNode.Items.Add(reportNode);
                                }
                                else if (interactionProperty.Name == nameof(interaction.Interrogation))
                                {
                                    interactionNode.Items.Add(AddInterrogationItems(interaction));

                                }
                                else if (interactionProperty.Name == nameof(interaction.SecondaryPosition))
                                {
                                    AddItemToTreeView(interactionNode, interactionProperty, interaction);
                                }
                                else
                                {
                                    AddItemToTreeView(interactionNode, interactionProperty, interaction);
                                }
                            }
                            AddLog($"Scene Item {item.ID} interaction checked in {(DateTime.Now - startTime_InteractionSettings).TotalMilliseconds}ms", true);
                        }

                        sceneNode.Items.Add(interactionNode);
                        Logger.Logger.AddLog($"sceneNode Items = {sceneNode.Items.Count}", true);
                    }
                    else
                    {
                        AddItemToTreeView(sceneNode, sceneProperty, item);
                    }
                    AddLog($"Scene Item {item.ID} properties checked in {(DateTime.Now - startTime_SceneProperty).TotalMilliseconds}ms", true);
                }
                AddLog($"Scene Item added in {(DateTime.Now - startTime_SceneItem).TotalMilliseconds}ms", true);
            }
            return sceneNode;
        }

        private MulticoreTreeViewBase AddInterrogationItemsMulticore(InteractionOptions interaction)
        {
            var interrogation = interaction.Interrogation;
            var rootNode = new MulticoreTreeViewBase($"Interrogation", AutomaticExpand);
            PropertyInfo[] interrogationProperties = typeof(Interrogation).GetProperties();
            for (int i3 = 0; i3 < interrogationProperties.Length; i3++)
            {
                PropertyInfo interrogationProperty = interrogationProperties[i3];
                foreach (var interrogationPropertyAttribute in interrogationProperty.GetCustomAttributes())
                {
                    if (!(interrogationPropertyAttribute is UserEditableAttribute _)) continue;

                    if (interrogationProperty.Name == nameof(interrogation.ID)) rootNode.AddChild(interrogationProperty, interrogation);
                    else if (interrogationProperty.Name == nameof(interrogation.Questions))
                    {
                        var questions = interrogation.Questions;
                        var questionRootNode = new MulticoreTreeViewBase($"Questions", AutomaticExpand);
                        PropertyInfo[] interrogationNodeProperties = typeof(InterrogationLine).GetProperties();
                        foreach (var line in questions)
                        {
                            var interrogationLineItem = new MulticoreTreeViewBase($"Question - {line.Question}", AutomaticExpand);
                            for (int i4 = 0; i4 < interrogationNodeProperties.Length; i4++)
                            {
                                PropertyInfo interrogationNodeProperty = interrogationNodeProperties[i4];
                                foreach (var interrogationNodePropertyAttribute in interrogationNodeProperty.GetCustomAttributes())
                                {
                                    if (!(interrogationNodePropertyAttribute is UserEditableAttribute _)) continue;

                                    interrogationLineItem.AddChild(interrogationNodeProperty, line);
                                }
                            }

                            questionRootNode.AddChild(interrogationLineItem);
                        }

                        rootNode.AddChild(questionRootNode);
                    }
                }
            }
            return rootNode;
        }

        private TreeViewItem AddInterrogationItems(InteractionOptions interaction)
        {
            var interrogation = interaction.Interrogation;
            var rootNode = CreateRootNode($"Interrogation");
            PropertyInfo[] interrogationProperties = typeof(Interrogation).GetProperties();
            for (int i3 = 0; i3 < interrogationProperties.Length; i3++)
            {
                PropertyInfo interrogationProperty = interrogationProperties[i3];
                foreach (var interrogationPropertyAttribute in interrogationProperty.GetCustomAttributes())
                {
                    if (!(interrogationPropertyAttribute is UserEditableAttribute _)) continue;

                    if (interrogationProperty.Name == nameof(interrogation.ID)) AddItemToTreeView(rootNode, interrogationProperty, interrogation);
                    else if (interrogationProperty.Name == nameof(interrogation.Questions))
                    {
                        var questions = interrogation.Questions;
                        var questionRootNode = CreateRootNode($"Questions");
                        PropertyInfo[] interrogationNodeProperties = typeof(InterrogationLine).GetProperties();
                        foreach (var line in questions)
                        {
                            var interrogationLineItem = CreateRootNode($"Question - {line.Question}");
                            for (int i4 = 0; i4 < interrogationNodeProperties.Length; i4++)
                            {
                                PropertyInfo interrogationNodeProperty = interrogationNodeProperties[i4];
                                foreach (var interrogationNodePropertyAttribute in interrogationNodeProperty.GetCustomAttributes())
                                {
                                    if (!(interrogationNodePropertyAttribute is UserEditableAttribute _)) continue;

                                    AddItemToTreeView(interrogationLineItem, interrogationNodeProperty, line);
                                }
                            }

                            questionRootNode.Items.Add(interrogationLineItem);
                        }

                        rootNode.Items.Add(questionRootNode);
                    }
                }
            }
            return rootNode;
        }


        private void AddItemToTreeView(TreeViewItem node, PropertyInfo property, object reference)
        {
            Logger.Logger.AddLog($"AddItemToTreeView: {node.Header}, {property.Name}, {reference}", true);
            var newNode = CreateNode(property, reference);
            Logger.Logger.AddLog($"Node created: {newNode.Header}", true);
            node.Items.Add(newNode);
            AllNodes.Add(newNode);
        }

        private void AddItemsForCustomObject(TreeViewItem node, PropertyInfo property, object reference)
        {
            foreach (var item in property.PropertyType.GetProperties())
            {
                var newNode = CreateNode(item, reference);
                node.Items.Add(newNode);
                AllNodes.Add(newNode);
                AllCustomNodes.Add(newNode);
            }
        }

        private CustomTreeViewItem CreateNode(PropertyInfo property, object obj)
        {
            var editable = GetAttributeData.IsEditable(property, out var name, out var desc, out Type type);
            return new CustomTreeViewItem(name, desc, editable, type, ref property, ref obj);
        }

    }
}
