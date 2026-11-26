using UnityEngine.UIElements;

namespace Xoggas.UI
{
    /// <summary>
    /// A panel with "Ok", "Close" and "Cancel" buttons. "Ok" confirms, the other two decline.
    /// </summary>
    public abstract class ConfirmationPanel : UIPanel
    {
        protected override void Awake()
        {
            base.Awake();

            Root.Q<Button>("Ok").clicked += Confirmed;
            Root.Q<Button>("Close").clicked += Declined;
            Root.Q<Button>("Cancel").clicked += Declined;
        }

        protected abstract void Confirmed();
        protected abstract void Declined();
    }
}
