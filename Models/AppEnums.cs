namespace PommeBar.Models;

public enum DockMode
{
    Floating,   // Island mode: floats 10px above taskbar with rounded acrylic corners
    Embedded    // Taskbar mode: sits flush against the taskbar surface
}

public enum LockMode
{
    Free,        // Unlocked, draggable anywhere
    Locked,      // Position locked
    SuperLocked  // Persistent topmost overlay over games and full screen windows
}

public enum MediaAppType
{
    Unknown,
    AppleMusic,
    Spotify,
    Browser,
    Other
}
