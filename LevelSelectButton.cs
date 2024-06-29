using Godot;
using System;

[Tool]
public partial class LevelSelectButton : Button
{
    LevelFile _levelFile;
    Resource _level;
    [Export]
    Resource Level {
        get => _level;
        set {
            _level = value;
            if (value != null) {
                _levelFile = LevelFile.FromJson(value);
                Text = _levelFile.Name;
            }
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
    }

    public override void _Pressed()
    {
        base._Pressed();
        GetParent<LevelSelect>().PlayLevel(_levelFile);
    }
}
