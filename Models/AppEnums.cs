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

public enum MediaFilterMode
{
    MusicOnly,       // Default: Only Apple Music & Spotify (filters out Chrome, YouTube, Edge, etc.)
    AllMedia,        // All system media sessions
    AppleMusicOnly,  // Lock to Apple Music only
    SpotifyOnly      // Lock to Spotify only
}

public enum WidgetSizePreset
{
    Compact,   // 360px
    Standard,  // 430px (default)
    Wide,      // 500px
    Custom
}
