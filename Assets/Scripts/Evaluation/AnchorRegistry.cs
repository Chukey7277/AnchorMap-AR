using System.Collections.Generic;
using UnityEngine;

public static class AnchorRegistry
{
    public class Record
    {
        public string anchorId;
        public string mode;     // "Indoor" or "Outdoor"
        public string mapId;    // for indoor; "" for outdoor

        public Transform target;     // the thing we track over time
        public Transform mapSpace;   // only for indoor (can be null for outdoor)

        public Vector3 initialWorldPos;
        public Vector3 initialMapLocalPos; // only meaningful for indoor
    }

    private static readonly Dictionary<string, Record> _records = new Dictionary<string, Record>();

    public static IReadOnlyDictionary<string, Record> Records => _records;

    public static void Register(Record r)
    {
        if (r == null || string.IsNullOrEmpty(r.anchorId) || r.target == null) return;
        _records[r.anchorId] = r;
    }

    public static void Unregister(string anchorId)
    {
        if (string.IsNullOrEmpty(anchorId)) return;
        _records.Remove(anchorId);
    }
}