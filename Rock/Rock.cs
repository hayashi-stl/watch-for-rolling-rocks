using Godot;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;

[Tool]
public partial class Rock : EntityNode2D
{
    Sprite _activeVisual;

    public enum RockType {
        LineChase,
    }

    RockType _type = RockType.LineChase;
    [Export]
    public RockType Type {
        get => _type;
        set {
            _type = value;
            if (_ready)
                UpdateTexture();
        }
    }

    public override Vector2I Size() => Vector2I.One * 2;

    public override Entity LevelEntity(int id) {
        return new Ent(id, this);
    }
        
	public override LevelFile.EntityCustomData LevelEntityCustomParams() {
        return new LevelFile.RockFile();
    }
        
    protected override void UpdateTexture() {
        _activeVisual.Visible = false;
        var visualName = Type switch {
            RockType.LineChase => "%LineChase",
            _ => throw new InvalidEnumArgumentException()
        };
        _activeVisual = GetNode<Sprite>(visualName);
        _activeVisual.Visible = true;
    }
            
            
    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        PrepareCommon();
        _activeVisual = GetNode<Sprite>("%LineChase");
    }


    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta) {
        if (Engine.EditorHint)
            Rotation = Mathf.Round(Rotation / (Mathf.Tau / 4)) * (Mathf.Tau / 4);
        ProcessCommon(delta);
    }
        
    public static EntityNode2D SpawnNode(LevelFile.RockFile file) {
        var node = Global.Scene.Rock.Instance<Rock>();
        node.Type = file.Type;
        return node;
    }

    public class Ent : Entity {
        Rock ThisNode => (Rock)EntityNode;

        public bool Moving { get; set; } = false;

        public Ent(int id, Rock node) : base(id, EntityType.Rock) {
            EntityNode = node;
        }

        public override bool IsFixed() => false;

        public override bool IsBlock(Vector3I dir) => true;

        public override bool IsRigid(Vector3I dir) => true;

        public override bool IsPushable(Vector3I dir) => false;

        public override EntityDef Def
        {
            get =>
                new EntityDef(Id, this, new LevelFile.RockFile{
                    Type = ThisNode.Type
                });
        }
    }
}

public partial class Level : Node2D
{
    void SetRockMovingUndoable(Rock.Ent rock, bool moving) {
        bool oldValue = rock.Moving;
        EditTypedEntityUndoable(rock, (r) => r.Moving = moving, (r) => r.Moving = oldValue);
    }

    void MoveRocks() {
        var rocks = _entriesByType[(int)Entity.EntityType.Rock].entities.Values.Select(e => (Rock.Ent)e).ToList();
        PrioritySort(rocks);
        foreach (var rock in rocks) {
            if (!rock.Moving) {
                // Detect players
                var rays = new List<(Vector3I Start, Vector3I Dir)>();
                for (int dx = 0; dx < rock.Size().x; ++dx) {
                    rays.Add((rock.Position + Util.DirUp + Util.DirRight * dx, Util.DirUp));
                    rays.Add((rock.Position + Util.DirDown * rock.Size().y + Util.DirRight * dx, Util.DirDown));
                }
                for (int dy = 0; dy < rock.Size().y; ++dy) {
                    rays.Add((rock.Position + Util.DirLeft + Util.DirDown * dy, Util.DirLeft));
                    rays.Add((rock.Position + Util.DirRight * rock.Size().x + Util.DirDown * dy, Util.DirRight));
                }

                foreach (var (Start, Dir) in rays) {
                    var entries = Raycast(Start, Dir);
                    if (entries.WithType(Entity.EntityType.Player).Any()) {
                        rock.SetDirection(Dir, false);
                        SetRockMovingUndoable(rock, true);
                        break;
                    }
                }
            }
        }

        foreach (var rock in rocks) {
            if (rock.Moving)
                Move(rock, rock.Direction, false);
        }

        BatchTweens();
        //foreach (var baddy in baddies)
        //    if (baddy.Alive) {
        //        var result = AttemptMove(baddy, baddy.Direction, false, false, false);
        //        if (baddy.Alive && result.Count == 0) {
        //            RotateEntityUndoable(baddy, -baddy.Direction);
        //            //_tweenGrouping.AddTween(new TweenSoundEffectEntry(Global.SFX.Swish, 0));
        //        }
        //        BatchTweens();
        //    }
    }
}