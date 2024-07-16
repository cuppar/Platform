﻿using Godot;

namespace Platform.globals;

public static class AutoloadManager
{
    public static Game Game { get; } =
        ((SceneTree)Engine.GetMainLoop()).Root.GetNode<Game>("/root/Game");
}