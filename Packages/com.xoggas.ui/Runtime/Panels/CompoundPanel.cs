using System.Collections.Generic;
using UnityEngine;

namespace Xoggas.UI
{
    /// <summary>
    /// Activates and deactivates several panels as one navigation step.
    /// </summary>
    public class CompoundPanel : PanelBase
    {
        [SerializeField]
        private List<PanelBase> _children;

        public override void Activate(PanelActivationArgs args = null)
        {
            foreach (var child in _children)
            {
                child.Activate(args);
            }
        }

        public override void Deactivate()
        {
            foreach (var child in _children)
            {
                child.Deactivate();
            }
        }
    }
}
