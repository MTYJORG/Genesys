using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Genesys.UI.Components.Controls.GridViews
{
    public class GenesysGridViewPersistenceService
    {
        private readonly IWin32Window owner;
        private readonly string gridKey;
        private readonly IGenesysGridViewStore store;
        private readonly IGenesysGridViewStateStore stateStore;
        private readonly string defaultViewName;
        private readonly Func<string> getCurrentViewName;
        private readonly Action<string> setCurrentViewName;
        private readonly Func<string, bool> isDefaultView;
        private readonly Func<string, GenesysGridViewLayout> captureLayout;
        private readonly Action applyDefaultLayout;
        private readonly Action updateButtonState;
        private readonly Action<bool> setHasChanges;

        public GenesysGridViewPersistenceService(
            IWin32Window owner,
            string gridKey,
            IGenesysGridViewStore store,
            IGenesysGridViewStateStore stateStore,
            string defaultViewName,
            Func<string> getCurrentViewName,
            Action<string> setCurrentViewName,
            Func<string, bool> isDefaultView,
            Func<string, GenesysGridViewLayout> captureLayout,
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

        public IList<GenesysGridViewLayout> LoadViews()
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
            System.Diagnostics.Debug.WriteLine("===== GRID VIEW PERSISTENCE: SaveCurrentView START =====");
            System.Diagnostics.Debug.WriteLine("viewName: " + viewName);
            System.Diagnostics.Debug.WriteLine("IsDefaultView: " + isDefaultView(viewName));

            if (string.IsNullOrWhiteSpace(viewName) || isDefaultView(viewName))
            {
                System.Diagnostics.Debug.WriteLine("SaveCurrentView: vista inválida o predeterminada; se invoca SaveAsNewView.");
                bool saveAsResult = SaveAsNewView();
                System.Diagnostics.Debug.WriteLine("SaveAsNewView result: " + saveAsResult);
                System.Diagnostics.Debug.WriteLine("===== GRID VIEW PERSISTENCE: SaveCurrentView END =====");
                return saveAsResult;
            }

            var layout = captureLayout(viewName);

            System.Diagnostics.Debug.WriteLine("Layout captured: " + (layout != null));

            if (layout != null)
            {
                System.Diagnostics.Debug.WriteLine("Columns: " + (layout.Columns == null ? 0 : layout.Columns.Count));
                System.Diagnostics.Debug.WriteLine("Groups: " + (layout.Groups == null ? 0 : layout.Groups.Count));
                System.Diagnostics.Debug.WriteLine("Summaries: " + (layout.Summaries == null ? 0 : layout.Summaries.Count));
                System.Diagnostics.Debug.WriteLine("Sorts: " + (layout.Sorts == null ? 0 : layout.Sorts.Count));
                System.Diagnostics.Debug.WriteLine("FilterStateXml length: " + (layout.FilterStateXml == null ? 0 : layout.FilterStateXml.Length));
                System.Diagnostics.Debug.WriteLine("NativeGridLayoutXml length: " + (layout.NativeGridLayoutXml == null ? 0 : layout.NativeGridLayoutXml.Length));
            }

            try
            {
                store.Save(layout);
                System.Diagnostics.Debug.WriteLine("store.Save ejecutado.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("store.Save ERROR: " + ex);
                throw;
            }

            setCurrentViewName(viewName);
            setHasChanges(false);
            PersistCurrentViewName(viewName);
            updateButtonState();

            System.Diagnostics.Debug.WriteLine("CurrentViewName: " + viewName);
            System.Diagnostics.Debug.WriteLine("hasChanges: False");
            System.Diagnostics.Debug.WriteLine("===== GRID VIEW PERSISTENCE: SaveCurrentView END =====");

            return true;
        }

        public bool SaveAsNewView()
        {
            string currentViewName = getCurrentViewName();
            string name = GenesysGridViewPrompt.Ask(
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
            string name = GenesysGridViewPrompt.Ask("Duplicar vista", "Nuevo nombre:", baseName);

            if (string.IsNullOrWhiteSpace(name))
                return;

            SaveCurrentView(name);
        }

        public void DeleteViewFromMenu()
        {
            IList<GenesysGridViewLayout> views = LoadViews();

            if (views == null || views.Count == 0)
            {
                MessageBox.Show(owner, "No hay vistas guardadas para eliminar.", "Eliminar vista", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string viewName = getCurrentViewName();

            if (isDefaultView(viewName))
                viewName = GenesysGridViewPrompt.Ask("Eliminar vista", "Nombre de la vista a eliminar:", string.Empty);

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
}
