using Godot;
using System;

public partial class LevelSelect : Node
{
    LevelFile _level;
        
    public void PlayLevel(LevelFile level) {
        _level = level;
        var stage = Stage.Instantiate(_level);
        Util.Root(this).AddChild(stage);
        QueueFree();
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        // TODO: Delete
        //for (int i = 1; i <= 24; ++i) {
        //    string filename = $"res://Level/{i:d3}.json";
        //    GD.Print(filename);
        //    var lf = LevelFile.Read(filename);
        //    lf.Save(filename);
        //}
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
    }
}
