using Godot;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using Priority_Queue;

[Tool]
public partial class Stairs : EntityNode2D
{
    Sprite _activeVisual;

    public override Vector2I Size() => Vector2I.One;

    public override Entity LevelEntity(int id) {
        return new Ent(id, this);
    }
        
	public override LevelFile.EntityCustomData LevelEntityCustomParams() {
        return new LevelFile.StairsFile();
    }
        
    protected override void UpdateTexture() {
    }
            
            
    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        PrepareCommon();
        _activeVisual = GetNode<Sprite>("%Stairs");
    }


    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta) {
        if (Engine.EditorHint)
            Rotation = Mathf.Round(Rotation / (Mathf.Tau / 4)) * (Mathf.Tau / 4);
        ProcessCommon(delta);
    }
        
    public static EntityNode2D SpawnNode(LevelFile.StairsFile file) {
        var node = Global.Scene.Stairs.Instance<Stairs>();
        return node;
    }

    public class Ent : Entity {
        Stairs ThisNode => (Stairs)EntityNode;

        public bool Moving { get; set; } = false;

        public Ent(int id, Stairs node) : base(id, EntityType.Stairs) {
            EntityNode = node;
        }

        public override bool IsFixed() => true;

        public override bool IsBlock(Vector3I dir) => dir != Direction;

        public override bool IsRigid(Vector3I dir) => IsBlock(dir);

        public override bool IsPushable(Vector3I dir) => false;

        public override EntityDef Def
        {
            get =>
                new EntityDef(Id, this, new LevelFile.StairsFile{

                });
        }
    }
}