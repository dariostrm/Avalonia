using Avalonia.Metadata;

namespace Avalonia.Input.Raw
{
    public enum RawKeyEventType
    {
        KeyDown,
        KeyUp
    }

    [PrivateApi]
    public class RawKeyEventArgs : RawInputEventArgs
    {
        public RawKeyEventArgs(
            IInputDevice device,
            ulong timestamp,
            IInputRoot root,
            RawKeyEventType type,
            Key key,
            RawInputModifiers modifiers,
            PhysicalKey physicalKey,
            string? keySymbol,
            KeyDeviceType keyDeviceType = KeyDeviceType.Keyboard)
            : base(device, timestamp, root)
        {
            Type = type;
            Key = key;
            Modifiers = modifiers;
            PhysicalKey = physicalKey;
            KeySymbol = keySymbol;
            KeyDeviceType = keyDeviceType;
        }

        public Key Key { get; set; }

        public RawInputModifiers Modifiers { get; set; }

        public RawKeyEventType Type { get; set; }

        public PhysicalKey PhysicalKey { get; set; }

        public KeyDeviceType KeyDeviceType { get; set; }

        public string? KeySymbol { get; set; }

        public string? KeyText { get; set; }

        public string? AlternateKeyText { get; set; }

        public RawInputModifiers? ConsumedModifiers { get; set; }

        public PlatformKeyInfo? PlatformKeyInfo { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the soft keyboard (input pane) of the root was
        /// open when the key was received. Soft keyboards may send key events (e.g. Backspace, Enter).
        /// </summary>
        internal bool IsInputPaneOpen { get; set; }
    }
}
