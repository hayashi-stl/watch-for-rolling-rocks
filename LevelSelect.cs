using Godot;
using System;
using System.Linq;

public partial class LevelSelect : Node
{
    LevelFile _level;
    [Export]
    NodePath _initFocus;
    [Export]
    Godot.Collections.Array<NodePath> _grid;
    [Export]
    int _gridWidth;
        
    public void PlayLevel(LevelFile level) {
        _level = level;
        var stage = Stage.Instantiate(_level);
        Util.Root(this).AddChild(stage);
        QueueFree();
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GetNode<Control>(_initFocus).GrabFocus();

        var grid = Enumerable.Range(0, _grid.Count / _gridWidth)
            .Select(i => _grid.Skip(i * _gridWidth).Take(_gridWidth).Select(p => GetNode<Control>(p)).ToList())
            .ToList();

        int sizeY = _grid.Count / _gridWidth;
        int sizeX = _gridWidth;
        for (int y = 0; y < sizeY; ++y)
            for (int x = 0; x < sizeX; ++x) {
                grid[y][x].FocusNeighbourLeft   = grid[y][(x + sizeX - 1) % sizeX].GetPath();
                grid[y][x].FocusNeighbourRight  = grid[y][(x + sizeX + 1) % sizeX].GetPath();
                grid[y][x].FocusNeighbourTop    = grid[(y + sizeY - 1) % sizeY][x].GetPath();
                grid[y][x].FocusNeighbourBottom = grid[(y + sizeY + 1) % sizeY][x].GetPath();
            }
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
