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

            System.Diagnostics.Debug.WriteLine("===== GRID VIEW FILE STORE: Load START =====");
            System.Diagnostics.Debug.WriteLine("GridKey: " + gridKey);
            System.Diagnostics.Debug.WriteLine("Folder: " + folder);
            System.Diagnostics.Debug.WriteLine("Folder exists: " + Directory.Exists(folder));

            if (!Directory.Exists(folder))
            {
                System.Diagnostics.Debug.WriteLine("Load result count: 0");
                System.Diagnostics.Debug.WriteLine("===== GRID VIEW FILE STORE: Load END =====");
                return new List<GenesysGridViewLayout>();
            }

            var result = new List<GenesysGridViewLayout>();

            foreach (string file in Directory.GetFiles(folder, "*.xml"))
            {
                if (string.Equals(Path.GetFileName(file), "__state.xml", StringComparison.OrdinalIgnoreCase))
                    continue;

                System.Diagnostics.Debug.WriteLine("Reading view file: " + file);

                GenesysGridViewLayout layout = Read(file);

                System.Diagnostics.Debug.WriteLine("Layout null: " + (layout == null));

                if (layout != null)
                {
                    System.Diagnostics.Debug.WriteLine("ViewName: " + layout.ViewName);
                    System.Diagnostics.Debug.WriteLine("FilterStateXml length: " + (layout.FilterStateXml == null ? 0 : layout.FilterStateXml.Length));
                    System.Diagnostics.Debug.WriteLine("NativeGridLayoutXml length: " + (layout.NativeGridLayoutXml == null ? 0 : layout.NativeGridLayoutXml.Length));
                    System.Diagnostics.Debug.WriteLine("Sorts: " + (layout.Sorts == null ? 0 : layout.Sorts.Count));
                    result.Add(layout);
                }
            }

            System.Diagnostics.Debug.WriteLine("Load result count: " + result.Count);
            System.Diagnostics.Debug.WriteLine("===== GRID VIEW FILE STORE: Load END =====");

            return result
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.ViewName)
                .ToList();
        }

        public void Save(GenesysGridViewLayout layout)
        {
            System.Diagnostics.Debug.WriteLine("===== GRID VIEW FILE STORE: Save START =====");
            System.Diagnostics.Debug.WriteLine("layout null: " + (layout == null));

            if (layout == null)
                throw new ArgumentNullException("layout");

            System.Diagnostics.Debug.WriteLine("GridKey: " + layout.GridKey);
            System.Diagnostics.Debug.WriteLine("ViewName: " + layout.ViewName);
            System.Diagnostics.Debug.WriteLine("Columns: " + (layout.Columns == null ? 0 : layout.Columns.Count));
            System.Diagnostics.Debug.WriteLine("Groups: " + (layout.Groups == null ? 0 : layout.Groups.Count));
            System.Diagnostics.Debug.WriteLine("Summaries: " + (layout.Summaries == null ? 0 : layout.Summaries.Count));
            System.Diagnostics.Debug.WriteLine("Sorts: " + (layout.Sorts == null ? 0 : layout.Sorts.Count));
            System.Diagnostics.Debug.WriteLine("FilterStateXml length: " + (layout.FilterStateXml == null ? 0 : layout.FilterStateXml.Length));
            System.Diagnostics.Debug.WriteLine("NativeGridLayoutXml length: " + (layout.NativeGridLayoutXml == null ? 0 : layout.NativeGridLayoutXml.Length));

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

            System.Diagnostics.Debug.WriteLine("Path: " + path);

            Write(path, layout);

            System.Diagnostics.Debug.WriteLine("File exists after save: " + File.Exists(path));
            System.Diagnostics.Debug.WriteLine("File size: " + (File.Exists(path) ? new FileInfo(path).Length : 0));
            System.Diagnostics.Debug.WriteLine("===== GRID VIEW FILE STORE: Save END =====");
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

            System.Diagnostics.Debug.WriteLine("===== GRID VIEW FILE STORE: LoadCurrentViewName =====");
            System.Diagnostics.Debug.WriteLine("GridKey: " + gridKey);
            System.Diagnostics.Debug.WriteLine("Path: " + path);
            System.Diagnostics.Debug.WriteLine("Exists: " + File.Exists(path));

            if (!File.Exists(path))
                return null;

            try
            {
                var serializer = new XmlSerializer(typeof(GenesysGridViewState));
                using (var stream = File.OpenRead(path))
                {
                    GenesysGridViewState state = serializer.Deserialize(stream) as GenesysGridViewState;
                    System.Diagnostics.Debug.WriteLine("State null: " + (state == null));
                    System.Diagnostics.Debug.WriteLine("CurrentViewName: " + (state == null ? string.Empty : state.CurrentViewName));
                    return state == null ? null : state.CurrentViewName;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LoadCurrentViewName ERROR: " + ex);
                return null;
            }
        }

        public void SaveCurrentViewName(string gridKey, string viewName)
        {
            System.Diagnostics.Debug.WriteLine("===== GRID VIEW FILE STORE: SaveCurrentViewName =====");
            System.Diagnostics.Debug.WriteLine("GridKey: " + gridKey);
            System.Diagnostics.Debug.WriteLine("ViewName: " + viewName);
            System.Diagnostics.Debug.WriteLine("Path: " + GetStatePath(gridKey));

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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Read ERROR: " + ex);
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
