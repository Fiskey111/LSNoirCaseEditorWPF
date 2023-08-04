using System;
using System.Collections.Generic;
using System.Windows.Shapes;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CaseManager.NewData;
using CaseManager.Resources;
using LSNoirCaseEditorWPF.Pages;
using Newtonsoft.Json;
using Xceed.Wpf.Toolkit;
using Color = System.Windows.Media.Color;

namespace LSNoirCaseEditorWPF.Logic.Editor;

    public class InteractiveItems : Button
    {
        private CustomTreeViewItem Item { get; set; }
        private InteractiveMasterItem.BoxType EBoxType { get; set; }
        private UIElement DataBox { get; set; }
        private Button SaveButton { get; set; }
        private Button AddButton { get; set; }
        private Button RemoveButton { get; set; }

        private List<TextBox> SpawnPointBoxes { get; set; } = new List<TextBox>();
        private List<TextBox> RotationBoxes { get; set; } = new List<TextBox>();

        private StackPanel Panel { get; set; }
        private CaseViewer PageRef;

        public InteractiveItems(StackPanel panel, InteractiveMasterItem.BoxType boxType, CustomTreeViewItem item, ref CaseViewer pageRef)
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
            if (EBoxType == InteractiveMasterItem.BoxType.TextBox)
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
            else if (EBoxType == InteractiveMasterItem.BoxType.ComboBoxStringList)
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
            else if (EBoxType == InteractiveMasterItem.BoxType.TimePicker)
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
            else if (EBoxType == InteractiveMasterItem.BoxType.EItemType)
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
            else if (EBoxType == InteractiveMasterItem.BoxType.InterrogationAnswerType)
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
            else if (EBoxType == InteractiveMasterItem.BoxType.Boolean)
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
            else if (EBoxType == InteractiveMasterItem.BoxType.Float)
            {
                DataBox = CreateNumberBox("Value", Convert.ToSingle(Item.Value), out var subPanel);
                Panel.Children.Add(subPanel);
            }
            else if (EBoxType == InteractiveMasterItem.BoxType.Integer)
            {
                DataBox = CreateNumberBox("Value", Convert.ToInt32(Item.Value), out var subPanel);
                Panel.Children.Add(subPanel);
            }
            else if (EBoxType == InteractiveMasterItem.BoxType.DateTime)
            {
                DataBox = new DateTimePicker()
                {
                    Format = DateTimeFormat.ShortDate,
                    Value = Item.DeserializeObject<DateTime>()
                };
                Panel.Children.Add(DataBox);
            }
            else if (EBoxType == InteractiveMasterItem.BoxType.Color)
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
            else if (EBoxType == InteractiveMasterItem.BoxType.SpawnPoint)
            {
                var subPanel = new StackPanel();

                var grid = new Grid()
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch
                };
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(75) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(250) });

                var spawnpoint = Item.DeserializeObject<SpawnPointTemp>();
                if (spawnpoint == null) spawnpoint = SpawnPointTemp.Zero;

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

            if (EBoxType == InteractiveMasterItem.BoxType.TextBox)
            {
                var box = DataBox as TextBox;
                serializedData = JsonConvert.SerializeObject(box.Text);
                unserializedData = box.Text;
                validType = Convert.ChangeType(unserializedData, Item.Type) != null;
            }
            else if (EBoxType == InteractiveMasterItem.BoxType.ComboBoxStringList)
            {
                var box = DataBox as ComboBox;
                List<string> saveData = (from object? item in box.Items select item.ToString()).ToList();
                serializedData = JsonConvert.SerializeObject(saveData);
                unserializedData = saveData;
                validType = Convert.ChangeType(unserializedData, Item.Type) != null;
            }
            else if (EBoxType == InteractiveMasterItem.BoxType.EItemType)
            {
                var box = DataBox as ComboBox;
                Enum.TryParse(typeof(SceneItem.EItemType), box.SelectedValue.ToString(), out var saveData);
                serializedData = JsonConvert.SerializeObject(saveData);
                unserializedData = saveData;
                validType = Convert.ChangeType(unserializedData, Item.Type) != null;
            }
            else if (EBoxType == InteractiveMasterItem.BoxType.InterrogationAnswerType)
            {
                var box = DataBox as ComboBox;
                Enum.TryParse(typeof(InterrogationLine.EResponseType), box.SelectedValue.ToString(), out var saveData);
                serializedData = JsonConvert.SerializeObject(saveData);
                unserializedData = saveData;
                validType = Convert.ChangeType(unserializedData, Item.Type) != null;
            }
            else if (EBoxType == InteractiveMasterItem.BoxType.TimePicker)
            {
                var box = DataBox as TimePicker;
                serializedData = JsonConvert.SerializeObject(box.Value);
                unserializedData = box.TimeInterval;
                validType = Convert.ChangeType(unserializedData, Item.Type) != null;
            }
            else if (EBoxType == InteractiveMasterItem.BoxType.Float)
            {
                var data = GetFloatDataFromBox(DataBox as TextBox);

                serializedData = JsonConvert.SerializeObject(data);
                unserializedData = data;
                validType = Convert.ChangeType(unserializedData, Item.Type) != null;
            }
            else if (EBoxType == InteractiveMasterItem.BoxType.Integer)
            {
                var data = GetIntDataFromBox(DataBox as TextBox);

                serializedData = JsonConvert.SerializeObject(data);
                unserializedData = data;
                validType = Convert.ChangeType(unserializedData, Item.Type) != null;
            }
            else if (EBoxType == InteractiveMasterItem.BoxType.Color)
            {
                var box = DataBox as ColorCanvas;
                var data = ConvertToDrawingColor(box.SelectedColor.Value);

                serializedData = JsonConvert.SerializeObject(data);
                unserializedData = data;
                validType = Convert.ChangeType(unserializedData, Item.Type) != null;
            }
            else if (EBoxType == InteractiveMasterItem.BoxType.DateTime)
            {
                var box = DataBox as DateTimePicker;
                DateTime data = (DateTime) box.Value;

                serializedData = JsonConvert.SerializeObject(data);
                unserializedData = data;
                validType = Convert.ChangeType(unserializedData, Item.Type) != null;
            }
            else if (EBoxType == InteractiveMasterItem.BoxType.SpawnPoint)
            {
                string[] axes = new[] { "x", "y", "z" };
                string[] rotation = new[] { "heading", "roll", "pitch", "yaw" };

                SpawnPointTemp point = new SpawnPointTemp()
                {
                    Position = new Vector3Temp(GetFloatDataFromBox(SpawnPointBoxes[0]), GetFloatDataFromBox(SpawnPointBoxes[1]), GetFloatDataFromBox(SpawnPointBoxes[2])),
                    Heading = GetFloatDataFromBox(RotationBoxes[0]),
                    Rotation = new RotatorTemp(GetFloatDataFromBox(RotationBoxes[1]), GetFloatDataFromBox(RotationBoxes[2]), GetFloatDataFromBox(RotationBoxes[3]))
                };

                serializedData = JsonConvert.SerializeObject(point);
                unserializedData = point;
                validType = Convert.ChangeType(unserializedData, Item.Type) != null;
            }
            else if (EBoxType == InteractiveMasterItem.BoxType.Boolean)
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

    }