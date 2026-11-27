using System;
using Avalonia.Input.Raw;
using Avalonia.Reactive;

namespace Avalonia.Input
{
    /// <summary>
    /// Receives input from the windowing subsystem and dispatches it to interested parties
    /// for processing.
    /// </summary>
    internal class InputManager : IInputManager, IDisposable
    {
        private readonly LightweightSubject<RawInputEventArgs> _preProcess = new();
        private readonly LightweightSubject<RawInputEventArgs> _process = new();
        private readonly LightweightSubject<RawInputEventArgs> _postProcess = new();
        private IInputRoot? _lastPointerRoot;
        private Point _lastPointerPosition;

        /// <summary>
        /// Gets the global instance of the input manager.
        /// </summary>
        public static IInputManager? Instance => AvaloniaLocator.Current.GetService<IInputManager>();

        /// <inheritdoc/>
        public IObservable<RawInputEventArgs> PreProcess => _preProcess;

        /// <inheritdoc/>
        public IObservable<RawInputEventArgs> Process => _process;

        /// <inheritdoc/>
        public IObservable<RawInputEventArgs> PostProcess => _postProcess;

        /// <summary>
        /// Gets the kind of device that produced the most recent user input. Decides whether focus
        /// moved with <see cref="NavigationMethod.Unspecified"/> shows a focus visual.
        /// </summary>
        internal LastInputDevice LastInputDevice { get; set; }

        /// <inheritdoc/>
        public void ProcessInput(RawInputEventArgs e)
        {
            UpdateLastInputDevice(e);
            _preProcess.OnNext(e);
            e.Device?.ProcessRawEvent(e);
            _process.OnNext(e);
            _postProcess.OnNext(e);
        }

        private void UpdateLastInputDevice(RawInputEventArgs e)
        {
            // Text input is ignored: a hardware key always raises a RawKeyEventArgs first, and text
            // input on its own comes from soft keyboards, IME, dictation and similar.
            switch (e)
            {
                case RawKeyEventArgs key:
                    // With a soft keyboard open, only keys that move focus count as keyboard input,
                    // so typing on a touch device does not show focus visuals (same as WinUI).
                    if (!key.IsInputPaneOpen || IsFocusNavigationKey(key.Key))
                        LastInputDevice = LastInputDevice.Keyboard;
                    break;

                case RawPointerEventArgs pointer:
                    var position = pointer.Position;
                    var root = pointer.Root;

                    switch (pointer.Type)
                    {
                        case RawPointerEventType.LeaveWindow:
                        case RawPointerEventType.CancelCapture:
                            break;

                        // Platforms send moves without a change in position, for example when a
                        // window or popup opens under the cursor. These are not user input.
                        case RawPointerEventType.Move when root == _lastPointerRoot && position == _lastPointerPosition:
                            break;

                        default:
                            LastInputDevice = LastInputDevice.Pointer;
                            break;
                    }

                    _lastPointerRoot = root;
                    _lastPointerPosition = position;
                    break;
            }
        }

        private static bool IsFocusNavigationKey(Key key) =>
            key is Key.Tab or Key.Left or Key.Right or Key.Up or Key.Down;

        public void Dispose()
        {
            _preProcess.OnCompleted();
            _process.OnCompleted();
            _postProcess.OnCompleted();
        }
    }
}
