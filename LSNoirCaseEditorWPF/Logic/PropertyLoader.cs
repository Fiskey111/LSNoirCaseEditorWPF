using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CaseManager.NewData;
using CaseManager.Resources;
using LSNoirCaseEditorWPF.Objects;

namespace LSNoirCaseEditorWPF.Logic;

    internal class PropertyLoader
    {
        public PropertyLoader()
        {
        }

        internal async Task<TreeViewItem> LoadPropertyInfo(PropertyInfo property, object obj)
        {
            if (property.GetCustomAttribute(typeof(UserEditableAttribute)) is not UserEditableAttribute resultingProperty) return null;

            var type = resultingProperty.ItemType;
            if (type == null) return null;
                    
            var isSimple = IsSimpleType(type) || type?.IsEnum == true;
            
            var isList = resultingProperty?.ItemType?.IsGenericType == true
                         && resultingProperty.ItemType.GetGenericTypeDefinition() == typeof(List<>);
                    
            if (isSimple)
            {
                var simpleNode = CreateNodeForSimpleItem(property, obj);
                return simpleNode;
            }
            
            var rootNode = CreateRootNode($"{resultingProperty.ItemName}", null);
            if (isList) // The children need to have the ability to be deleted - sometimes
            {
                var itemAsList = (IEnumerable)Convert.ChangeType(property.GetValue(obj), type);
                foreach (var p in itemAsList)
                {
                    if (resultingProperty?.IsMaster == true)
                    {
                        var listType = itemAsList.GetType().GetGenericArguments().Single();
                        var customAttributeRef = listType.GetCustomAttribute<UserEditableAttribute>();
                        if (customAttributeRef != null)
                        {
                            var simpleNode = CreateNodeForMaster(customAttributeRef, p);
                            rootNode.Items.Add(simpleNode);
                            continue;
                        }
                    }
                    
                    var newNode = GetTreeviewItem_Properties(itemAsList.GetType().GetGenericArguments().Single(), property, p);
                    if (newNode == null)
                    {
                        continue;
                    }
                                
                    rootNode.Items.Add(newNode);
                }
                return rootNode;
            }
            else
            {
                var newNode = GetTreeviewItem_Properties(type, property, property.GetValue(obj));
                if (newNode == null)
                {
                    return null;
                }
                rootNode.Items.Add(newNode);
            }
            return rootNode;
        }
        
        
        private TreeViewItem GetTreeviewItem_Properties(Type type, PropertyInfo rootProperty, object obj)
        {
            string nullref = null;
            TreeViewItem returnedNode = null;
            try
            {
                if (type == null || rootProperty == null || obj == null)
                {
                    return returnedNode;
                }
                var start = DateTime.Now;
                var attribute = rootProperty.GetCustomAttribute(typeof(UserEditableAttribute)) as UserEditableAttribute;
                var id = string.Empty;
                if (obj is IDataBase @base) id = @base.ID;
                
                returnedNode = CreateRootNode($"{attribute.ItemName} - {id}", obj, attribute, true);
                var rootProperties = type.GetProperties();
                foreach (var property in rootProperties)
                {
                        if (property.GetCustomAttribute(typeof(UserEditableAttribute)) is not UserEditableAttribute
                            resultingProperty) continue;

                        var isSimple = IsSimpleType(resultingProperty.ItemType) ||
                                       resultingProperty.ItemType?.IsEnum == true;

                        var isList = resultingProperty?.ItemType?.IsGenericType == true
                                     && resultingProperty.ItemType.GetGenericTypeDefinition() == typeof(List<>);

                        if (isSimple)
                        {
                            var simpleNode = CreateNodeForSimpleItem(property, obj);
                            returnedNode.Items.Add(simpleNode);
                        }
                        else
                        {
                            var rootNode = CreateRootNode($"{resultingProperty.ItemName}", nullref);
                            if (isList)
                            {
                                var listType = resultingProperty.ItemType;
                                var itemAsList = (IEnumerable)Convert.ChangeType(property.GetValue(obj), listType);
                                foreach (var p in itemAsList)
                                {
                                    var newNode =
                                        GetTreeviewItem_Properties(itemAsList.GetType().GetGenericArguments().Single(),
                                            property, p);
                                    if (newNode == null)
                                    {
                                        continue;
                                    }

                                    rootNode.Items.Add(newNode);
                                }
                            }
                            else
                            {
                                var newNode = GetTreeviewItem_Properties(resultingProperty.ItemType, property,
                                    property.GetValue(obj));
                                if (newNode == null)
                                {
                                    continue;
                                }

                                rootNode.Items.Add(newNode);
                            }

                            returnedNode.Items.Add(rootNode);
                        }
                    }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }

            return returnedNode;
        }

        private readonly List<Type> simpleTypes = new()
        {
            typeof(bool), typeof(int), typeof(string), typeof(float), typeof(double), 
            typeof(List<string>), typeof(List<bool>), typeof(List<int>), typeof(List<float>), typeof(List<double>),
            typeof(SpawnPointTemp), typeof(TimeSpan), typeof(DateTime)
        };

        private bool IsSimpleType(Type type)
        {
            return simpleTypes.Any(t => t == type);
        }

        private CustomTreeViewItem CreateNodeForSimpleItem(PropertyInfo property, object obj)
        {
            var editable = GetAttributeData.IsEditable(property, out var name, out var desc, out Type type);
            return new CustomTreeViewItem(name, desc, editable, type, ref property, ref obj);
        }
        
        private MasterTreeViewItem CreateNodeForMaster(UserEditableAttribute attribute, object obj)
        {
            var dataType = obj as IDataBase;
            if (dataType.ID == string.Empty) dataType.ID = "None";
            return new MasterTreeViewItem(dataType.ID, attribute.ItemDescription, attribute.IsUserEditable, attribute.ItemType, ref obj);
        }
        
        private TreeViewItem CreateRootNode(string text, object obj, UserEditableAttribute property = null, bool deletable = false)
        {
            if (deletable)
            {
                DeletableObjectTreeviewItem node = null;
                node = new DeletableObjectTreeviewItem(obj, property)
                {
                    Header = text,
                    IsExpanded = true
                };
                return node;
            }
            else
            {
                TreeViewItem node = null;
                node = new TreeViewItem
                {
                    Header = text,
                    IsExpanded = true
                };
                return node;
            }
        }

        private void ShowError(Exception exception, bool displayMessage = true)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                if (displayMessage) MessageBox.Show(exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.Logger.AddLog($"!! ERROR !! {exception.Message}", false);
                Logger.Logger.AddLog($"!! ERROR !! {exception.StackTrace}", false);
            });
        }
    }