#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEssentials
{
    public sealed class CustomReorderableListUIToolkitPlain
    {
        private readonly Func<VisualElement> _doLayoutList;
        private readonly Action _refresh;

        internal CustomReorderableListUIToolkitPlain(Func<VisualElement> doLayoutList, Action refresh)
        {
            _doLayoutList = doLayoutList;
            _refresh = refresh;
        }

        public VisualElement DoLayoutList() => _doLayoutList();
        public void Refresh() => _refresh();
    }

    /// <summary>
    /// Simplified UI Toolkit reorderable list that binds directly to an <see cref="IList{T}"/>,
    /// similar to IMGUI <c>ReorderableList</c> usage.
    /// </summary>
    public sealed class CustomReorderableListUIToolkit<T>
    {
        private sealed class ListAdapter : IList
        {
            private readonly IList<T> _list;

            public ListAdapter(IList<T> list) => _list = list;

            public int Count => _list.Count;
            public bool IsReadOnly => _list.IsReadOnly;
            public bool IsFixedSize => false;
            public bool IsSynchronized => false;
            public object SyncRoot => this;

            public object this[int index]
            {
                get => _list[index];
                set => _list[index] = (T)value;
            }

            public int Add(object value)
            {
                _list.Add((T)value);
                return _list.Count - 1;
            }

            public void Clear() => _list.Clear();

            public bool Contains(object value) => value is T t && _list.Contains(t);

            public int IndexOf(object value) => value is T t ? _list.IndexOf(t) : -1;

            public void Insert(int index, object value) => _list.Insert(index, (T)value);

            public void Remove(object value)
            {
                if (value is T t)
                    _list.Remove(t);
            }

            public void RemoveAt(int index) => _list.RemoveAt(index);

            public void CopyTo(Array array, int index)
            {
                for (var i = 0; i < _list.Count; i++)
                    array.SetValue(_list[i], index + i);
            }

            public IEnumerator GetEnumerator() => _list.GetEnumerator();
        }

        private readonly IList<T> _items;
        private readonly IList _itemsSource;
        private readonly Func<VisualElement> _makeItem;
        private readonly Action<VisualElement, int> _bindItem;
        private readonly Action _onAdd;
        private readonly Action<int> _onRemoveAt;
        private readonly float _maxHeight;

        private VisualElement _root;
        private ListView _listView;

        public CustomReorderableListUIToolkit(
            IList<T> items,
            string header,
            Func<VisualElement> makeItem,
            Action<VisualElement, int> bindItem,
            Action onAdd,
            Action<int> onRemoveAt,
            float maxHeight = 260f)
        {
            _items = items;
            _itemsSource = new ListAdapter(items);
            Header = header;
            _makeItem = makeItem;
            _bindItem = bindItem;
            _onAdd = onAdd;
            _onRemoveAt = onRemoveAt;
            _maxHeight = maxHeight;
        }

        public string Header { get; }

        public VisualElement DoLayoutList()
        {
            if (_root != null)
                return _root;

            _root = new VisualElement();
            _root.style.flexDirection = FlexDirection.Column;

            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.alignItems = Align.Center;
            headerRow.style.marginBottom = 2;

            if (!string.IsNullOrEmpty(Header))
            {
                var title = new Label(Header);
                title.style.unityFontStyleAndWeight = FontStyle.Bold;
                title.style.flexGrow = 1;
                headerRow.Add(title);
            }
            else
            {
                headerRow.Add(new VisualElement { style = { flexGrow = 1 } });
            }

            var buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.alignItems = Align.Center;

            var addBtn = new Button(Add) { text = "+" };
            addBtn.style.width = 22;
            addBtn.style.height = 18;
            addBtn.style.marginLeft = 4;

            var removeBtn = new Button(RemoveSelectedOrLast) { text = "-" };
            removeBtn.style.width = 22;
            removeBtn.style.height = 18;
            removeBtn.style.marginLeft = 2;

            buttons.Add(addBtn);
            buttons.Add(removeBtn);
            headerRow.Add(buttons);

            _root.Add(headerRow);

            _listView = new ListView
            {
                reorderable = true,
                showBorder = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                selectionType = SelectionType.Single,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                itemsSource = _itemsSource
            };

            _listView.style.maxHeight = _maxHeight;
            _listView.makeItem = _makeItem;
            _listView.bindItem = (e, i) => _bindItem(e, i);

            _listView.itemIndexChanged += (from, to) =>
            {
                if (_items is List<T> list)
                {
                    var item = list[from];
                    list.RemoveAt(from);
                    list.Insert(to, item);
                }

                Refresh();
            };

            _root.Add(_listView);

            return _root;
        }

        public void Refresh() => _listView?.Rebuild();

        private void Add()
        {
            _onAdd?.Invoke();
            Refresh();
        }

        private void RemoveSelectedOrLast()
        {
            if (_items.Count <= 0)
                return;

            var idx = _listView?.selectedIndex ?? -1;
            if (idx < 0 || idx >= _items.Count)
                idx = _items.Count - 1;

            _onRemoveAt?.Invoke(idx);
            Refresh();
        }

        public CustomReorderableListUIToolkitPlain AsNonGeneric() =>
            new CustomReorderableListUIToolkitPlain(DoLayoutList, Refresh);
    }
}
#endif

