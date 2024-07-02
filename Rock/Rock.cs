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

    // HitRock can be null to indicate that another type of block was hit
    List<(Rock.Ent Rock, Rock.Ent HitRock, float Time)> RockCollisions(Rock.Ent rock) {
        var collisions = new List<(Rock.Ent Rock, Rock.Ent HitRock, float Time)>();
        if (!rock.Moving)
            return collisions;

        for (int y = 0; y < rock.Size().y; ++y)
            for (int x = 0; x < rock.Size().x; ++x) {
                var pos = rock.Position + new Vector3I(x, y, 0);
                var entry = EntryAt(pos);
                var rocks = entry.WithType(Entity.EntityType.Rock).Select(e => (Rock.Ent)e).ToList();
                
                if (entry.HasRigidEntity(-rock.Direction) || rocks.Exists(r => !r.Moving))
                    collisions.Add((null, 0.0f));

                foreach (var other in rocks.Where(r => r != rock)) {
                    // Opposite directions: always move back
                    if (rock.Direction.Dot(other.Direction) < 0) {
                        var supp1 = rock.SupportVector(rock.Direction);
                        var supp2 = other.SupportVector(other.Direction);
                        float distance = (supp2 - supp1).Dot(rock.Direction);
                        // 0 => 1, -2 => 0
                        collisions.Add((other, distance * 0.5f + 1));
                    }
                    // Perpendicular directions: move back if would reach intersection point at same time
                    // TODO: This is generally wrong. It should be if their corners touch.
                    else if (rock.Direction.Dot(other.Direction) == 0) {
                        var horzRock = rock.Direction.x == 0 ? other : rock;
                        var vertRock = rock.Direction.x == 0 ? rock : other;
                        var intersectPos = new Vector3I(vertRock.Position.x, horzRock.Position.y, pos.z);
                        var horzDistance = (horzRock.Position - intersectPos).Dot(horzRock.Direction);
                        var vertDistance = (vertRock.Position - intersectPos).Dot(vertRock.Direction);
                        if (rock == horzRock ? horzDistance <= vertDistance : vertDistance <= horzDistance)
                            collisions.Add((other, horzDistance == vertDistance ? 0.1f : 0.0f));
                    }
                }
            }

        return collisions;
    }

    void HandleRockCollision(IEnumerable<Rock.Ent> rocks) {
        var collisionRocks = new SimplePriorityQueue<(Rock.Ent Rock, Rock.Ent HitRock, float Time)>();
        foreach (var rock in rocks) {
            var collisions = RockCollisions(rock);
            foreach  (var (hitRock, time) in collisions)
                collisionRocks.Enqueue((rock, hitRock, time), time);
        }

        while (collisionRocks.Any()) {
            var (_, _, time) = collisionRocks.First();
            var rockPairs = new List<(Rock.Ent Rock, Rock.Ent HitRock)>();
            while (collisionRocks.Any() && collisionRocks.First().Time == time) {
                var (rock, hitRock, _) = collisionRocks.Dequeue();
                rockPairs.Add((rock, hitRock));
            }

            var rocksToMove = rockPairs.SelectMany(pair => {
                var (rock, hitRock) = pair;
                return !rock.Moving ? new List<Rock.Ent>() :
                    hitRock == null ? new List<Rock.Ent>(){ rock } :
                    hitRock.Moving ? new List<Rock.Ent>(){ rock } :
                    // TODO: Do they still intersect
                    new List<Rock.Ent>();
            }).ToList();
            foreach (var rock in rocksToMove) {
                if (rock.Moving) {
                    Move(rock, -rock.Direction, false);
                    SetRockMovingUndoable(rock, false);

                    // TODO: Add backward collisions
                    var collisions = RockCollisions(rock);
                    foreach  (var (hitRock, time_) in collisions)
                        collisionRocks.Enqueue((rock, hitRock, time_), time_);
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
        HandlePlayerRockCollision();

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