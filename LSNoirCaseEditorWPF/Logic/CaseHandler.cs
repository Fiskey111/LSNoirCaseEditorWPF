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
using LSNoirCaseEditorWPF.Windows;
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
            
            StageLoader stageLoader = new StageLoader();
            RootNode = await stageLoader.LoadCase(CurrentCase);
            CaseLoadedFireEvent(RootNode);
        }

        internal TreeViewItem RootNode { get; private set; }

    }
}
