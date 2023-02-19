﻿using System;
using System.Reflection;
using System.Windows.Controls;
using Newtonsoft.Json;

namespace LSNoirCaseEditorWPF.Logic
{
    public class CustomTreeViewItem : TreeViewItem
    {
        public CustomTreeViewItem(string name, string description, bool editable, Type type, ref PropertyInfo property, ref object obj) 
        {
            try
            {
                Name = name;
                Description = description;
                Value = property.GetValue(obj) != null ? JsonConvert.SerializeObject(property.GetValue(obj)) : string.Empty;
                Editable = editable;
                Type = type;
                Header = name;
                PropertyRef = property;
                Object = obj;
                Uid = Guid.NewGuid().ToString();
            }
            catch (Exception ex)
            {
                Logger.Logger.AddLog($"{ex}\n{ex.StackTrace}", false);
            }
        }

        public T DeserializeObject<T>()
        {
            try
            {
                var result = JsonConvert.DeserializeObject<T>(Value);
                if (result == null) result = default(T);
                return result;
            }
            catch (Exception ex)
            {
                Logger.Logger.AddLog(ex.ToString(), true);
                return default(T);
            }
        }

        public void SerializeObject(string data)
        {
            Logger.Logger.AddLog($"{this.Name}.SerializeObject({data})", true);
            Value = data;
        }

        public object Object { get; set; }
        public PropertyInfo PropertyRef { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Value { get; set; }
        public bool Editable { get; set; }
        public Type Type { get; set; }
    }

}
