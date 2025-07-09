using System;

public static partial class JsonHelper
{
    public static string ExtractArray(string json, string key)
    {
        int start = json.IndexOf(key) + key.Length + 2;
        if (start < key.Length + 2 || start >= json.Length)
            return "[]";

        int end = json.IndexOf("]", start) + 1;
        if (end <= start)
            return "[]";

        return json.Substring(start, end - start);
    }
}
