using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Vrc.OscQuery
{
    public class OscQueryRootNode : OscQueryNode
    {
        private Dictionary<string, OscQueryNode> myPathLookup;

        public OscQueryRootNode()
        {
            // Initialize path lookup with self reference
            myPathLookup = new Dictionary<string, OscQueryNode>()
            {
                {"/", this}
            };
            
        }
        public OscQueryNode? GetNodeWithPath(string path)
        {
            return myPathLookup.GetValueOrDefault(path);
        }
        
        public OscQueryNode AddNode(OscQueryNode node)
        {
            // Todo: parse path and figure out which sub-node to add it to
            var parent = GetNodeWithPath(node.ParentPath) ?? AddNode(new OscQueryNode(node.ParentPath));

            // Add to contents
            parent.Contents.Add(node.Name, node);
            
            // Todo: handle case where this full path already exists, but I don't think it should ever happen
            myPathLookup.Add(node.FullPath, node);
            
            return node;
        }

        public bool RemoveNode(string path)
        {
            if (!myPathLookup.TryGetValue(path, out var node)) return false;
            var parent = GetNodeWithPath(node.ParentPath);
            if (parent?.Contents.ContainsKey(node.Name) != true) return false;
            parent.Contents.Remove(node.Name);
            myPathLookup.Remove(path);
            return true;
        }

        public void RebuildLookup()
        {
            myPathLookup = new Dictionary<string, OscQueryNode>()
            {
                { "/", this },
            };
            AddContents(this);
        }

        /// <summary>
        /// Recursive Function to rebuild Lookup
        /// </summary>
        /// <param name="node"></param>
        public void AddContents(OscQueryNode node)
        {
            foreach (var subNode in node.Contents.Select(pair => pair.Value))
            {
                myPathLookup.Add(subNode.FullPath, subNode);
                AddContents(subNode);
            }
        }
        
        public static OscQueryRootNode? FromString(string json)
        {
            var tree = JsonSerializer.Deserialize(json, GeneratedJsonSerializers.Default.OscQueryRootNode);
            tree?.RebuildLookup();
            return tree;
        }
        
        public static async ValueTask<OscQueryRootNode?> FromStreamAsync(Stream json, CancellationToken cancellationToken = default)
        {
            var tree = await JsonSerializer.DeserializeAsync(json, GeneratedJsonSerializers.Default.OscQueryRootNode, cancellationToken);
            tree?.RebuildLookup();
            return tree;
        }
    }
    public class OscQueryNode
    {
        // Empty Constructor for Json Serialization
        public OscQueryNode(){}

        public OscQueryNode(string fullPath)
        {
            FullPath = fullPath;
        }
        
        [JsonPropertyName(Attributes.DESCRIPTION)]
        public string? Description;
        
        [JsonPropertyName(Attributes.FULL_PATH)]
        public string FullPath = "";
        
        [JsonPropertyName(Attributes.ACCESS)]
        public Attributes.AccessValues Access;
        
        [JsonPropertyName(Attributes.CONTENTS)]
        public Dictionary<string, OscQueryNode> Contents = new();
        
        [JsonPropertyName(Attributes.TYPE)]
        public string? OscType;

        private object[]? myValue;
        [JsonPropertyName(Attributes.VALUE)]
        public object[]? Value
        {
            get => ValueGetter != null ? ValueGetter(this) : myValue;
            set => myValue = value;
        }

        [JsonIgnore]
        public Func<OscQueryNode, object[]?>? ValueGetter;

        [JsonPropertyName(Attributes.RANGE)]
        public OscRange[]? Range;
        
        [JsonIgnore]
        public string ParentPath {
            get
            {
                var length = Math.Max(1, FullPath.LastIndexOf('/'));
                return FullPath.Substring(0, length);
            }
        }
        
        [JsonIgnore]
        public string Name => FullPath.Substring(FullPath.LastIndexOf('/')+1);

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, GeneratedJsonSerializers.Default.OscQueryNode);
        }
        
    }

    public class OscRange
    {
        [JsonPropertyName("MIN")]
        public object? Min;
        
        [JsonPropertyName("MAX")]
        public object? Max;
        
        [JsonPropertyName("VALS")]
        public object[]? Values;
    }
}