using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Xoggas.UI
{
    /// <summary>
    /// A transient element inside a <see cref="UIDocument"/>: <see cref="ShowInternal"/> adds the
    /// <c>visible</c> USS class for <c>_duration</c> seconds, then removes it.
    /// </summary>
    public abstract class Popup : MonoBehaviour
    {
        public const string VisibleClass = "visible";

        protected VisualElement Root { get; private set; }

        [Header("Components")]
        [SerializeField]
        private UIDocument _rootDocument;

        [Header("Settings")]
        [SerializeField]
        private float _duration = 0.4f;

        [SerializeField]
        private string _popupName;

        protected virtual void Awake()
        {
            Root = _rootDocument.rootVisualElement.Q<VisualElement>(_popupName);
        }

        protected void ShowInternal()
        {
            StartCoroutine(ShowingRoutine());
        }

        private IEnumerator ShowingRoutine()
        {
            Root.AddToClassList(VisibleClass);

            yield return new WaitForSeconds(_duration);

            Root.RemoveFromClassList(VisibleClass);
        }
    }
}
