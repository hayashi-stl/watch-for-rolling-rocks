using Godot;
using System;

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

        public static readonly PackedScene Block = GD.Load<PackedScene>("res://Object/Block/Block.tscn");
        public static readonly PackedScene Player = GD.Load<PackedScene>("res://Object/Player/Player.tscn");
        public static readonly PackedScene Baddy = GD.Load<PackedScene>("res://Object/Baddy/Baddy.tscn");
        public static readonly PackedScene Coin = GD.Load<PackedScene>("res://Object/Coin/Coin.tscn");

        public static readonly PackedScene Times = GD.Load<PackedScene>("res://Object/Counter/Times.tscn");

        public static readonly PackedScene Punch = GD.Load<PackedScene>("res://Object/Player/Punch.tscn");

        public static readonly PackedScene Stage = GD.Load<PackedScene>("res://Stage.tscn");
        public static readonly PackedScene Level = GD.Load<PackedScene>("res://Level/Level.tscn");
        public static readonly PackedScene LevelSelect = GD.Load<PackedScene>("res://LevelSelect.tscn");

        public static readonly PackedScene MeshTilemap = GD.Load<PackedScene>("res://Tileset/MeshTilemap.tscn");
        public static readonly PackedScene MeshTile = GD.Load<PackedScene>("res://Tileset/MeshTile.tscn");
    }

    public class Tileset {
        public class Plain {
            public static readonly PackedScene Outline = GD.Load<PackedScene>("res://Tileset/Plain Outline.tscn");
            public static readonly PackedScene Floor = GD.Load<PackedScene>("res://Tileset/Plain_Floor.tscn");
            public static readonly PackedScene Wall = GD.Load<PackedScene>("res://Tileset/Plain_Wall.tscn");
        }
    }

    public class ParticleEffect {
        public static readonly PackedScene Dash = GD.Load<PackedScene>("res://Effect/Dash.tscn");
        public static readonly PackedScene BaddyPoof = GD.Load<PackedScene>("res://Effect/BaddyPoof.tscn");
        public static readonly PackedScene PlayerPoof = GD.Load<PackedScene>("res://Effect/PlayerPoof.tscn");
        public static readonly PackedScene CoinSparkles = GD.Load<PackedScene>("res://Effect/CoinSparkles.tscn");
    }

    public class SFX {
        public static readonly PackedScene Decrement = GD.Load<PackedScene>("res://Object/Counter/DecrementSFX.tscn");
        public static readonly PackedScene Move = GD.Load<PackedScene>("res://Object/MoveSFX.tscn");
        public static readonly PackedScene Bump = GD.Load<PackedScene>("res://Object/BumpSFX.tscn");
        public static readonly PackedScene Punch = GD.Load<PackedScene>("res://Object/PunchSFX.tscn");
        public static readonly PackedScene Poof = GD.Load<PackedScene>("res://Object/PoofSFX.tscn");
        public static readonly PackedScene Squish = GD.Load<PackedScene>("res://Object/SquishSFX.tscn");
        public static readonly PackedScene Swish = GD.Load<PackedScene>("res://Object/SwishSFX.tscn");
        public static readonly PackedScene PunchSwing = GD.Load<PackedScene>("res://Object/PunchSwingSFX.tscn");
        public static readonly PackedScene CoinCollect = GD.Load<PackedScene>("res://Object/Coin/CollectSFX.tscn");
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
