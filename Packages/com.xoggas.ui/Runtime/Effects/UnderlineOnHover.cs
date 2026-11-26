using UnityEngine;
using UnityEngine.UIElements;

namespace Xoggas.UI
{
    /// <summary>
    /// Wraps the text of a Label/Button in &lt;u&gt; tags while the pointer is over the hover element.
    /// The underlined element defaults to the hover element itself.
    /// </summary>
    public class UnderlineOnHover : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private string _hoverElementName;
        [SerializeField] private string _underlineElementName;

        private VisualElement _hoverElement;
        private TextElement _textElement;
        private string _originalText;

        private void OnEnable()
        {
            var root = _uiDocument.rootVisualElement;

            _hoverElement = root.Q(_hoverElementName);

            if (_hoverElement == null)
            {
                Debug.LogWarning($"[UnderlineOnHover] Hover element '{_hoverElementName}' not found.");
                return;
            }

            var underlineName = string.IsNullOrEmpty(_underlineElementName)
                ? _hoverElementName
                : _underlineElementName;

            _textElement = _hoverElement.Q<TextElement>(underlineName);

            if (_textElement == null)
            {
                Debug.LogWarning($"[UnderlineOnHover] Underline element '{underlineName}' not found.");
                return;
            }

            _hoverElement.RegisterCallback<MouseEnterEvent>(OnMouseIn);
            _hoverElement.RegisterCallback<MouseLeaveEvent>(OnMouseOut);
            _textElement.RegisterCallback<ChangeEvent<string>>(OnTextChanged);
        }

        private void OnDisable()
        {
            if (_hoverElement != null)
            {
                _hoverElement.UnregisterCallback<MouseEnterEvent>(OnMouseIn);
                _hoverElement.UnregisterCallback<MouseLeaveEvent>(OnMouseOut);
            }

            if (_textElement != null)
            {
                _textElement.UnregisterCallback<ChangeEvent<string>>(OnTextChanged);
            }
        }

        private void OnMouseIn(MouseEnterEvent _)
        {
            _originalText = StripUnderlineTag(_textElement.text);
            _textElement.text = $"<u>{_originalText}</u>";
        }

        private void OnMouseOut(MouseLeaveEvent _)
        {
            if (_originalText != null)
            {
                _textElement.text = _originalText;
            }
        }

        private void OnTextChanged(ChangeEvent<string> e)
        {
            _originalText = StripUnderlineTag(e.newValue);
        }

        private static string StripUnderlineTag(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Replace("<u>", "").Replace("</u>", "");
        }
    }
}
