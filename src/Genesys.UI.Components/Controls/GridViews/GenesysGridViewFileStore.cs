using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Genesys.UI.Components.Controls.GridViews
{
    public class GenesysGridViewFileStore : IGenesysGridViewStore, IGenesysGridViewStateStore
    {
        private readonly string rootPath;

        public GenesysGridViewFileStore()
            : this(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Genesys",
                "GridViews"))
        {
        }

        public GenesysGridViewFileStore(string rootPath)
        {
            this.rootPath = rootPath;

            if (!Directory.Exists(this.rootPath))
                Directory.CreateDirectory(this.rootPath);
        }

        public IList<GenesysGridViewLayout> Load(string gridKey)
        {
            string folder = GetGridFolder(gridKey);

            if (!Directory.Exists(folder))
                return new List<GenesysGridViewLayout>();

            var result = new List<GenesysGridViewLayout>();

            foreach (string file in Directory.GetFiles(folder, "*.xml"))
            {
                if (string.Equals(Path.GetFileName(file), "__state.xml", StringComparison.OrdinalIgnoreCase))
                    continue;

                GenesysGridViewLayout layout = Read(file);

                if (layout != null)
                    result.Add(layout);
            }

            return result
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.ViewName)
                .ToList();
        }

        public void Save(GenesysGridViewLayout layout)
        {
            if (layout == null)
                throw new ArgumentNullException("layout");

            if (string.IsNullOrWhiteSpace(layout.GridKey))
                throw new ArgumentException("GridKey requerido.");

            if (string.IsNullOrWhiteSpace(layout.ViewName))
                throw new ArgumentException("ViewName requerido.");

            string folder = GetGridFolder(layout.GridKey);

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            layout.ModifiedAt = DateTime.Now;

            if (layout.CreatedAt == DateTime.MinValue)
                layout.CreatedAt = DateTime.Now;

            string path = Path.Combine(folder, SafeFileName(layout.ViewName) + ".xml");
            Write(path, layout);
        }

        public void Delete(string gridKey, string viewName)
        {
            string folder = GetGridFolder(gridKey);
            string path = Path.Combine(folder, SafeFileName(viewName) + ".xml");

            if (File.Exists(path))
                File.Delete(path);
        }


        public string LoadCurrentViewName(string gridKey)
        {
            string path = GetStatePath(gridKey);

            if (!File.Exists(path))
                return null;

            try
            {
                var serializer = new XmlSerializer(typeof(GenesysGridViewState));
                using (var stream = File.OpenRead(path))
                {
                    GenesysGridViewState state = serializer.Deserialize(stream) as GenesysGridViewState;
                    return state == null ? null : state.CurrentViewName;
                }
            }
            catch
            {
                return null;
            }
        }

        public void SaveCurrentViewName(string gridKey, string viewName)
        {
            if (string.IsNullOrWhiteSpace(gridKey))
                return;

            string folder = GetGridFolder(gridKey);

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var state = new GenesysGridViewState
            {
                GridKey = gridKey,
                CurrentViewName = string.IsNullOrWhiteSpace(viewName) ? "Predeterminada" : viewName,
                ModifiedAt = DateTime.Now
            };

            var serializer = new XmlSerializer(typeof(GenesysGridViewState));
            using (var stream = File.Create(GetStatePath(gridKey)))
            {
                serializer.Serialize(stream, state);
            }
        }

        private GenesysGridViewLayout Read(string path)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(GenesysGridViewLayout));
                using (var stream = File.OpenRead(path))
                {
                    return serializer.Deserialize(stream) as GenesysGridViewLayout;
                }
            }
            catch
            {
                return null;
            }
        }

        private void Write(string path, GenesysGridViewLayout layout)
        {
            var serializer = new XmlSerializer(typeof(GenesysGridViewLayout));
            using (var stream = File.Create(path))
            {
                serializer.Serialize(stream, layout);
            }
        }

        private string GetGridFolder(string gridKey)
        {
            return Path.Combine(rootPath, SafeFileName(gridKey));
        }

        private string GetStatePath(string gridKey)
        {
            return Path.Combine(GetGridFolder(gridKey), "__state.xml");
        }

        private static string SafeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "default";

            foreach (char c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');

            value = value.Replace('.', '_');
            value = value.Replace(' ', '_');

            return value;
        }
    }
}
