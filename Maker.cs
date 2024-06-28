using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

[Tool]
public class Maker : Node2D
{
    TileMap _floorOutline;
    TileMap _floor;
    TileMap _wallOutline;
    TileMap _wall;

    bool _save = false;
    [Export]
    bool Save {
        get { return _save; }
        set {
            if (value) {
                var level = SaveLevel();
                _levelFile.Set("text", level.ToJson());
            }
        }
    }

    [Export]
    string LevelName { get; set; }

    Resource _levelFile;
    [Export]
    Resource LevelTextFile {
        get { return _levelFile; }
        set {
            _levelFile = value;
            if (_levelFile != null) {
                var level = LevelFile.FromJson(_levelFile);
                LoadLevel(level);
            }
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _floorOutline = GetNode<TileMap>("%TileMapFloorOutline");
        _floor = GetNode<TileMap>("%TileMapFloor");
        _wallOutline = GetNode<TileMap>("%TileMapWallOutline");
        _wall = GetNode<TileMap>("%TileMapWall");
    }

    void ClearLevel()
    {
        _floorOutline.Clear();
        _floor.Clear();
        _wallOutline.Clear();
        _wall.Clear();
    }

    void LoadLevel(LevelFile level)
    {
        ClearLevel();
        LevelName = level.Name;

        // Fill entries from the grid map
        var tileMapFloor = GetNode<TileMap>("%TileMapFloor");
        var tileMapWall = GetNode<TileMap>("%TileMapWall");
        for (int z = Level.MinZ; z < Level.MinZ + Level.SizeZ; ++z)
            for (int y = level.Base.y; y < level.Base.y + level.Size.y; ++y)
                for (int x = level.Base.x; x < level.Base.x + level.Size.x; ++x) {
                    var cellPosition = new Vector3I(x, y, z);
                    var mapPosition = cellPosition - level.Base;
                    var cell = level.Map[(mapPosition.z * level.Size.y + mapPosition.y) * level.Size.x + mapPosition.x];

                    if (cell != 0) {
                        if (z == 0) {
                            tileMapFloor.SetCell(x, y, 0);
                        } else if (z == 1) {
                            tileMapFloor.SetCell(x, y, -1);
                            tileMapWall.SetCell(x, y, 1);
                        }
                    }
                }

        foreach (var entData in level.Entities) {
            var def = new Entity.EntityDef(0, entData);
            var entityNode = def.SpawnInMaker(this);
        }
    }

    Rect2I Bounds()
    {
        var cells = _floor.GetUsedCells().Cast<Vector2>()
            .Concat(_wall.GetUsedCells().Cast<Vector2>())
            .Select(x => (Vector2I)x)
            .ToList();

        var minX = cells.Select(x => x.x).Min();
        var minY = cells.Select(x => x.y).Min();
        var maxX = cells.Select(x => x.x).Max() + 1;
        var maxY = cells.Select(x => x.y).Max() + 1;
        return new Rect2I(minX, minY, maxX - minX, maxY - minY);
    }

    LevelFile SaveLevel()
    {
        var bounds = Bounds();
        var entities = GetChildren().Cast<Node>().Where(n => n is EntityNode2D).Select(n => (EntityNode2D)n).ToList();

        return new LevelFile {
            Name = LevelName,
            Base = new Vector3I(bounds.Position.x, bounds.Position.y, Level.MinZ),
            Size = new Vector3I(bounds.Size.x, bounds.Size.y, Level.SizeZ),
            Map = Enumerable.Range(Level.MinZ, Level.SizeZ).SelectMany(z =>
                Enumerable.Range(bounds.Position.y, bounds.Size.y).SelectMany(y =>
                    Enumerable.Range(bounds.Position.x, bounds.Size.x).Select(x =>
                        z > 0
                            ? _wall.GetCell(x, y) != TileMap.InvalidCell ? 1 : 0
                            : _wall.GetCell(x, y) != TileMap.InvalidCell ||
                                _floor.GetCell(x, y) != TileMap.InvalidCell ? 1 : 0 
                ))).ToList(),
            Entities = entities.Select(e => e.LevelEntityFile()).ToList()
        };
    }

    void UpdateTileOutlines(TileMap map, TileMap outline)
    {
        var mapCells = map.GetUsedCells().Cast<Vector2>().Select(x => (Vector2I)x).ToHashSet();
        var outlineCells = outline.GetUsedCells().Cast<Vector2>().Select(x => (Vector2I)x).ToHashSet();
        var added = mapCells.Where(x => !outlineCells.Contains(x)).ToList();
        var removed = outlineCells.Where(x => !mapCells.Contains(x)).ToList();
        foreach (var cell in added)
            outline.SetCell(cell.x, cell.y, 0);
        foreach (var cell in removed)
            outline.SetCell(cell.x, cell.y, -1);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        UpdateTileOutlines(_wall, _wallOutline);
        UpdateTileOutlines(_floor, _floorOutline);
    }
}
