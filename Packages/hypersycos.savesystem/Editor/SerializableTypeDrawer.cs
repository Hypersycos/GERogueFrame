using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hypersycos.SaveSystem
{
    [CustomPropertyDrawer(typeof(SerializableType<>), true)]
    internal class SerializableTypePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var targetObjectType = DetermineTargetType(fieldInfo.FieldType);
            var field = new SerializableTypeField(targetObjectType, property.displayName);
            var targetProperty = property.FindPropertyRelative("data");
            field.TrackPropertyValue(targetProperty, field.UpdateDisplay);
            field.BindProperty(targetProperty);
            field.UpdateDisplay(targetProperty);
            return field;
        }

        private static Type GetElementType(Type t)
        {
            if (t.IsGenericType)
            {
                return t.GetGenericArguments()[0];
            }
            return t;
        }

        private static Type DetermineTargetType(Type t)
        {
            if (typeof(IEnumerable).IsAssignableFrom(t) && t.IsGenericType)
            {
                return GetElementType(t.GetGenericArguments()[0]);
            }
            if (t.IsArray)
            {
                return GetElementType(t.GetElementType());
            }
            return GetElementType(t);
        }
    }

    internal static class SerializableTypeDataUtility
    {
        private static readonly Dictionary<Type, List<Type>> s_map = new();
        private static readonly Dictionary<Type, List<Type>> s_mapWithNull = new();

        internal static List<Type> GetFilteredTypesAndNull(Type type)
        {
            if (!s_mapWithNull.TryGetValue(type, out var list))
            {
                s_mapWithNull.Add(type, list = new List<Type> { null });
                list.AddRange(GetFilteredTypes(type));
            }
            return list;
        }

        internal static List<Type> GetFilteredTypes(Type type)
        {
            if (!s_map.TryGetValue(type, out var list))
            {
                s_map.Add(type, list = GetFilteredTypesInternal(type));
            }
            return list;
        }

        private static List<Type> GetFilteredTypesInternal(Type type)
        {
            HashSet<Type> types;
            if (type == typeof(object))
            {
                types =
                    (from a in AppDomain.CurrentDomain.GetAssemblies()
                    from t in a.GetTypes()
                    select t).ToHashSet();
            }
            else
            {
                types = new(TypeCache.GetTypesDerivedFrom(type));
                types.Add(type);
            }
            return types.OrderBy(static s => s.FullName, StringComparer.InvariantCulture).ToList();
        }
    }

    internal partial class SerializableTypeField : BaseField<string>
    {
        private readonly Type _filterType;
        private readonly VisualElement _content;
        private readonly Label _label;
        private readonly VisualElement _warningBox;
        private readonly Button _selectButton;
        private readonly TypeSelectorPopupWindowContent _typeSelectorPopupWindowContent;
        private readonly List<Type> _filteredTypes;

        public SerializableTypeField(Type filterType) : this(filterType, null)
        {
        }

        public SerializableTypeField(Type filterType, string label) : this(filterType, label, new VisualElement())
        {
        }

        public SerializableTypeField(Type filterType, string label, VisualElement visualInput) : base(label, visualInput)
        {
            _filterType = filterType;
            AddToClassList(alignedFieldUssClassName);
            _content = visualInput;
            _content.AddToClassList(ObjectField.inputUssClassName);
            _content.style.flexDirection = FlexDirection.Row;
            _content.Add(_label = new Label());
            _content.Add(_warningBox = new VisualElement
            {
                style = {
                width = 16,
                height = 16,
                backgroundImage = new StyleBackground(EditorGUIUtility.IconContent("warning").image as Texture2D),
                backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center),
                backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center),
                backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat),
                backgroundSize = new BackgroundSize(BackgroundSizeType.Contain) },
                tooltip = $"The currently assigned type is not compatible with the property's type restriction ({_filterType}). The type will be considered null when read."
            });
            _warningBox.style.display = DisplayStyle.None;
            _content.Add(_selectButton = new Button(OpenSelector)
            {
                style =
                {
                    marginBottom = 0,
                    marginLeft = 0,
                    marginRight = 0,
                    marginTop = 0,
                    borderBottomWidth = 0,
                    borderLeftWidth = 0,
                    borderRightWidth = 0,
                    borderTopWidth = 0,
                    paddingBottom = 0,
                    paddingLeft = 0,
                    paddingRight = 0,
                    paddingTop = 0
                },
            });
            _label.AddToClassList("unity-object-field-display__label");
            _selectButton.AddToClassList(ObjectField.selectorUssClassName);
            _typeSelectorPopupWindowContent = new TypeSelectorPopupWindowContent(this);
            _filteredTypes = SerializableTypeDataUtility.GetFilteredTypesAndNull(filterType);
        }

        protected override void UpdateMixedValueContent()
        {
            UpdateDisplay("\u2014");
        }

        internal void UpdateDisplay(SerializedProperty serializedProperty)
        {
            UpdateDisplay(SerializableTypeUtility.ReadFromString(serializedProperty.stringValue));
        }

        private void UpdateDisplay(Type type)
        {
            if (type != null)
            {
                _warningBox.style.display = _filterType.IsAssignableFrom(type) ? DisplayStyle.None : DisplayStyle.Flex;
                UpdateDisplay(GetFormattedType(type));
            }
            else
            {
                _warningBox.style.display = DisplayStyle.None;
                UpdateDisplay("Not Set");
            }

        }
        private void UpdateDisplay(string text)
        {
            _label.text = text;
        }

        internal static string GetFormattedType(Type t)
        {
            return t != null ? $"{t.Name} ({t.FullName})" : "None";
        }

        public List<Type> GetFilteredTypes()
        {
            return _filteredTypes;
        }

        private void OpenSelector()
        {
            UnityEditor.PopupWindow.Show(worldBound, _typeSelectorPopupWindowContent);
        }

        private class TypeSelectorPopupWindowContent : PopupWindowContent
        {
            private const int itemHeight = 18;
            private readonly SerializableTypeField _field;
            private readonly Toolbar _toolbar;
            private readonly ToolbarSearchField _search;
            private readonly ListView _listView;
            private List<Type> _types;
            private bool _pauseSelectionCheck;

            internal TypeSelectorPopupWindowContent(SerializableTypeField field)
            {
                _field = field;
                _listView = new ListView
                {
                    reorderable = false,
                    selectionType = SelectionType.Single,
                    showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                    virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                    fixedItemHeight = itemHeight
                };
                _listView.makeItem = () => new Label();
                _listView.bindItem = (v, i) =>
                {
                    Type t = ((IReadOnlyList<Type>)_listView.itemsSource)[i];
                    ((Label)v).text = GetFormattedType(t);
                };
                _listView.selectionChanged += (s) =>
                {
                    SetFromSelection(s);
                };
                _listView.itemsChosen += (s) =>
                {
                    SetFromSelection(s);
                    editorWindow.Close();
                };
                _toolbar = new Toolbar();
                _search = new ToolbarSearchField();
                var searchState = new DelayedSearchController(_search);
                searchState.SearchTextChanged += s => { SetFilter(s); };
                _toolbar.Add(_search);
            }

            private void SetFromSelection(IEnumerable<object> selection)
            {
                if (_pauseSelectionCheck)
                {
                    return;
                }
                Type type = selection.FirstOrDefault() as Type;
                _field.value = SerializableTypeUtility.WriteToString(type);
            }

            private void SetList(List<Type> types)
            {
                _listView.itemsSource = types;
                int index = types.IndexOf(SerializableTypeUtility.ReadFromString(_field.value));
                if (index >= 0)
                {
                    _listView.SetSelection(index);
                }
                else
                {
                    _listView.ClearSelection();
                }
            }

            private void SetFilter(string filter)
            {
                _pauseSelectionCheck = true;
                if (string.IsNullOrEmpty(filter))
                {
                    SetList(_types);
                }
                else
                {
                    List<Type> filtered = new();
                    foreach (var type in _types)
                    {
                        if (type is not null && type.FullName is not null && type.FullName.Contains(filter, StringComparison.InvariantCultureIgnoreCase))
                        {
                            filtered.Add(type);
                        }
                    }
                    SetList(filtered);
                }
                _pauseSelectionCheck = false;
            }

            public override Vector2 GetWindowSize()
            {
                float width = Math.Max(200, _field.resolvedStyle.width);
                return new Vector2(width, 500);
            }

            public override void OnOpen()
            {
                editorWindow.rootVisualElement.Clear();
                editorWindow.rootVisualElement.Add(_toolbar);
                editorWindow.rootVisualElement.Add(_listView);
                _types = _field.GetFilteredTypes();
                SetList(_types);
                editorWindow.rootVisualElement.RegisterCallback<NavigationCancelEvent>(HandleNavigationCancelEvent);
                editorWindow.rootVisualElement.schedule.Execute(() =>
                {
                    _search.Focus();
                });
            }

            public override void OnClose()
            {
                editorWindow.rootVisualElement.UnregisterCallback<NavigationCancelEvent>(HandleNavigationCancelEvent);
            }

            private void HandleNavigationCancelEvent(NavigationCancelEvent evt)
            {
                editorWindow.Close();
            }

            private class DelayedSearchController : IDisposable
            {
                private const int DelayMs = 300;

                public event Action<string> SearchTextChanged;

                private readonly VisualElement _search;
                private readonly int _delayMs;
                private string _activeText;
                private readonly IVisualElementScheduledItem _scheduledItem;
                private bool _disposed;

                public DelayedSearchController(VisualElement search, int delayMs = DelayMs)
                {
                    _search = search;
                    _delayMs = delayMs;
                    _activeText = "";
                    _scheduledItem = null;
                    search.RegisterCallback<ChangeEvent<string>>(OnSearchChanged);
                    search.RegisterCallback<NavigationSubmitEvent>(OnSearchEnter);
                    _scheduledItem = search.schedule.Execute(UpdateItem);
                    _scheduledItem.Pause();
                }

                public void Dispose()
                {
                    if (_disposed)
                    {
                        return;
                    }
                    _disposed = true;
                    _search.UnregisterCallback<ChangeEvent<string>>(OnSearchChanged);
                    _search.UnregisterCallback<NavigationSubmitEvent>(OnSearchEnter);
                    _scheduledItem.Pause();
                }

                private void OnSearchEnter(NavigationSubmitEvent evt)
                {
                    if (_disposed)
                    {
                        return;
                    }
                    _scheduledItem?.ExecuteLater(0);
                }

                private void OnSearchChanged(ChangeEvent<string> evt)
                {
                    if (_disposed)
                    {
                        return;
                    }
                    _activeText = evt.newValue;
                    if (string.IsNullOrEmpty(_activeText))
                    {
                        _scheduledItem?.ExecuteLater(0);
                    }
                    else
                    {
                        _scheduledItem?.ExecuteLater(_delayMs);
                    }
                }

                private void UpdateItem()
                {
                    SearchTextChanged?.Invoke(_activeText);
                }
            }
        }
    }
}
