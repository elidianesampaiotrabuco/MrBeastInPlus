using BepInEx;
using MTM101BaldAPI.AssetTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Raldi
{
    public static class FileNameConfig
    {
        private static Dictionary<string, string> _fileMappings = new Dictionary<string, string>(StringComparer.Ordinal);
        private static bool _isLoaded = false;
        private static string _cachedConfigPath;
        private static string _cacheFilePath;

        private const string ITEMS_ARRAY_PATTERN = "\"items\":[";
        private static readonly string KeySearchString = "\"key\":";
        private static readonly string ValueSearchString = "\"value\":";

        public static void LoadConfig(BaseUnityPlugin plugin)
        {
            if (plugin == null) return;

            string configDir = Path.Combine(AssetLoader.GetModPath(plugin), "FileConfig");
            string configPath = Path.Combine(configDir, "FileNames.json");
            _cachedConfigPath = configPath;
            _cacheFilePath = Path.Combine(configDir, "FileNames.cache");

            if (!File.Exists(configPath)) return;

            try
            {
                if (TryLoadFromCache())
                {
                    _isLoaded = true;
                    return;
                }

                string json = File.ReadAllText(configPath);
                ParseJson(json, true);
                CreateCache(json);
                _isLoaded = true;
            }
            catch
            {
                // Silent error handling
            }
        }

        private static bool TryLoadFromCache()
        {
            try
            {
                if (!File.Exists(_cacheFilePath) || !File.Exists(_cachedConfigPath))
                    return false;

                string currentHash = CalculateFileHash(_cachedConfigPath);
                string[] cacheLines = File.ReadAllLines(_cacheFilePath);

                if (cacheLines.Length < 2 || currentHash != cacheLines[0])
                    return false;

                var sourceInfo = new FileInfo(_cachedConfigPath);
                var cacheInfo = new FileInfo(_cacheFilePath);

                if (sourceInfo.LastWriteTime > cacheInfo.LastWriteTime)
                    return false;

                ParseJson(cacheLines[1], false);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void CreateCache(string originalJson)
        {
            try
            {
                string minifiedJson = RemoveWhitespace(originalJson);
                string fileHash = CalculateFileHash(_cachedConfigPath);
                File.WriteAllText(_cacheFilePath, $"{fileHash}\n{minifiedJson}");
            }
            catch
            {
                // Cache is optional
            }
        }

        private static string CalculateFileHash(string filePath)
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            }
        }

        private static void ParseJson(string json, bool needsMinification)
        {
            _fileMappings.Clear();

            string cleanJson = needsMinification ? RemoveWhitespace(json) : json;

            int itemsStart = cleanJson.IndexOf(ITEMS_ARRAY_PATTERN, StringComparison.Ordinal);
            if (itemsStart == -1) return;

            itemsStart += ITEMS_ARRAY_PATTERN.Length;

            int itemsEnd = FindBracketEnd(cleanJson, itemsStart, ']');
            if (itemsEnd == -1) return;

            string itemsArray = cleanJson.Substring(itemsStart, itemsEnd - itemsStart);

            int position = 0;
            while (position < itemsArray.Length)
            {
                int itemStart = itemsArray.IndexOf('{', position);
                if (itemStart == -1) break;

                int itemEnd = FindBracketEnd(itemsArray, itemStart + 1, '}');
                if (itemEnd == -1) break;

                string itemJson = itemsArray.Substring(itemStart, itemEnd - itemStart + 1);
                ParseItem(itemJson);

                position = itemEnd + 1;
            }
        }

        private static int FindBracketEnd(string json, int startIndex, char bracketToFind)
        {
            bool inString = false;
            bool escaped = false;

            for (int i = startIndex; i < json.Length; i++)
            {
                char current = json[i];

                if (current == '\\')
                {
                    escaped = !escaped;
                    continue;
                }

                if (current == '"' && !escaped)
                {
                    inString = !inString;
                }

                if (!inString && current == bracketToFind)
                {
                    return i;
                }

                escaped = false;
            }

            return -1;
        }

        private static string RemoveWhitespace(string input)
        {
            var result = new StringBuilder(input.Length);
            bool inQuotes = false;
            bool escaped = false;

            foreach (char c in input)
            {
                if (c == '\\' && !escaped)
                {
                    escaped = true;
                    result.Append(c);
                    continue;
                }

                if (c == '"' && !escaped)
                {
                    inQuotes = !inQuotes;
                }

                if (inQuotes || !char.IsWhiteSpace(c))
                {
                    result.Append(c);
                }

                escaped = false;
            }

            return result.ToString();
        }

        private static void ParseItem(string itemJson)
        {
            string key = ExtractFieldValue(itemJson, "key");
            string value = ExtractFieldValue(itemJson, "value");

            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
            {
                _fileMappings[key] = value;
            }
        }

        private static string ExtractFieldValue(string json, string fieldName)
        {
            string searchString = fieldName == "key" ? KeySearchString : ValueSearchString;
            int start = json.IndexOf(searchString, StringComparison.Ordinal);
            if (start == -1) return null;

            start += searchString.Length;

            while (start < json.Length && char.IsWhiteSpace(json[start]))
            {
                start++;
            }

            if (start >= json.Length) return null;

            if (json[start] == '"')
            {
                start++;
                int end = start;
                bool escaped = false;

                while (end < json.Length)
                {
                    if (json[end] == '\\' && !escaped)
                    {
                        escaped = true;
                    }
                    else if (json[end] == '"' && !escaped)
                    {
                        break;
                    }
                    else
                    {
                        escaped = false;
                    }
                    end++;
                }

                if (end >= json.Length) return null;
                return json.Substring(start, end - start);
            }

            int end2 = start;
            while (end2 < json.Length && json[end2] != ',' && json[end2] != '}' && !char.IsWhiteSpace(json[end2]))
            {
                end2++;
            }

            return json.Substring(start, end2 - start);
        }

        public static string GetFileName(string key, string defaultFileName)
        {
            return _isLoaded && _fileMappings.TryGetValue(key, out string customName) ? customName : defaultFileName;
        }

        public static void ClearCache()
        {
            try
            {
                if (File.Exists(_cacheFilePath))
                    File.Delete(_cacheFilePath);
            }
            catch { }
        }
    }
}