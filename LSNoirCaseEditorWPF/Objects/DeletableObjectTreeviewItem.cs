using System;
using System.Reflection;
using System.Windows.Controls;

namespace LSNoirCaseEditorWPF.Objects;

public class DeletableObjectTreeviewItem : TreeViewItem
{
    public object Item { get; set; }
    public UserEditableAttribute Property { get; set; }
    
    public DeletableObjectTreeviewItem(object item, UserEditableAttribute prop)
    {
        try
        {
            Item = item;
            Property = prop;
        }
        catch (Exception ex)
        {
            Logger.Logger.AddLog($"{ex}\n{ex.StackTrace}", false);
        }
    }
}