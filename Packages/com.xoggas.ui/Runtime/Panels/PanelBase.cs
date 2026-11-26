using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace Xoggas.UI
{
    /// <summary>
    /// Base class for anything <see cref="PanelSwitcher"/> can navigate to.
    /// </summary>
    public abstract class PanelBase : MonoBehaviour
    {
        protected PanelSwitcher PanelSwitcher => _panelSwitcher;

        [Header("Panel Settings")]
        [FormerlySerializedAs("_isActive")]
        [SerializeField]
        protected bool IsActive;

        [Inject]
        private readonly PanelSwitcher _panelSwitcher;

        public abstract void Activate(PanelActivationArgs args = null);
        public abstract void Deactivate();
    }
}
