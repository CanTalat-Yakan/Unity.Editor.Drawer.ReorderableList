#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEssentials
{
    public sealed class CustomReorderableList
    {
        private readonly Dictionary<string, ReorderableList> _listsCache = new();
        public void Draw(SerializedObject serializedObject, SerializedProperty property)
        {
            if (property == null || !property.isArray || property.propertyType == SerializedPropertyType.String)
                return;

            var targetType = serializedObject.targetObject.GetType();
            var fieldInfo = targetType.GetField(property.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var spaceAttribute = fieldInfo?.GetCustomAttribute<SpaceAttribute>();
            if (spaceAttribute != null)
                EditorGUILayout.Space(spaceAttribute.height);

            string cacheKey = property.serializedObject.targetObject.GetInstanceID().ToString() + property.propertyPath;

            ReorderableList reorderableList = null;
            if (!_listsCache.TryGetValue(cacheKey, out reorderableList))
            {
                reorderableList = new ReorderableList(serializedObject, property, true, true, true, true);
                _listsCache.Add(cacheKey, reorderableList);
            }

            reorderableList.drawHeaderCallback = (Rect position) =>
            {
                float buttonWidth = 70f;
                var labelPostion = new Rect(position.x - 16, position.y, position.width - buttonWidth - 5, position.height);
                var buttonPosition = new Rect(position.x + position.width - buttonWidth, position.y, buttonWidth, position.height);

                EditorGUI.LabelField(labelPostion, property.displayName);

                if (GUI.Button(buttonPosition, "Clear All", EditorStyles.toolbarButton))
                {
                    property.ClearArray();
                    property.serializedObject.ApplyModifiedProperties();
                }
            };

            reorderableList.drawElementCallback = (Rect position, int index, bool isActive, bool isFocused) =>
            {
                position.y += 2;
                position.height -= 4;
                var element = property.GetArrayElementAtIndex(index);
                EditorGUI.PropertyField(position, element, GUIContent.none);
            };

            var listRect = EditorGUILayout.GetControlRect(false, reorderableList.GetHeight());
            listRect = EditorGUI.IndentedRect(listRect);
            reorderableList.DoList(listRect);
            HandleDragAndDrop(listRect, property);
        }

        private void HandleDragAndDrop(Rect dropArea, SerializedProperty arrayProperty)
        {
            var evt = Event.current;

            if (!dropArea.Contains(evt.mousePosition))
                return;

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (DragAndDrop.objectReferences.Length > 0)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                        if (evt.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();
                            foreach (var obj in DragAndDrop.objectReferences)
                            {
                                int index = arrayProperty.arraySize;
                                arrayProperty.InsertArrayElementAtIndex(index);
                                var element = arrayProperty.GetArrayElementAtIndex(index);
                                if (element.propertyType == SerializedPropertyType.ObjectReference)
                                    element.objectReferenceValue = obj;
                            }
                            arrayProperty.serializedObject.ApplyModifiedProperties();
                        }

                        Event.current.Use();
                    }
                    break;
            }
        }
    }
}
#endif