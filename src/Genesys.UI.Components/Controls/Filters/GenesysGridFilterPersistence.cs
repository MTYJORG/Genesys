using Newtonsoft.Json;
using System;
using System.IO;

namespace Genesys.UI.Components.Controls.Filters
{
    public class GenesysGridFilterPersistence
    {
        private readonly string basePath;

        public GenesysGridFilterPersistence()
        {
            basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Genesys",
                "Filters");
        }

        public void Save(string key, GenesysGridFilterState state)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            if (state == null)
                return;

            Directory.CreateDirectory(basePath);

            string path = Path.Combine(basePath, MakeSafeFileName(key) + ".json");
            string json = JsonConvert.SerializeObject(state, Formatting.Indented);

            File.WriteAllText(path, json);
        }

        public GenesysGridFilterState Load(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            string path = Path.Combine(basePath, MakeSafeFileName(key) + ".json");

            if (!File.Exists(path))
                return null;

            string json = File.ReadAllText(path);

            return JsonConvert.DeserializeObject<GenesysGridFilterState>(json);
        }

        private string MakeSafeFileName(string value)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(c, '_');
            }

            return value;
        }
    }
}