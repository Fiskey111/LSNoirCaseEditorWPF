using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LSNoirCaseEditorWPF.Objects
{
    internal class MulticoreTreeViewBase
    {
        internal string Name { get; set; }
        internal bool IsExpanded { get; set; }
        internal PropertyInfo Property { get; set; }
        internal object ObjectRef { get; set; }
        internal bool IsCustomObject { get; set; }
        internal List<MulticoreTreeViewBase> Children { get; set; } = new List<MulticoreTreeViewBase>();

        internal MulticoreTreeViewBase() { }

        internal MulticoreTreeViewBase(string name, bool isExpanded)
        {
            Name = name;
            IsExpanded = isExpanded;
        }

        internal void AddChild(PropertyInfo info, object objRef, bool isCustom = false)
        {
            Children.Add(new MulticoreTreeViewBase() { Property = info, ObjectRef = objRef, IsCustomObject = isCustom });          
        }

        internal void AddChild(MulticoreTreeViewBase child)
        {
            Children.Add(child);
        }
    }
}
