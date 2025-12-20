namespace Home.Base.Widgets
{
    public interface ISettingsView
    {
        /// <summary>
        /// Called when the user clicks the "Save" button.
        /// The view should commit changes to the configuration.
        /// </summary>
        void OnSave();

        /// <summary>
        /// Called when the user clicks the "Reset" button.
        /// The view should restore default values.
        /// </summary>
        void OnReset();

        /// <summary>
        /// Called when the user clicks "Close" or "Cancel".
        /// The view should revert any unsaved changes if necessary.
        /// </summary>
        void OnCancel();
    }
}
