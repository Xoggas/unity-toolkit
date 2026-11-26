using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Xoggas.UI.Editor
{
    [CustomEditor(typeof(Xoggas.UI.UnderlineOnHover))]
    public class UnderlineOnHoverEditor : UnityEditor.Editor
    {
        private SerializedProperty _uiDocumentProp;
        private SerializedProperty _hoverElementNameProp;
        private SerializedProperty _underlineElementNameProp;

        private List<string> _elementNames = new();

        private DropdownField _hoverDropdown;
        private DropdownField _underlineDropdown;
        private Toggle _separateToggle;

        public override VisualElement CreateInspectorGUI()
        {
            _uiDocumentProp = serializedObject.FindProperty("_uiDocument");
            _hoverElementNameProp = serializedObject.FindProperty("_hoverElementName");
            _underlineElementNameProp = serializedObject.FindProperty("_underlineElementName");

            var root = new VisualElement();
            root.style.paddingTop = 4;

            // UIDocument
            var documentField = new PropertyField(_uiDocumentProp);
            documentField.RegisterValueChangeCallback(_ =>
            {
                RefreshElementNames();
                RebuildDropdowns();
            });
            root.Add(documentField);

            // Refresh button
            var refreshButton = new Button(RefreshAndRebuild) { text = "Refresh Elements" };
            refreshButton.style.marginTop = 4;
            root.Add(refreshButton);

            var divider = new VisualElement();
            divider.style.height = 1;
            divider.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            divider.style.marginTop = 6;
            divider.style.marginBottom = 6;
            root.Add(divider);

            // Hover dropdown
            _hoverDropdown = new DropdownField("Hover Element", _elementNames, 0);
            _hoverDropdown.RegisterValueChangedCallback(e =>
            {
                _hoverElementNameProp.stringValue = e.newValue;
                serializedObject.ApplyModifiedProperties();
            });
            root.Add(_hoverDropdown);

            root.Add(new VisualElement { style = { height = 4 } });

            // Separate toggle
            _separateToggle = new Toggle("Separate Underline");
            _separateToggle.value = !string.IsNullOrEmpty(_underlineElementNameProp.stringValue);
            _separateToggle.RegisterValueChangedCallback(e =>
            {
                _underlineDropdown.style.display = e.newValue ? DisplayStyle.Flex : DisplayStyle.None;

                if (!e.newValue)
                {
                    _underlineElementNameProp.stringValue = string.Empty;
                    serializedObject.ApplyModifiedProperties();
                }
            });
            root.Add(_separateToggle);

            // Underline dropdown
            _underlineDropdown = new DropdownField("Underline Element", _elementNames, 0);
            _underlineDropdown.style.display = _separateToggle.value ? DisplayStyle.Flex : DisplayStyle.None;
            _underlineDropdown.RegisterValueChangedCallback(e =>
            {
                _underlineElementNameProp.stringValue = e.newValue;
                serializedObject.ApplyModifiedProperties();
            });
            root.Add(_underlineDropdown);

            RefreshElementNames();
            RebuildDropdowns();

            return root;
        }

        private void RefreshAndRebuild()
        {
            RefreshElementNames();
            RebuildDropdowns();
        }

        private void RebuildDropdowns()
        {
            if (_hoverDropdown == null || _underlineDropdown == null) return;

            var choices = _elementNames.Count > 0 ? _elementNames : new List<string> { "—" };

            _hoverDropdown.choices = choices;
            _hoverDropdown.SetValueWithoutNotify(
                _elementNames.Contains(_hoverElementNameProp.stringValue)
                    ? _hoverElementNameProp.stringValue
                    : choices[0]
            );

            _underlineDropdown.choices = choices;
            _underlineDropdown.SetValueWithoutNotify(
                _elementNames.Contains(_underlineElementNameProp.stringValue)
                    ? _underlineElementNameProp.stringValue
                    : choices[0]
            );
        }

        private void RefreshElementNames()
        {
            _elementNames.Clear();

            var uiDocument = _uiDocumentProp.objectReferenceValue as UIDocument;
            if (uiDocument?.visualTreeAsset == null) return;

            var tempRoot = new VisualElement();
            uiDocument.visualTreeAsset.CloneTree(tempRoot);

            _elementNames = tempRoot.Query()
                .ToList()
                .Where(e => !string.IsNullOrEmpty(e.name))
                .Select(e => e.name)
                .Distinct()
                .ToList();
        }
    }
}
