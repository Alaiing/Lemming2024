using System.Collections.Generic;

namespace Oudidon
{
    public static class ConfigManager
    {
        private static readonly Dictionary<string, string> config = new Dictionary<string, string>();

        public static void LoadConfig(string path)
        {
            config.Clear();

            if (System.IO.File.Exists(path))
            {
                string[] lines = System.IO.File.ReadAllLines(path);
                foreach (string line in lines)
                {
                    if (!line.StartsWith("--")) // comment line
                    {
                        string[] split = line.Split('=');
                        if (split.Length == 2)
                        {
                            if (config.ContainsKey(split[0]))
                            {
                                config[split[0]] = split[1];
                            }
                            else
                            {
                                config.Add(split[0].Trim(), split[1].Trim());
                            }
                        }
                    }
                }
            }
        }

        public static int GetConfig(string key, int defaulValue)
        {
            if (config.ContainsKey(key))
            {
                try
                {
                    return int.Parse(config[key]);
                }
                catch
                {
                    return defaulValue;
                }
            }

            return defaulValue;
        }

        public static float GetConfig(string key, float defaultValue)
        {
            if (config.ContainsKey(key))
            {
                try
                {
                    return float.Parse(config[key]);
                }
                catch
                {
                    return defaultValue;
                }
            }

            return defaultValue;
        }

        public static string GetConfig(string key, string defaultValue = null)
        {
            if (config.ContainsKey(key))
            {
                return config[key];
            }

            return defaultValue;
        }

        public static bool GetConfig(string key, bool defaultValue = false)
        {
            if (config.ContainsKey(key))
            {
                try
                {
                    return bool.Parse(config[key]);
                }
                catch
                {
                    return defaultValue;
                }
            }

            return defaultValue;
        }

    }
}
