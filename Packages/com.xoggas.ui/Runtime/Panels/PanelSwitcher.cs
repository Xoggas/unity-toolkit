using System.Collections.Generic;
using UnityEngine;

namespace Xoggas.UI
{
    /// <summary>
    /// Stack-based navigation between panels: <see cref="GoTo"/> pushes, <see cref="GoBack()"/> pops.
    /// </summary>
    public sealed class PanelSwitcher : MonoBehaviour
    {
        [SerializeField]
        private PanelBase _startPanel;

        private readonly Stack<PanelBase> _panelStack = new();

        private void Awake()
        {
            _panelStack.Push(_startPanel);
        }

        public void GoTo(PanelBase panel, PanelActivationArgs args = null)
        {
            _panelStack.Peek().Deactivate();
            _panelStack.Push(panel);
            _panelStack.Peek().Activate(args);
        }

        public void GoBack()
        {
            GoBack(null);
        }

        public void GoBack(PanelActivationArgs args)
        {
            if (_panelStack.Count <= 1)
            {
                return;
            }

            _panelStack.Peek().Deactivate();
            _panelStack.Pop();
            _panelStack.Peek().Activate(args);
        }

        public void GoBackUntil<T>(PanelActivationArgs args = null) where T : PanelBase
        {
            if (_panelStack.Count <= 1)
            {
                return;
            }

            _panelStack.Peek().Deactivate();
            _panelStack.Pop();

            while (_panelStack.Count > 1 && _panelStack.Peek() is not T)
            {
                _panelStack.Pop();
            }

            _panelStack.Peek().Activate(args);
        }
    }
}
