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

        public static readonly PackedScene Player = GD.Load<PackedScene>("res://Player/Player.tscn");
        public static readonly PackedScene Rock = GD.Load<PackedScene>("res://Rock/Rock.tscn");
        public static readonly PackedScene Block = GD.Load<PackedScene>("res://Block/Block.tscn");
        public static readonly PackedScene Stairs = GD.Load<PackedScene>("res://Stairs/Stairs.tscn");

        public static readonly PackedScene Stage = GD.Load<PackedScene>("res://Stage.tscn");
        public static readonly PackedScene Level = GD.Load<PackedScene>("res://Level.tscn");
        public static readonly PackedScene LevelSelect = GD.Load<PackedScene>("res://LevelSelect.tscn");
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
