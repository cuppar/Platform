using Godot;

namespace Platform.globals;

public static class AutoloadManager
{
    public static Game Game { get; } =
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode<Game>("/root/Game");

    public static SoundManager SoundManager { get; } =
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode<SoundManager>("/root/SoundManager");
}