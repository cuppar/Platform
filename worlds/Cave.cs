using Godot;
using Platform.constants;
using Platform.globals;

namespace Platform.worlds;

public partial class Cave : World
{
    public async void OnBoarDied()
    {
        using var timer = GetTree().CreateTimer(1);
        await ToSignal(timer, SceneTreeTimer.SignalName.Timeout);

        AutoloadManager.Game.ChangeScene(ScenePaths.GameEndScreen, new Game.ChangeSceneParams
        {
            Duration = 1
        });
    }
}