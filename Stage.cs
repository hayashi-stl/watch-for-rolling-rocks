using Godot;
using System;

public partial class Stage : Node
{
    LevelFile _levelFile;
    Level level;

    const int FULL_SIZE_MAX_HEIGHT = 14;

    public static Stage Instantiate(LevelFile level) {
        var stage = Global.Scene.Stage.Instance<Stage>();
        stage._levelFile = level;
        return stage;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        GetNode<Label>("%Title").Text = _levelFile.Name;
        GetNode<Label>("%Controls").Text = _levelFile.Controls;

        var windowSize = GetViewport().GetVisibleRect().Size;
        level = Level.Instantiate(_levelFile);
        level.LevelStage = this;
        AddChild(level);
        MoveChild(level, 0);
        var levelSize = level.LevelRect();
        var scale = Mathf.Min(
            Mathf.Min(1.0f, (float)FULL_SIZE_MAX_HEIGHT / levelSize.Size.y),
            (float)FULL_SIZE_MAX_HEIGHT * windowSize.x / windowSize.y / levelSize.Size.x
        );
        level.Scale = Vector2.One * scale;
        var dims = (Vector2)levelSize.Size * Util.TileSize * scale;
        var corner = (windowSize - dims) / 2;
        level.BasePosition = corner;
    }

    public void SetLevelClear(bool is_clear) {
        GetNode<Label>("%Clear").Visible = is_clear;
        GetNode<Timer>("%Timer").Start();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta) {
        if (Input.IsActionJustPressed("back")) {
            QueueFree();
            Util.Root(this).AddChild(Global.Scene.LevelSelect.Instance());
        }
    }


    public void _on_Timer_timeout() {
        QueueFree();
        Util.Root(this).AddChild(Global.Scene.LevelSelect.Instance());
    }
}
