using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using JsonSubTypes;
using System.IO;
using System.Text;
using Newtonsoft.Json.Converters;

// All coordinates are (horizontal, vertical, layer)
public class LevelFile
{
    public abstract class EntityCustomData {
        public static JsonConverter Converter() {
            return JsonSubtypesConverterBuilder
                .Of<EntityCustomData>("$Type")
                .RegisterSubtype<PlayerFile>("Player")
                .RegisterSubtype<RockFile>("Rock")
                .RegisterSubtype<BlockFile>("Block")
                .RegisterSubtype<StairsFile>("Stairs")
                .SerializeDiscriminatorProperty()
                .Build();
        }
    }

    public class PlayerFile : EntityCustomData {}
    public class RockFile : EntityCustomData {
        [JsonConverter(typeof(StringEnumConverter))]
        public Rock.RockType Type { get; set; }
    }
    public class BlockFile : EntityCustomData {
        [JsonConverter(typeof(StringEnumConverter))]
        public Block.BlockType Type { get; set; }
        [JsonConverter(typeof(BlockShapeConverter))]
        public List<Vector2I> Shape { get; set; } = new List<Vector2I>(){ Vector2I.Zero };
    }
    public class StairsFile : EntityCustomData {}

    public class EntityFile
    {
        public Vector3I Position { get; set; }
        public Vector3I Direction { get; set; }
        public Vector3I Gravity { get; set; }
        public int? CounterValue { get; set; }
        public EntityCustomData CustomData { get; set; }
    }

    public string Name { get; set; }
    public string Controls { get; set; } = "";
    public Vector3I Base { get; set; } // minimum coordinates
    public Vector3I Size { get; set; }
    public List<int> Map { get; set; } // row-major then plane-major
    public List<EntityFile> Entities { get; set; }

    public class Tile {
        public const int Invalid = 0;
        public const int Block = 1;
        public const int Spikes = 2;
    }

    public class Vector2IJsonConverter : JsonConverter<Vector2I>
    {
        public override Vector2I ReadJson(JsonReader reader, Type objectType, Vector2I existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var arr = serializer.Deserialize<int[]>(reader);
            return new Vector2I(arr[0], arr[1]);
        }

        public override void WriteJson(JsonWriter writer, Vector2I value, JsonSerializer serializer)
        {
            writer.WriteRawValue($"[{value.x}, {value.y}]");
        }
    }

    public class BlockShapeConverter : JsonConverter<List<Vector2I>>
    {
        public override List<Vector2I> ReadJson(JsonReader reader, Type objectType, List<Vector2I> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return serializer.Deserialize<List<Vector2I>>(reader);
        }

        public override void WriteJson(JsonWriter writer, List<Vector2I> value, JsonSerializer serializer)
        {
            var str = "[" + string.Join(", ", value.Select(v => $"[{v.x}, {v.y}]")) + "]";
            writer.WriteRawValue(str);
        }
    }

    public class Vector3IJsonConverter : JsonConverter<Vector3I>
    {
        public override Vector3I ReadJson(JsonReader reader, Type objectType, Vector3I existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var arr = serializer.Deserialize<int[]>(reader);
            return new Vector3I(arr[0], arr[1], arr[2]);
        }

        public override void WriteJson(JsonWriter writer, Vector3I value, JsonSerializer serializer)
        {
            writer.WriteRawValue($"[{value.x}, {value.y}, {value.z}]");
        }
    }

    public class SerialMapJsonConverter : JsonConverter<List<int>>
    {
        public Vector3I Size { private get; set; }

        public override List<int> ReadJson(JsonReader reader, Type objectType, List<int> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return serializer.Deserialize<List<int>>(reader);
        }

        public override void WriteJson(JsonWriter writer, List<int> value, JsonSerializer serializer)
        {
            var indent = string.Join("", Enumerable.Repeat("  ", 2));
            var str = string.Join($",\n\n{indent}  ", Enumerable.Range(0, Size.z).Select(z =>
                string.Join($",\n{indent}  ", Enumerable.Range(0, Size.y).Select(y =>
                    string.Join(", ", Enumerable.Range(0, Size.x).Select(x =>
                        value[(z * Size.y + y) * Size.x + x].ToString()))))));
            writer.WriteRawValue($"[\n{indent}  {str}\n{indent}]");
        }
    }

    public string ToJson() {
        var serializer = new JsonSerializer { Formatting = Formatting.Indented };
        serializer.Converters.Add(new Vector2IJsonConverter());
        serializer.Converters.Add(new Vector3IJsonConverter());
        serializer.Converters.Add(new SerialMapJsonConverter{ Size = Size });
        serializer.Converters.Add(EntityCustomData.Converter());
        var stream = new MemoryStream();
        using var streamWriter = new StreamWriter(stream);
        using var jsonWriter = new JsonTextWriter(streamWriter);
        serializer.Serialize(jsonWriter, this);
        jsonWriter.Flush();
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static LevelFile FromJson(string json) {
        var serializer = new JsonSerializer { Formatting = Formatting.Indented };
        serializer.Converters.Add(new Vector2IJsonConverter());
        serializer.Converters.Add(new Vector3IJsonConverter());
        serializer.Converters.Add(EntityCustomData.Converter());
        var jsonReader = new JsonTextReader(new StringReader(json));
        return serializer.Deserialize<LevelFile>(jsonReader);
    }

    public static LevelFile FromJson(Resource json) => FromJson((string)json.Get("text"));

    public static LevelFile Read(string filename) {
        var file = new Godot.File();
        file.Open(filename, Godot.File.ModeFlags.Read);
        var json = file.GetAsText();
        file.Close();
        return FromJson(json);
    }

    public void Save(string filename) {
        var file = new Godot.File();
        file.Open(filename, Godot.File.ModeFlags.Write);
        var json = ToJson();
        file.StoreString(json);
        file.Close();
    }

    // Rebase the level so that the minimum coordinates are [0, 0, 0]
    public void Rebase() {
        var offset = -Base;
        Base = Vector3I.Zero;
        foreach (var entity in Entities) {
            entity.Position += offset;
        }
    }
}