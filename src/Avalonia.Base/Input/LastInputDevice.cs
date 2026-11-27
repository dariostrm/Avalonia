namespace Avalonia.Input
{
    /// <summary>
    /// The kind of device that produced the most recent user input.
    /// </summary>
    internal enum LastInputDevice
    {
        /// <summary>
        /// No input has been received yet. Treated the same as <see cref="Pointer"/>.
        /// </summary>
        None,

        /// <summary>
        /// A hardware keyboard, gamepad or remote.
        /// </summary>
        Keyboard,

        /// <summary>
        /// A mouse, touch, pen or touchpad.
        /// </summary>
        Pointer,
    }
}
