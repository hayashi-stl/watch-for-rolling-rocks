using Godot;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using Priority_Queue;

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

    void RocksDetectPlayers(IEnumerable<Rock.Ent> rocks) {
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
                        RotateEntityUndoable(rock, Dir);
                        SetRockMovingUndoable(rock, true);
                        break;
                    }
                }
            }
        }
    }

    IEnumerable<Rock.Ent> RocksThatMoveBack(Vector3I pos) {
        var entry = EntryAt(pos);
        var rocks = entry.WithType(Entity.EntityType.Rock).Select(e => (Rock.Ent)e).ToList();
        if (entry.HasFixedBlock() || rocks.Exists(r => !r.Moving))
            return rocks.Where(r => r.Moving);

        var rocksToMove = new HashSet<Rock.Ent>();
        for (int i = 0; i < rocks.Count; ++i)
            foreach (var other in rocks.Skip(i + 1)) {
                var rock = rocks[i];
                // Opposite directions: always move back
                if (rock.Direction.Dot(other.Direction) < 0) {
                    rocksToMove.Add(rock);
                    rocksToMove.Add(other);
                }
                // Perpendicular directions: move back if would reach intersection point at same time
                else if (rock.Direction.Dot(other.Direction) == 0) {
                    var horzRock = rock.Direction.x == 0 ? other : rock;
                    var vertRock = rock.Direction.x == 0 ? rock : other;
                    var intersectPos = new Vector3I(vertRock.Position.x, horzRock.Position.y, pos.z);
                    var horzDistance = (horzRock.Position - intersectPos).Dot(horzRock.Direction);
                    var vertDistance = (vertRock.Position - intersectPos).Dot(vertRock.Direction);
                    if (horzDistance <= vertDistance)
                        rocksToMove.Add(horzRock);
                    if (vertDistance <= horzDistance)
                        rocksToMove.Add(vertRock);
                }
            }

        return rocksToMove;
    }

    void HandleRockCollision(IEnumerable<Rock.Ent> rocks) {
        var collisionTiles = new SimplePriorityQueue<Vector3I>();
        foreach (var rock in rocks) {
            for (int y = 0; y < rock.Size().y; ++y)
                for (int x = 0; x < rock.Size().x; ++x) {
                    var pos = rock.Position + new Vector3I(x, y, 0);
                    collisionTiles.Enqueue(pos, RocksThatMoveBack(pos).Count());
                }
        }

        while (collisionTiles.Any()) {
            var pos = collisionTiles.Dequeue();
            var rocksToMove = RocksThatMoveBack(pos);
            foreach (var rock in rocksToMove) {
                Move(rock, -rock.Direction, false);
                SetRockMovingUndoable(rock, false);

                for (int y = 0; y < rock.Size().y; ++y)
                    for (int x = 0; x < rock.Size().x; ++x) {
                        var dPos = rock.Position + new Vector3I(x, y, 0);
                        collisionTiles.Enqueue(dPos, RocksThatMoveBack(dPos).Count());
                    }
            }
        }
    }

    void MoveRocks() {
        var rocks = _entriesByType[(int)Entity.EntityType.Rock].entities.Values.Select(e => (Rock.Ent)e).ToList();
        PrioritySort(rocks);
        RocksDetectPlayers(rocks);

        foreach (var rock in rocks) {
            if (rock.Moving)
                Move(rock, rock.Direction, false);
        }

        HandleRockCollision(rocks);

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