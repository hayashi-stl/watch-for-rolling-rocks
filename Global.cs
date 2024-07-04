using Godot;
using System;
using System.ComponentModel;

public partial class Global : Node
{
    int _currEntityID = 0;

	public static Global Instance(Node node) => node.GetNode<Global>("/root/Global");

    public static Level Level(Node node) {
        var parent = node;
        while (true) {
            if (parent is Level level)
                return level;
            parent = parent.GetParent();
        }
    }

    public class Scene {
        //public static readonly PackedScene Fader = GD.Load<PackedScene>("res://Fader.tscn");

        public static readonly PackedScene Player = GD.Load<PackedScene>("res://Player/Player.tscn");
        public static readonly PackedScene Rock = GD.Load<PackedScene>("res://Rock/Rock.tscn");
        public static readonly PackedScene Block = GD.Load<PackedScene>("res://Block/Block.tscn");
        public static readonly PackedScene Stairs = GD.Load<PackedScene>("res://Stairs/Stairs.tscn");

        public static readonly PackedScene Stage = GD.Load<PackedScene>("res://Stage.tscn");
        public static readonly PackedScene Level = GD.Load<PackedScene>("res://Level.tscn");
        public static readonly PackedScene LevelSelect = GD.Load<PackedScene>("res://LevelSelect.tscn");
    }

    public class Tile {
        public const int Invalid = -1;
        public const int Floor = 0;
        public const int Wall = 1;
        public const int Spikes = 2;

        public static bool IsWall(int tile) => tile == Wall;
        public static bool IsHazard(int tile) => tile == Spikes;
        public static (int Tile, int Z) ToLevelFileTile(int tile) => tile switch {
            Invalid => (LevelFile.Tile.Invalid, 0),
            Floor   => (LevelFile.Tile.Block,   0),
            Wall    => (LevelFile.Tile.Block,   1),
            Spikes  => (LevelFile.Tile.Spikes,  0),
            _ => throw new InvalidEnumArgumentException()
        };

        public static int FromLevelFileTile(int levelFileTile, int z) => levelFileTile switch {
            LevelFile.Tile.Invalid => Invalid,
            LevelFile.Tile.Block => z == 0 ? Floor : Wall,
            LevelFile.Tile.Spikes => Spikes,
            _ => throw new InvalidEnumArgumentException()
        };
    }

    public class ParticleEffect {
    }

    public class SFX {
    }

    public int NextEntityID() {
        _currEntityID += 1;
        return _currEntityID - 1;
	}

    public AudioStreamPlayer PlaySound(PackedScene scene) {
        var player = scene.Instance<AudioStreamPlayer>();
        AddChild(player);
        return player;
    }
}
