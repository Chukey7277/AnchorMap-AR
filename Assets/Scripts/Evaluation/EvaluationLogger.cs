using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class EvaluationLogger
{
    private static string _filePath;
    private static bool _initialized;

    private static readonly string _sessionId = Guid.NewGuid().ToString("N");

    public static string FilePath
    {
        get { InitIfNeeded(); return _filePath; }
    }

    public static string NewTapId() => Guid.NewGuid().ToString("N");
    public static string NewAnchorId() => Guid.NewGuid().ToString("N");

    private static void InitIfNeeded()
    {
        if (_initialized) return;

        string day = DateTime.Now.ToString("yyyy-MM-dd");
        string fileName = $"evaluation_{day}.csv";
        _filePath = Path.Combine(Application.persistentDataPath, fileName);

        try
        {
            if (!File.Exists(_filePath))
            {
                File.WriteAllText(
                    _filePath,
                    "timestamp_iso,session_id,event,mode,mapId,success,details," +
                    "x,y,z," +
                    "tap_id,anchor_id," +
                    "screen_x,screen_y,latency_ms," +
                    "hit_type,trackable_type,trackable_id," +
                    "earth_state,earth_tracking," +
                    "earth_hacc_m,earth_vacc_m,earth_headacc_deg\n",
                    Encoding.UTF8
                );
            }

            // ✅ mark initialized only after file is valid
            _initialized = true;

            // ✅ Write SessionStart directly (no Log() call)
            AppendRawLine(
                ev: "SessionStart",
                mode: "",
                mapId: "",
                success: "",
                details: SystemInfo.deviceModel
            );
        }
        catch (Exception e)
        {
            Debug.LogWarning("[EvaluationLogger] Failed to init log file: " + e.Message);
            _filePath = null;
            _initialized = false;
        }
    }

    public static void Log(
        string ev,
        string mode,
        string mapId,
        string success = "",
        string details = "",
        Vector3? pos = null,
        string tapId = "",
        string anchorId = "",
        Vector2? screenPos = null,
        double? latencyMs = null,
        string hitType = "",
        string trackableType = "",
        string trackableId = "",
        string earthState = "",
        string earthTracking = "",
        float? earthHAcc = null,
        float? earthVAcc = null,
        float? earthHeadAcc = null
    )
    {
        InitIfNeeded();
        if (string.IsNullOrEmpty(_filePath)) return;

        AppendRawLine(ev, mode, mapId, success, details, pos,
            tapId, anchorId, screenPos, latencyMs,
            hitType, trackableType, trackableId,
            earthState, earthTracking, earthHAcc, earthVAcc, earthHeadAcc);
    }

    private static void AppendRawLine(
        string ev,
        string mode,
        string mapId,
        string success = "",
        string details = "",
        Vector3? pos = null,
        string tapId = "",
        string anchorId = "",
        Vector2? screenPos = null,
        double? latencyMs = null,
        string hitType = "",
        string trackableType = "",
        string trackableId = "",
        string earthState = "",
        string earthTracking = "",
        float? earthHAcc = null,
        float? earthVAcc = null,
        float? earthHeadAcc = null
    )
    {
        string ts = DateTime.Now.ToString("o");

        string x = "", y = "", z = "";
        if (pos.HasValue)
        {
            Vector3 p = pos.Value;
            x = p.x.ToString("F6");
            y = p.y.ToString("F6");
            z = p.z.ToString("F6");
        }

        string sx = "", sy = "";
        if (screenPos.HasValue)
        {
            Vector2 sp = screenPos.Value;
            sx = sp.x.ToString("F2");
            sy = sp.y.ToString("F2");
        }

        string lat = latencyMs.HasValue ? latencyMs.Value.ToString("F1") : "";

        string hacc = earthHAcc.HasValue ? earthHAcc.Value.ToString("F2") : "";
        string vacc = earthVAcc.HasValue ? earthVAcc.Value.ToString("F2") : "";
        string headacc = earthHeadAcc.HasValue ? earthHeadAcc.Value.ToString("F2") : "";

        string Safe(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Contains(",") || s.Contains("\n") || s.Contains("\""))
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }

        string line =
            $"{Safe(ts)},{Safe(_sessionId)},{Safe(ev)},{Safe(mode)},{Safe(mapId)},{Safe(success)},{Safe(details)}," +
            $"{x},{y},{z}," +
            $"{Safe(tapId)},{Safe(anchorId)}," +
            $"{sx},{sy},{lat}," +
            $"{Safe(hitType)},{Safe(trackableType)},{Safe(trackableId)}," +
            $"{Safe(earthState)},{Safe(earthTracking)}," +
            $"{hacc},{vacc},{headacc}\n";

        try
        {
            File.AppendAllText(_filePath, line, Encoding.UTF8);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[EvaluationLogger] Failed to write log line: " + e.Message);
        }
    }
}