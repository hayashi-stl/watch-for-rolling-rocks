using Godot;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using Priority_Queue;

[Tool]
public partial class Block : EntityNode2D
{
    Sprite _activeVisual;

    public enum BlockType {
        Brittle,
    }

	public static readonly int NumTypes = Enum.GetNames(typeof(BlockType)).Length;

    BlockType _type = BlockType.Brittle;
    [Export]
    public BlockType Type {
        get => _type;
        set {
            _type = value;
            if (_ready)
                UpdateTexture();
        }
    }

    List<Vector2I> _shape = new List<Vector2I>(){ Vector2I.Zero };
    public List<Vector2I> Shape {
        get => _shape;
        set {
            _shape = value;
            if (_ready)
                UpdateTexture();
        }
    }
    [Export]
    public Godot.Collections.Array<Vector2> Shape_ {
        get => new Godot.Collections.Array<Vector2>(Shape.Select(v => (Vector2)v));
        set => Shape = value.Select(v => (Vector2I)v).ToList();
    }

    class Def {
        public Sprite Visual { get; set; }
        public Sprite HorzConnector { get; set; }
        public Sprite VertConnector { get; set; }
    }

    List<Def> _defs = Enumerable.Range(0, NumTypes).Select(_ => null as Def).ToList();

    List<Node2D> _addedNodes = new List<Node2D>();

    public override Entity LevelEntity(int id) {
        return new Ent(id, this);
    }
        
	public override LevelFile.EntityCustomData LevelEntityCustomParams() {
        return new LevelFile.BlockFile() {
            Type = Type,
            Shape = Shape,
        };
    }

    void UpdateShape() {
        foreach (var node in _addedNodes)
            node.QueueFree();
        _addedNodes.Clear();

        foreach (var pos in Shape) {
            var visual = (Node2D)_defs[(int)Type].Visual.Duplicate();
            AddChild(visual);
            visual.Position = (Vector2)pos * Util.TileSize;
            visual.Visible = true;
            visual.Owner = this;
            _addedNodes.Add(visual);
        }

        // Connectors
        var halfEdges = new HashSet<Vector2I>();
        var edges = new HashSet<Vector2I>();
        var directions = new List<Vector2I>(){ Vector2I.Right, Vector2I.Up, Vector2I.Left, Vector2I.Down };
        foreach (var pos in Shape)
            foreach (var dir in directions) {
                var edgePos = 2 * pos + dir;
                if (!halfEdges.Add(edgePos))
                    edges.Add(edgePos);
            }

        foreach (var edge in edges) {
            var visual = (Node2D)(edge.x % 2 != 0 ? _defs[(int)Type].HorzConnector : _defs[(int)Type].VertConnector).Duplicate();
            AddChild(visual);
            visual.Position = (Vector2)edge * Util.TileSize / 2;
            visual.Visible = true;
            visual.Owner = this;
            _addedNodes.Add(visual);
        }
    }
        
    protected override void UpdateTexture() {
        UpdateShape();
    }
            
            
    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        PrepareCommon();

        _defs[(int)BlockType.Brittle] = new Def() {
            Visual = GetNode<Sprite>("%Brittle"),
            HorzConnector = GetNode<Sprite>("%BrittleHorz"),
            VertConnector = GetNode<Sprite>("%BrittleVert"),
        };

        UpdateTexture();
    }


    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta) {
        if (Engine.EditorHint)
            Rotation = Mathf.Round(Rotation / (Mathf.Tau / 4)) * (Mathf.Tau / 4);
        ProcessCommon(delta);
    }
        
    public static EntityNode2D SpawnNode(LevelFile.BlockFile file) {
        var node = Global.Scene.Block.Instance<Block>();
        node.Type = file.Type;
        node.Shape = file.Shape;
        return node;
    }

    public class Ent : Entity {
        Block ThisNode => (Block)EntityNode;

        public BlockType BlockType => ThisNode.Type;

        public Ent(int id, Block node) : base(id, EntityType.Block) {
            EntityNode = node;
        }

        public override List<Vector3I> Shape() => ThisNode.Shape.Select(v => new Vector3I(v.x, v.y, 0)).ToList();

        public override bool IsFixed() => true;

        public override bool IsBlock(Vector3I dir) => true;

        public override bool IsRigid(Vector3I dir) => false;

        public override bool IsPushable(Vector3I dir) => false;

        public override EntityDef Def
        {
            get =>
                new EntityDef(Id, this, new LevelFile.BlockFile{
                    Type = ThisNode.Type,
                    Shape = ThisNode.Shape
                });
        }
    }
}