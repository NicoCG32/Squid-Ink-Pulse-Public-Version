public static class SquidInkPulseInputContract
{
    public const string GameplayMap = "Gameplay";
    public const string UiMap = "UI";

    public static class Gameplay
    {
        public const string SteerPosition = "SteerPosition";
        public const string ActivateInkPulse = "ActivateInkPulse";
        public const string TogglePause = "TogglePause";
        public const string UseGadgetSlot1 = "UseGadgetSlot1";
        public const string UseGadgetSlot2 = "UseGadgetSlot2";
    }

    public static class Ui
    {
        public const string Navigate = "Navigate";
        public const string Submit = "Submit";
        public const string Cancel = "Cancel";
        public const string Point = "Point";
        public const string Click = "Click";
        public const string RightClick = "RightClick";
        public const string MiddleClick = "MiddleClick";
        public const string ScrollWheel = "ScrollWheel";
        public const string TrackedDevicePosition = "TrackedDevicePosition";
        public const string TrackedDeviceOrientation = "TrackedDeviceOrientation";
    }

    public static class ControlSchemes
    {
        public const string KeyboardAndMouse = "Keyboard&Mouse";
        public const string Gamepad = "Gamepad";
        public const string Touch = "Touch";
    }
}
