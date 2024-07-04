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
        DirLineChase,
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

    bool _moving = false;
    public bool Moving {
        get => _moving;
        set {
            _moving = value;
            if (_ready)
                UpdateTexture();
        }
    }

    protected override Vector2 NaturalOffsetPosition => Vector2.One * Util.TileSize / 2;

    public override Entity LevelEntity(int id) {
        return new Ent(id, this);
    }
        
	public override LevelFile.EntityCustomData LevelEntityCustomParams() {
        return new LevelFile.RockFile() {
            Type = Type
        };
    }
        
    protected override void UpdateTexture() {
        _activeVisual.Visible = false;
        var visualName = (Type, Moving) switch {
            (RockType.LineChase, false) => "%LineChase",
            (RockType.LineChase, true) => "%LineChaseMoving",
            (RockType.DirLineChase, false) => "%DirLineChase",
            (RockType.DirLineChase, true) => "%DirLineChaseMoving",
            _ => throw new InvalidEnumArgumentException()
        };
        _activeVisual = GetNode<Sprite>(visualName);
        _activeVisual.Visible = true;
    }
            
            
    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        PrepareCommon();
        _activeVisual = GetNode<Sprite>("%LineChase");
        UpdateTexture();
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

        public RockType RockType => ThisNode.Type;

        public override List<Vector3I> Shape() => new List<Vector3I>() {
            new Vector3I(0, 0, 0), new Vector3I(1, 0, 0),
            new Vector3I(0, 1, 0), new Vector3I(1, 1, 0),
        };

        public int SideLength => 2;

        public bool Moving {
            get => ThisNode.Moving;
            set => ThisNode.Moving = value;
        }

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

        public bool CanDetectPlayerInDir(Vector3I dir) {
            return RockType switch {
                RockType.LineChase => true,
                RockType.DirLineChase => Math.Abs(dir.Dot(Direction)) > 0,
                _ => throw new InvalidEnumArgumentException()
            };
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
                for (int dx = 0; dx < rock.SideLength; ++dx) {
                    rays.Add((rock.Position + Util.DirUp + Util.DirRight * dx, Util.DirUp));
                    rays.Add((rock.Position + Util.DirDown * rock.SideLength + Util.DirRight * dx, Util.DirDown));
                }
                for (int dy = 0; dy < rock.SideLength; ++dy) {
                    rays.Add((rock.Position + Util.DirLeft + Util.DirDown * dy, Util.DirLeft));
                    rays.Add((rock.Position + Util.DirRight * rock.SideLength + Util.DirDown * dy, Util.DirRight));
                }

                foreach (var (Start, Dir) in rays) {
                    if (rock.CanDetectPlayerInDir(Dir)) {
                        var entries = RaycastToPlayer(Start, Dir);
                        if (entries != null && entries.WithType(Entity.EntityType.Player).Any()) {
                            RotateEntityUndoable(rock, Dir);
                            SetRockMovingUndoable(rock, true);
                            break;
                        }
                    }
                }
            }
        }
    }

    List<(Entity HitEntity, float Time)> RockCollisions(Rock.Ent rock) {
        var collisions = new List<(Entity HitEntity, float Time)>();
        if (!rock.Moving)
            return collisions;

        foreach (Vector3I offset in rock.Shape()) {
            var pos = rock.Position + offset;
            var entry = EntryAt(pos);
            var rocks = entry.WithType(Entity.EntityType.Rock).Select(e => (Rock.Ent)e).ToList();
            
            if (entry.entities.Values.Where(e => e.IsRigid(-rock.Direction) && e.Type != Entity.EntityType.Rock).FirstOrDefault() is Entity e &&
                !rock.IncludesTile(pos + rock.Direction))
                collisions.Add((e, 0.0f));

            foreach (var en in entry.entities.Values.Where(e => e is Block.Ent b && b.BlockType == Block.BlockType.Brittle && b.Shape().Count >= rock.SideLength * rock.SideLength))
                collisions.Add((en, 0.05f));

            foreach (var other in rocks.Where(r => !r.Moving))
                collisions.Add((other, 0.0f));

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

    class RockCollisionResult {
        public List<Vector3I> overlappedPositions; // two rocks may bump into each other with 1 tile in between
        public List<Entity> extraDestroyedBlocks; // in case block size = rock size
    }

    RockCollisionResult HandleRockCollision(IEnumerable<Rock.Ent> rocks) {
        var collisionRocks = new SimplePriorityQueue<(Rock.Ent Rock, Entity HitEntity, float Time)>();
        foreach (var rock in rocks) {
            var collisions = RockCollisions(rock);
            foreach  (var (hitEntity, time) in collisions) {
                collisionRocks.Enqueue((rock, hitEntity, time), time);
            }
        }

        var overlappedPositions = new HashSet<Vector3I>();
        var extraDestroyedBlocks = new HashSet<Entity>();

        while (collisionRocks.Any()) {
            var (_, _, time) = collisionRocks.First();
            var rockPairs = new List<(Rock.Ent Rock, Entity HitRock)>();
            while (collisionRocks.Any() && collisionRocks.First().Time == time) {
                var (rock, hitEntity, _) = collisionRocks.Dequeue();
                rockPairs.Add((rock, hitEntity));
            }

            var rocksToMove = rockPairs.SelectMany(pair => {
                var (rock, hitEntity) = pair;
                return !rock.Moving ? new List<(Rock.Ent Rock, Entity HitEntity)>() :
                    rock.Intersects(hitEntity) ? new List<(Rock.Ent Rock, Entity HitEntity)>(){ (rock, hitEntity) }
                        : new List<(Rock.Ent Rock, Entity HitEntity)>();
            }).ToList();
            foreach (var (rock, hitEntity) in rocksToMove) {
                if (rock.Moving) {
                    // The space between blocks hitting each other with 1 space apart isn't safe!
                    if (time >= 0.5)
                        foreach (var rockPos in rock.Positions())    
                            overlappedPositions.Add(rockPos);

                    Move(rock, -rock.Direction, false);
                    SetRockMovingUndoable(rock, false);
                    _tweenGrouping.AddTween(new TweenEntityBumpPositionEntry(rock, (Vector2)rock.Direction.XY * Util.TileSize * (0.15f + time)));

                    if (hitEntity is Block.Ent b && b.BlockType == Block.BlockType.Brittle && b.Shape().Count == rock.SideLength * rock.SideLength)
                        extraDestroyedBlocks.Add(hitEntity);

                    // Have intersecting rocks check for collisions.
                    // This rock already moved back, so it can't move again, but it may cause
                    // other rocks to move back via a chain reaction.
                    var intersectingRocks = EntriesAt(rock.Positions()).SelectMany(entry => entry.entities.Values)
                        .Where(e => e is Rock.Ent)
                        .Select(r => (Rock.Ent)r);
                    foreach (var intersectingRock in intersectingRocks) {
                        foreach (var (hitRock, time_) in RockCollisions(intersectingRock))
                            collisionRocks.Enqueue((intersectingRock, hitRock, time_), time_);
                    }
                }
            }
        }

        foreach (var pos in rocks.Where(r => r.Moving).SelectMany(r => r.Positions()))
            overlappedPositions.Add(pos);

        return new RockCollisionResult(){
            overlappedPositions = overlappedPositions.ToList(),
            extraDestroyedBlocks = extraDestroyedBlocks.ToList(),
        };
    }

    void MoveRocks() {
        var rocks = _entriesByType[(int)Entity.EntityType.Rock].entities.Values.Select(e => (Rock.Ent)e).ToList();
        PrioritySort(rocks);
        RocksDetectPlayers(rocks);

        foreach (var rock in rocks) {
            if (rock.Moving)
                Move(rock, rock.Direction, false);
        }

        var result = HandleRockCollision(rocks);
        HandleRockDestruction(result);

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