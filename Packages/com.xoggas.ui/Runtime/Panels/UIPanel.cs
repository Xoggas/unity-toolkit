using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Xoggas.UI
{
    /// <summary>
    /// A panel backed by its own <see cref="UIDocument"/>, with separate layouts for
    /// portrait and landscape screens. Visibility is driven by the <c>enabled</c> USS class,
    /// so show/hide transitions can be styled in USS.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [RequireComponent(typeof(UIDocument))]
    public abstract class UIPanel : PanelBase
    {
        public enum DeactivationAction
        {
            None,
            ChangeDisplayStyle
        }

        public const string EnabledClass = "enabled";

        public static bool IsPortrait => Screen.height > Screen.width;

        protected VisualElement Root { get; private set; }

        private static readonly WaitForSeconds TransitionDelay = new(0.5f);

        [SerializeField]
        private UIDocument _document;

        [SerializeField]
        private DeactivationAction _deactivationAction;

        [Header("Responsive UI Settings")]
        [Header("Landscape")]
        [SerializeField]
        private VisualTreeAsset _landscapeDocument;

        [SerializeField]
        private PanelSettings _landscapePanelSettings;

        [Header("Portrait")]
        [SerializeField]
        private VisualTreeAsset _portraitDocument;

        [SerializeField]
        private PanelSettings _portraitPanelSettings;

        public void ForceRevalidate()
        {
            OnValidate();
        }

        protected virtual void Awake()
        {
            if (IsPortrait)
            {
                _document.visualTreeAsset = _portraitDocument;
                _document.panelSettings = _portraitPanelSettings;
            }
            else
            {
                _document.visualTreeAsset = _landscapeDocument;
                _document.panelSettings = _landscapePanelSettings;
            }

            Root = _document.rootVisualElement;

            SetActive(Root, IsActive);
            SetVisible(Root, IsActive);
        }

        private void OnValidate()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            if (root is null)
            {
                Debug.LogWarning("Couldn't update the UI element, press Panels/Force Validate to update them");
                return;
            }

            SetActive(root, IsActive);
            SetVisible(root, IsActive);
        }

        public override void Activate(PanelActivationArgs args = null)
        {
            StartCoroutine(ActivationRoutine());
        }

        public override void Deactivate()
        {
            StartCoroutine(DeactivationRoutine());
        }

        private IEnumerator ActivationRoutine()
        {
            SetActive(Root, true);
            SetVisible(Root, true);

            yield return TransitionDelay;
        }

        private IEnumerator DeactivationRoutine()
        {
            SetVisible(Root, false);

            yield return TransitionDelay;

            SetActive(Root, false);
        }

        private void SetActive(VisualElement element, bool isActive)
        {
            switch (_deactivationAction)
            {
                case DeactivationAction.ChangeDisplayStyle:
                    element.style.display = isActive ? DisplayStyle.Flex : DisplayStyle.None;
                    break;
            }
        }

        private static void SetVisible(VisualElement root, bool isActive)
        {
            root.EnableInClassList(EnabledClass, isActive);
        }
    }
}
