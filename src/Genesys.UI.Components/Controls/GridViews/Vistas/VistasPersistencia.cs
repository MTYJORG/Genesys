using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using System.Windows.Forms;
using System.Drawing;

namespace Genesys.UI.Components.Controls.GridViews
{
    public interface IGridViewStore
    {
        IList<GridViewLayout> Load(string gridKey);
        void Save(GridViewLayout layout);
        void Delete(string gridKey, string viewName);
    }

    public interface IGenesysGridViewStateStore
    {
        string LoadCurrentViewName(string gridKey);
        void SaveCurrentViewName(string gridKey, string viewName);
    }


    public class GridViewFileStore : IGridViewStore, IGenesysGridViewStateStore
    {
        private readonly string rootPath;

        public GridViewFileStore()
            : this(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Genesys",
                "GridViews"))
        {
        }

        public GridViewFileStore(string rootPath)
        {
            this.rootPath = rootPath;

            if (!Directory.Exists(this.rootPath))
                Directory.CreateDirectory(this.rootPath);
        }

        public IList<GridViewLayout> Load(string gridKey)
        {
            string folder = GetGridFolder(gridKey);


            if (!Directory.Exists(folder))
            {
                return new List<GridViewLayout>();
            }

            var result = new List<GridViewLayout>();

            foreach (string file in Directory.GetFiles(folder, "*.xml"))
            {
                if (string.Equals(Path.GetFileName(file), "__state.xml", StringComparison.OrdinalIgnoreCase))
                    continue;


                GridViewLayout layout = Read(file);


                if (layout != null)
                {
                    result.Add(layout);
                }
            }


            return result
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.ViewName)
                .ToList();
        }

        public void Save(GridViewLayout layout)
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

        private GridViewLayout Read(string path)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(GridViewLayout));
                using (var stream = File.OpenRead(path))
                {
                    return serializer.Deserialize(stream) as GridViewLayout;
                }
            }
            catch
            {
                return null;
            }
        }

        private void Write(string path, GridViewLayout layout)
        {
            var serializer = new XmlSerializer(typeof(GridViewLayout));
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


public class GridViewPersistenceService
    {
        private readonly IWin32Window owner;
        private readonly string gridKey;
        private readonly IGridViewStore store;
        private readonly IGenesysGridViewStateStore stateStore;
        private readonly string defaultViewName;
        private readonly Func<string> getCurrentViewName;
        private readonly Action<string> setCurrentViewName;
        private readonly Func<string, bool> isDefaultView;
        private readonly Func<string, GridViewLayout> captureLayout;
        private readonly Action applyDefaultLayout;
        private readonly Action updateButtonState;
        private readonly Action<bool> setHasChanges;

        public GridViewPersistenceService(
            IWin32Window owner,
            string gridKey,
            IGridViewStore store,
            IGenesysGridViewStateStore stateStore,
            string defaultViewName,
            Func<string> getCurrentViewName,
            Action<string> setCurrentViewName,
            Func<string, bool> isDefaultView,
            Func<string, GridViewLayout> captureLayout,
            Action applyDefaultLayout,
            Action updateButtonState,
            Action<bool> setHasChanges)
        {
            this.owner = owner;
            this.gridKey = gridKey;
            this.store = store;
            this.stateStore = stateStore;
            this.defaultViewName = defaultViewName;
            this.getCurrentViewName = getCurrentViewName;
            this.setCurrentViewName = setCurrentViewName;
            this.isDefaultView = isDefaultView;
            this.captureLayout = captureLayout;
            this.applyDefaultLayout = applyDefaultLayout;
            this.updateButtonState = updateButtonState;
            this.setHasChanges = setHasChanges;
        }

        public IList<GridViewLayout> LoadViews()
        {
            return store.Load(gridKey);
        }

        public string RestoreCurrentViewName(string fallbackViewName)
        {
            if (stateStore == null)
                return fallbackViewName;

            string savedViewName = stateStore.LoadCurrentViewName(gridKey);
            return string.IsNullOrWhiteSpace(savedViewName) ? fallbackViewName : savedViewName;
        }

        public void PersistCurrentViewName(string currentViewName)
        {
            if (stateStore != null)
                stateStore.SaveCurrentViewName(gridKey, currentViewName);
        }

        public bool SaveCurrentView(string viewName)
        {

            if (string.IsNullOrWhiteSpace(viewName) || isDefaultView(viewName))
            {
                bool saveAsResult = SaveAsNewView();
                return saveAsResult;
            }

            var layout = captureLayout(viewName);


            if (layout != null)
            {
            }

            try
            {
                store.Save(layout);
            }
            catch
            {
                throw;
            }

            setCurrentViewName(viewName);
            setHasChanges(false);
            PersistCurrentViewName(viewName);
            updateButtonState();


            return true;
        }

        public bool SaveAsNewView()
        {
            string currentViewName = getCurrentViewName();
            string name = GridViewPrompt.Ask(
                "Nueva vista",
                "Nombre de la vista:",
                isDefaultView(currentViewName) ? string.Empty : currentViewName);

            if (string.IsNullOrWhiteSpace(name))
                return false;

            return SaveCurrentView(name);
        }

        public void DuplicateView()
        {
            string currentViewName = getCurrentViewName();
            string baseName = isDefaultView(currentViewName) ? "Nueva vista" : currentViewName + " copia";
            string name = GridViewPrompt.Ask("Duplicar vista", "Nuevo nombre:", baseName);

            if (string.IsNullOrWhiteSpace(name))
                return;

            SaveCurrentView(name);
        }

        public void DeleteViewFromMenu()
        {
            IList<GridViewLayout> views = LoadViews();

            if (views == null || views.Count == 0)
            {
                MessageBox.Show(owner, "No hay vistas guardadas para eliminar.", "Eliminar vista", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string viewName = getCurrentViewName();

            if (isDefaultView(viewName))
                viewName = GridViewPrompt.Ask("Eliminar vista", "Nombre de la vista a eliminar:", string.Empty);

            if (string.IsNullOrWhiteSpace(viewName) || isDefaultView(viewName))
                return;

            bool exists = views.Any(x => string.Equals(x.ViewName, viewName, StringComparison.OrdinalIgnoreCase));

            if (!exists)
            {
                MessageBox.Show(owner, "No se encontró la vista '" + viewName + "'.", "Eliminar vista", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DeleteView(viewName);
        }

        public void DeleteView(string viewName)
        {
            if (string.IsNullOrWhiteSpace(viewName) || isDefaultView(viewName))
                return;

            DialogResult result = MessageBox.Show(
                owner,
                "¿Deseas eliminar la vista '" + viewName + "'? Esta acción no se puede deshacer.",
                "Eliminar vista",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            store.Delete(gridKey, viewName);

            if (string.Equals(getCurrentViewName(), viewName, StringComparison.OrdinalIgnoreCase))
            {
                applyDefaultLayout();
                return;
            }

            updateButtonState();
        }

        public bool SaveCurrentOrAsk()
        {
            string currentViewName = getCurrentViewName();

            if (isDefaultView(currentViewName))
                return SaveAsNewView();

            return SaveCurrentView(currentViewName);
        }
    }


public static class GridViewPrompt
    {
        public static string Ask(string title, string label, string defaultValue)
        {
            using (var form = new Form())
            using (var textBox = new TextBox())
            using (var lbl = new Label())
            using (var ok = new Button())
            using (var cancel = new Button())
            {
                form.Text = title;
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.ClientSize = new Size(360, 120);
                form.Font = new Font("Segoe UI", 9F);

                lbl.Text = label;
                lbl.Left = 12;
                lbl.Top = 12;
                lbl.Width = 330;

                textBox.Left = 12;
                textBox.Top = 38;
                textBox.Width = 330;
                textBox.Text = defaultValue ?? string.Empty;

                ok.Text = "Aceptar";
                ok.Left = 186;
                ok.Top = 78;
                ok.Width = 75;
                ok.DialogResult = DialogResult.OK;

                cancel.Text = "Cancelar";
                cancel.Left = 267;
                cancel.Top = 78;
                cancel.Width = 75;
                cancel.DialogResult = DialogResult.Cancel;

                form.Controls.Add(lbl);
                form.Controls.Add(textBox);
                form.Controls.Add(ok);
                form.Controls.Add(cancel);
                form.AcceptButton = ok;
                form.CancelButton = cancel;

                return form.ShowDialog() == DialogResult.OK
                    ? textBox.Text.Trim()
                    : null;
            }
        }
    }

}
