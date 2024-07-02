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

    public override Entity LevelEntity(int id) {
        return new Ent(id, this);
    }
        
	public override LevelFile.EntityCustomData LevelEntityCustomParams() {
        return new LevelFile.BlockFile();
    }
        
    protected override void UpdateTexture() {
        _activeVisual.Visible = false;
        var visualName = Type switch {
            BlockType.Brittle => "%Brittle",
            _ => throw new InvalidEnumArgumentException()
        };
        _activeVisual = GetNode<Sprite>(visualName);
        _activeVisual.Visible = true;
    }
            
            
    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        PrepareCommon();
        _activeVisual = GetNode<Sprite>("%Brittle");
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
        return node;
    }

    public class Ent : Entity {
        Block ThisNode => (Block)EntityNode;

        public bool Moving { get; set; } = false;

        public Ent(int id, Block node) : base(id, EntityType.Block) {
            EntityNode = node;
        }

        public override bool IsFixed() => true;

        public override bool IsBlock(Vector3I dir) => true;

        public override bool IsRigid(Vector3I dir) => false;

        public override bool IsPushable(Vector3I dir) => false;

        public override EntityDef Def
        {
            get =>
                new EntityDef(Id, this, new LevelFile.BlockFile{
                    Type = ThisNode.Type
                });
        }
    }
}