using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Converters;
using Gum.Wireframe;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System.Reflection;
using Gum.Forms.DefaultVisuals;

namespace Gumknix
{
    public class AppletGumToolbox : BaseApplet
    {
        public static readonly string DefaultTitle = "Gum Toolbox";
        public static readonly string DefaultIcon = "\uEE71";

        private Menu _menu;

        private StackPanel _stackPanel;
        private ContainerRuntime container;
        private ColoredRectangleRuntime _background;

        private Panel _newControlPanel;
        private ContainerRuntime _newControlContainer;
        private ColoredRectangleRuntime _newControlBackground;
        private ListBox _newControlListBox;
        private FrameworkElement newControlSelected;

        private Panel _workSpacePanel;
        private ColoredRectangleRuntime _workSpaceBackground;
        private ElementItem _workSpaceListRoot;

        private StackPanel _elementsPropertiesStackPanel;

        Splitter splitterLeft;
        Splitter splitterRight;

        private Panel _elementsPanel;
        private ContainerRuntime _elementsContainer;
        private ListBox _elementsListBox;

        private Panel _propertiesPanel;
        private ContainerRuntime _propertiesContainer;
        private ListBox _propertiesListBox;

        private ElementItem _draggingElement;
        private Label _draggingElementDetails;

        public AppletGumToolbox(Gumknix gumknix, object[] args = null) : base(gumknix, args)
        {
            base.Initialize(DefaultTitle, DefaultIcon);

            _menu = new();
            MainStackPanel.Visual.AddChild(_menu.Visual);

            MenuItem menuItemFile = new();
            menuItemFile.Header = "File";
            _menu.Items.Add(menuItemFile);

            MenuItem menuItemNew = new();
            menuItemNew.Header = "New";
            menuItemNew.Visual.Width = 220;
            menuItemNew.Visual.WidthUnits = DimensionUnitType.Absolute;
            menuItemNew.Clicked += (s, e) => NewWorkSpacePanel();
            menuItemFile.Items.Add(menuItemNew);

            MenuItem menuItemClone = new();
            menuItemClone.Header = "Clone Running Applet";
            menuItemClone.Visual.Width = 220;
            menuItemClone.Visual.WidthUnits = DimensionUnitType.Absolute;
            menuItemFile.Items.Add(menuItemClone);

            menuItemFile.Clicked += (s, e) =>
            {
                menuItemClone.Items.Clear();
                for (int i = 0; i < GumknixInstance.RunningApplets.Count; i++)
                {
                    BaseApplet applet = GumknixInstance.RunningApplets[i];
                    MenuItem menuItemCloneApplet = new();
                    menuItemCloneApplet.Header = applet.Title;
                    menuItemCloneApplet.Clicked += (s, e) => CloneRunningApplet(applet);
                    menuItemClone.Items.Add(menuItemCloneApplet);
                }
            };

            MenuItem menuItemView = new();
            menuItemView.Header = "View";
            _menu.Items.Add(menuItemView);

            container = new();
            container.Dock(Dock.Fill);
            container.Anchor(Anchor.TopLeft);
            MainStackPanel.AddChild(container);

            _background = new();
            _background.Color = new Color(32, 32, 32);
            _background.Dock(Dock.Fill);
            _background.Anchor(Anchor.TopLeft);
            container.AddChild(_background);

            _stackPanel = new();
            _stackPanel.Orientation = Orientation.Horizontal;
            _stackPanel.Visual.Anchor(Anchor.TopLeft);
            _stackPanel.Visual.Dock(Dock.Fill);
            _stackPanel.Visual.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
            container.AddChild(_stackPanel);

            _newControlPanel = new();
            _newControlPanel.Dock(Dock.Fill);
            _newControlPanel.Anchor(Anchor.TopLeft);
            _newControlPanel.Visual.Width = 250f;
            _newControlPanel.Visual.WidthUnits = DimensionUnitType.Absolute;
            _stackPanel.AddChild(_newControlPanel);

            _newControlContainer = new();
            _newControlContainer.Dock(Dock.Fill);
            _newControlContainer.Anchor(Anchor.TopLeft);
            _newControlPanel.AddChild(_newControlContainer);

            _newControlBackground = new();
            _newControlBackground.Color = Color.Black;
            _newControlBackground.Dock(Dock.Fill);
            _newControlBackground.Anchor(Anchor.TopLeft);
            _newControlContainer.AddChild(_newControlBackground);

            _newControlListBox = new();
            _newControlListBox.Dock(Dock.Fill);
            _newControlListBox.Anchor(Anchor.TopLeft);
            _newControlContainer.AddChild(_newControlListBox);

            List<Type> controlTypes = [
                typeof(Button),
                typeof(CheckBox),
                typeof(ComboBox),
                typeof(Image),
                typeof(Label),
                typeof(ListBox),
                typeof(ListBoxItem),
                typeof(Menu),
                typeof(MenuItem),
                typeof(Panel),
                typeof(PasswordBox),
                typeof(RadioButton),
                typeof(ScrollBar),
                typeof(ScrollViewer),
                typeof(Slider),
                typeof(Splitter),
                typeof(StackPanel),
                typeof(TextBox),
                typeof(Window)];

            for (int i = 0; i < controlTypes.Count; i++)
            {
                Type type = controlTypes[i];
                ListBoxItem listItem = new();
                (listItem.Visual.GetGraphicalUiElementByName("TextInstance") as TextRuntime).Text = type.Name;
                listItem.Visual.Push += (s, e) =>
                {
                    object?[]? args = null;
                    args = type switch
                    {
                        Type t when t == typeof(RadioButton) => [""],
                        _ => null
                    };
                    newControlSelected = Activator.CreateInstance(type, args) as FrameworkElement;
                    if (newControlSelected != null)
                        _workSpacePanel?.AddChild(newControlSelected);
                };
                _newControlListBox.AddChild(listItem);
            }

            splitterLeft = new();
            splitterLeft.Dock(Dock.FillVertically);
            splitterLeft.Width = 5;
            _stackPanel.AddChild(splitterLeft);

            NewWorkSpacePanel();

            splitterRight = new();
            splitterRight.Dock(Dock.FillVertically);
            splitterRight.X = 20;
            splitterRight.Width = 5;
            _stackPanel.AddChild(splitterRight);

            _elementsPropertiesStackPanel = new();
            _elementsPropertiesStackPanel.Orientation = Orientation.Vertical;
            _elementsPropertiesStackPanel.Dock(Dock.FillVertically);
            _elementsPropertiesStackPanel.Anchor(Anchor.TopLeft);
            _elementsPropertiesStackPanel.Visual.Width = 250f;
            _elementsPropertiesStackPanel.Visual.WidthUnits = DimensionUnitType.Absolute;
            _stackPanel.AddChild(_elementsPropertiesStackPanel);

            _elementsPanel = new();
            _elementsPanel.Dock(Dock.Fill);
            _elementsPanel.Anchor(Anchor.TopLeft);
            _elementsPanel.Visual.Height = 50;
            _elementsPanel.Visual.HeightUnits = DimensionUnitType.PercentageOfParent;
            _elementsPropertiesStackPanel.AddChild(_elementsPanel);

            _elementsContainer = new();
            _elementsContainer.Dock(Dock.Fill);
            _elementsContainer.Anchor(Anchor.TopLeft);
            _elementsPanel.AddChild(_elementsContainer);

            _elementsListBox = new();
            _elementsListBox.Dock(Dock.Fill);
            _elementsListBox.Anchor(Anchor.TopLeft);
            _elementsListBox.ListBoxItemFormsType = typeof(ElementListBoxItem);
            _elementsListBox.ItemClicked += (s, e) => SetPropertiesListToElement();
            _elementsListBox.ItemPushed += (s, e) =>
            {
                if (newControlSelected != null)
                    return;

                ElementListBoxItem elementListBoxItem = s as ElementListBoxItem;
                _draggingElement = elementListBoxItem.ElementItem;

                _draggingElementDetails = new();
                _draggingElementDetails.Text = "";
                _draggingElementDetails.Visual.XOrigin = HorizontalAlignment.Right;
                _draggingElementDetails.Visual.YOrigin = VerticalAlignment.Top;
                _draggingElementDetails.Visual.Visible = false;
                GumknixInstance.TooltipLayer.Add(_draggingElementDetails.Visual);
            };
            _elementsContainer.AddChild(_elementsListBox);

            //root = new() { Name = "ElementsListBoxRootProxy" };
            //ElementItem elementItemRoot = new(root);
            //_elementsListBox.Items.Add(elementItemRoot);

            Splitter splitter3 = new();
            splitter3.Dock(Dock.FillHorizontally);
            splitter3.Height = 5;
            _elementsPropertiesStackPanel.AddChild(splitter3);

            _propertiesPanel = new();
            _propertiesPanel.Dock(Dock.Fill);
            _propertiesPanel.Anchor(Anchor.TopLeft);
            _propertiesPanel.Visual.Width = 0;
            _propertiesPanel.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
            _propertiesPanel.Visual.Height = 50;
            _propertiesPanel.Visual.HeightUnits = DimensionUnitType.PercentageOfParent;
            _elementsPropertiesStackPanel.AddChild(_propertiesPanel);

            _propertiesContainer = new();
            _propertiesContainer.Dock(Dock.Fill);
            _propertiesContainer.Anchor(Anchor.TopLeft);
            _propertiesPanel.AddChild(_propertiesContainer);

            _propertiesListBox = new();
            _propertiesListBox.Dock(Dock.Fill);
            _propertiesListBox.Anchor(Anchor.TopLeft);
            _propertiesListBox.ListBoxItemFormsType = typeof(PropertiesListBoxItem);
            _propertiesContainer.AddChild(_propertiesListBox);

            _elementsListBox.Items.Clear();
            _workSpaceListRoot = new(_workSpacePanel, _workSpacePanel);
            _elementsListBox.Items.Add(_workSpaceListRoot);
        }

        public override void Update()
        {
            if ((newControlSelected != null) && (_draggingElement == null))
            {
                newControlSelected.X = GumService.Default.Cursor.X - _workSpacePanel.AbsoluteLeft;
                newControlSelected.Y = GumService.Default.Cursor.Y - _workSpacePanel.AbsoluteTop;

                if (GumService.Default.Cursor.PrimaryDown == false)
                {
                    if ((GumService.Default.Cursor.X >= _workSpacePanel.AbsoluteLeft) &&
                        (GumService.Default.Cursor.X <= (_workSpacePanel.AbsoluteLeft + _workSpacePanel.Visual.GetAbsoluteWidth())) &&
                        (GumService.Default.Cursor.Y >= _workSpacePanel.AbsoluteTop) &&
                        (GumService.Default.Cursor.Y <= (_workSpacePanel.AbsoluteTop + _workSpacePanel.Visual.GetAbsoluteHeight())))
                    {
                        ElementItem element = new(newControlSelected, _workSpacePanel);
                        _elementsListBox.Items.Add(element);
                        _elementsListBox.SelectedObject = element;
                        SetPropertiesListToElement();
                    }
                    else
                    {
                        newControlSelected.RemoveFromRoot();
                    }

                    newControlSelected = null;
                }
            }

            if ((_draggingElement != null) && (newControlSelected == null))
            {
                _draggingElementDetails.X = GumService.Default.Cursor.X + 10;
                _draggingElementDetails.Y = GumService.Default.Cursor.Y + 10;

                ElementListBoxItem elementUnderCursor = null;
                bool elementUnderCursorIsChild = false;
                for (int i = 0; i < _elementsListBox.ListBoxItems.Count; i++)
                {
                    ElementListBoxItem itemToCheck = _elementsListBox.ListBoxItems[i] as ElementListBoxItem;
                    if (itemToCheck.Visual.HasCursorOver(GumService.Default.Cursor))
                    {
                        if (itemToCheck.ElementItem == _draggingElement)
                            break;

                        elementUnderCursor = itemToCheck;
                        FrameworkElement root = elementUnderCursor.ElementItem.FrameworkElement.ParentFrameworkElement;
                        while (root != null && root != _draggingElement.FrameworkElement)
                            root = root.ParentFrameworkElement;
                        elementUnderCursorIsChild = root != null;

                        if (GumService.Default.Cursor.PrimaryDown == false)
                        {
                            if (!elementUnderCursorIsChild)
                            {
                                float x = _draggingElement.FrameworkElement.AbsoluteLeft;
                                float y = _draggingElement.FrameworkElement.AbsoluteTop;
                                _draggingElement.FrameworkElement.RemoveFromRoot();
                                elementUnderCursor.ElementItem.FrameworkElement.AddChild(_draggingElement.FrameworkElement);
                                _draggingElement.FrameworkElement.X = x - elementUnderCursor.ElementItem.FrameworkElement.AbsoluteLeft;
                                _draggingElement.FrameworkElement.Y = y - elementUnderCursor.ElementItem.FrameworkElement.AbsoluteTop;

                                _elementsListBox.Items.Remove(_draggingElement);
                                int indexOfNewParent = _elementsListBox.ListBoxItems.IndexOf(elementUnderCursor);
                                _elementsListBox.Items.Insert(indexOfNewParent + 1, _draggingElement);
                            }

                            GumknixInstance.TooltipLayer.Remove(_draggingElementDetails.Visual);
                            _draggingElement = null;
                            _draggingElementDetails = null;
                        }

                        break;
                    }
                }

                if (_draggingElementDetails != null)
                {
                    if ((elementUnderCursor != null) && !elementUnderCursorIsChild)
                        _draggingElementDetails.Text = $"Move to \"{elementUnderCursor.ElementItem.FrameworkElement.GetType().Name}\"";
                    _draggingElementDetails.IsVisible = elementUnderCursor != null;
                }
            }

            base.Update();
        }

        public void NewWorkSpacePanel()
        {
            _workSpacePanel?.RemoveFromRoot();
            splitterRight?.RemoveFromRoot();
            _elementsPropertiesStackPanel?.RemoveFromRoot();

            _workSpacePanel = new() { Name = "WorkSpacePanelRootProxy" };
            _workSpacePanel.Anchor(Anchor.TopLeft);
            _workSpacePanel.Dock(Dock.Fill);
            _workSpacePanel.X = 20f;
            _workSpacePanel.Visual.XUnits = GeneralUnitType.PixelsFromSmall;
            _workSpacePanel.Visual.XOrigin = HorizontalAlignment.Left;
            _workSpacePanel.Y = 20f;
            _workSpacePanel.Visual.YUnits = GeneralUnitType.PixelsFromSmall;
            _workSpacePanel.Visual.YOrigin = VerticalAlignment.Top;
            _workSpacePanel.Width = -550f;
            _workSpacePanel.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
            _workSpacePanel.Height = -40f;
            _workSpacePanel.Visual.HeightUnits = DimensionUnitType.RelativeToParent;
            _workSpacePanel.Visual.ClipsChildren = true;
            _stackPanel.AddChild(_workSpacePanel);

            _workSpaceBackground = new();
            _workSpaceBackground.Anchor(Anchor.TopLeft);
            _workSpaceBackground.Dock(Dock.Fill);
            _workSpaceBackground.Color = Color.Black;
            _workSpacePanel.AddChild(_workSpaceBackground);

            if (_elementsListBox == null)
                return;

            _elementsListBox.Items.Clear();
            _workSpaceListRoot = new(_workSpacePanel, _workSpacePanel);
            _elementsListBox.Items.Add(_workSpaceListRoot);

            _stackPanel.AddChild(splitterRight);
            _stackPanel.AddChild(_elementsPropertiesStackPanel);
        }

        public void CloneRunningApplet(BaseApplet applet)
        {
            NewWorkSpacePanel();

            cloneElementItems = [];

            FrameworkElement clone = CloneFrameworkElement(applet.Window);
            _workSpacePanel?.AddChild(clone);

            for (int i = 0; i < cloneElementItems.Count; i++)
                _elementsListBox.Items.Add(cloneElementItems[i]);
        }

        List<ElementItem> cloneElementItems = [];

        public FrameworkElement CloneFrameworkElement(FrameworkElement original)
        {
            FrameworkElement clone = original switch
            {
                Gum.Forms.Window => new Window(),
                StackPanel => new StackPanel(),
                Panel => new Panel(),
                Button => new Button(),
                CheckBox => new CheckBox(),
                ComboBox => new ComboBox(),
                Image => new Image(),
                Label => new Label(),
                ListBox => new ListBox(),
                ListBoxItem => new ListBoxItem(),
                Menu => new Menu(),
                MenuItem => new MenuItem(),
                PasswordBox => new PasswordBox(),
                RadioButton => new RadioButton(),
                ScrollBar => new ScrollBar(),
                ScrollViewer => new ScrollViewer(),
                Slider => new Slider(),
                Splitter => new Splitter(),
                TextBox => new TextBox(),
                _ => throw new()
            };

            clone.Name = original.Name;
            clone.IsVisible = original.IsVisible;
            clone.X = original.X;
            clone.Visual.XOrigin = original.Visual.XOrigin;
            clone.Visual.XUnits = original.Visual.XUnits;
            clone.Y = original.Y;
            clone.Visual.YOrigin = original.Visual.YOrigin;
            clone.Visual.YUnits = original.Visual.YUnits;
            clone.Width = original.Width;
            clone.Visual.WidthUnits = original.Visual.WidthUnits;
            clone.Height = original.Height;
            clone.Visual.HeightUnits = original.Visual.HeightUnits;
            clone.Visual.ClipsChildren = original.Visual.ClipsChildren;

            Type elementType = original.GetType();

            string text = elementType.GetProperty("Text")?.GetValue(original) as string;
            if (text != null)
                elementType.GetProperty("Text")?.SetValue(clone, text);

            ResizeMode? resizeMode = elementType.GetProperty("ResizeMode")?.GetValue(original) as ResizeMode?;
            if (resizeMode != null)
                elementType.GetProperty("ResizeMode")?.SetValue(clone, resizeMode);

            Orientation? orientation = elementType.GetProperty("Orientation")?.GetValue(original) as Orientation?;
            if (orientation != null)
                elementType.GetProperty("Orientation")?.SetValue(clone, orientation);

            ElementItem element = new(clone, _workSpacePanel);
            cloneElementItems.Add(element);

            IRenderableIpso originalVisual = original.Visual;
            for (int i = 0; i < originalVisual.Children.Count; i++)
            {
                IRenderableIpso renderableIpso = originalVisual.Children[i];
                InteractiveGue interactiveGue = renderableIpso as InteractiveGue;
                if (interactiveGue != null)
                {
                    FrameworkElement frameworkElement = interactiveGue.FormsControlAsObject as FrameworkElement;
                    if (frameworkElement != null)
                    {
                        FrameworkElement clonedChild = CloneFrameworkElement(frameworkElement);
                        clone.AddChild(clonedChild);
                        continue;
                    }
                }

                GraphicalUiElement gue = renderableIpso as GraphicalUiElement;
                if (gue != null)
                {
                    GraphicalUiElement clonedChild = gue.Clone();
                    clone.AddChild(clonedChild);
                }
            }

            return clone;
        }

        public void SetPropertiesListToElement()
        {
            _propertiesListBox.Items.Clear();

            ElementItem element = _elementsListBox.SelectedObject as ElementItem;
            if (element == null)
                return;

            List<string> propertyNames = [
                "HorizontalAlignment",
                "VerticalAlignment",
                "TextOverflowHorizontalMode",
                "TextOverflowVerticalMode",
                "Name",
                "IsVisible",
                "Text",
                "X",
                "Y",
                "Width",
                "Height",
                "XOrigin",
                "XUnits",
                "YOrigin",
                "YUnits",
                "WidthUnits",
                "HeightUnits",
                "Orientation",
                "ClipsChildren",
                "ResizeMode"];

            foreach (string propertyName in propertyNames)
            {
                PropertiesListBoxItem item = PropertiesListBoxItem.Create(element, propertyName);
                if (item != null)
                    _propertiesListBox.Items.Add(item);
            }
        }

        protected override void Close()
        {

            base.Close();
        }
    }

    public class ElementItem
    {
        public FrameworkElement FrameworkElement { get; set; }

        public FrameworkElement RootElement { get; set; }

        public ElementListBoxItem ElementListBoxItem { get; set; }

        public ElementItem(FrameworkElement frameworkElement, FrameworkElement rootElement)
        {
            FrameworkElement = frameworkElement;
            RootElement = rootElement;
        }
    }

    public class ElementListBoxItem : ListBoxItem
    {
        public ElementItem ElementItem { get; set; }

        public ElementListBoxItem(InteractiveGue gue) : base(gue)
        {
            //TextRuntime text = Visual.GetGraphicalUiElementByName("TextInstance") as TextRuntime;
        }

        public override void UpdateToObject(object obj)
        {
            ElementItem = obj as ElementItem;
            ElementItem.ElementListBoxItem = this;
            FrameworkElement frameworkElement = ElementItem.FrameworkElement;

            int levelToRoot = 0;
            FrameworkElement elementToCheck = frameworkElement;
            while (elementToCheck != ElementItem.RootElement)
            {
                levelToRoot++;
                elementToCheck = elementToCheck.ParentFrameworkElement;
            }

            string indent = new(' ', levelToRoot * 2);
            if (levelToRoot >= 1)
                coreText.RawText = $"{indent}{frameworkElement.GetType().Name} ({frameworkElement.Name ?? "null"})";
            else
                coreText.RawText = "Root";
        }
    }

    public class PropertiesListBoxItem : ListBoxItem
    {
        public ElementItem ElementItem { get; set; }

        public static PropertiesListBoxItem Create(ElementItem elementItem, string propertyName)
        {
            object elementObject = elementItem.FrameworkElement;
            PropertyInfo propertyInfo = elementObject.GetType().GetProperty(propertyName);
            if (propertyInfo == null)
            {
                elementObject = elementItem.FrameworkElement.Visual;
                propertyInfo = elementObject.GetType().GetProperty(propertyName);
            }
            if (propertyInfo == null)
            {
                elementObject = (elementItem.FrameworkElement.Visual as TextBoxVisual).TextInstance;
                propertyInfo = elementObject?.GetType().GetProperty(propertyName);
            }
            if (propertyInfo == null)
                return null;

            PropertiesListBoxItem listBoxItem = new();

            listBoxItem.ElementItem = elementItem;
            listBoxItem.coreText.RawText = propertyName;

            ContainerRuntime container = new();
            container.Dock(Gum.Wireframe.Dock.FillHorizontally);
            container.Height = 35;
            container.HeightUnits = DimensionUnitType.Absolute;
            listBoxItem.Visual.AddChild(container);

            object currentValue = null;
            if (propertyInfo?.CanRead == true)
                currentValue = propertyInfo.GetValue(elementObject);

            if (propertyInfo.PropertyType == typeof(bool))
            {
                CheckBox checkBox = new() { Text = "" };
                checkBox.Visual.Anchor(Gum.Wireframe.Anchor.Right);
                checkBox.Visual.X = -10;
                checkBox.Visual.Width = 50;
                checkBox.Visual.WidthUnits = DimensionUnitType.PercentageOfParent;
                checkBox.IsChecked = (bool?)currentValue;
                checkBox.Checked += (s, e) =>
                {
                    if (propertyInfo?.CanWrite == true)
                        propertyInfo.SetValue(elementObject, checkBox.IsChecked);
                };
                container.AddChild(checkBox);
                return listBoxItem;
            }

            if (propertyInfo.PropertyType.IsEnum)
            {
                ComboBox comboBox = new() { Text = "" };
                comboBox.Visual.Anchor(Gum.Wireframe.Anchor.Right);
                comboBox.Visual.X = -10;
                comboBox.Visual.Width = 50;
                comboBox.Visual.WidthUnits = DimensionUnitType.PercentageOfParent;
                string[] enumNames = propertyInfo.PropertyType.GetEnumNames();
                Array enumValues = propertyInfo.PropertyType.GetEnumValues();
                comboBox.Items = enumNames;
                comboBox.SelectedIndex = (int)currentValue;
                comboBox.SelectionChanged += (s, e) =>
                {
                    if (propertyInfo?.CanWrite == true)
                    {
                        object convertedValue = Convert.ChangeType(enumValues.GetValue(comboBox.SelectedIndex), propertyInfo.PropertyType);
                        propertyInfo.SetValue(elementObject, convertedValue);
                        elementItem.FrameworkElement.Visual.UpdateLayout();
                    }
                };
                container.AddChild(comboBox);
                return listBoxItem;
            }

            TextBox value = new();
            value.Text = currentValue?.ToString() ?? "null";
            value.Visual.Anchor(Gum.Wireframe.Anchor.Right);
            value.Visual.X = -10;
            value.Visual.Width = 50;
            value.Visual.WidthUnits = DimensionUnitType.PercentageOfParent;

            value.TextChanged += (s, e) =>
            {
                if ((propertyInfo?.CanWrite == true) && (string.IsNullOrEmpty(value.Text) == false))
                {
                    try
                    {
                        object convertedValue = Convert.ChangeType(value.Text, propertyInfo.PropertyType);
                        propertyInfo.SetValue(elementObject, convertedValue);
                    }
                    catch (Exception convertException)
                    {
                    }
                }
            };

            container.AddChild(value);

            return listBoxItem;
        }
    }
}
