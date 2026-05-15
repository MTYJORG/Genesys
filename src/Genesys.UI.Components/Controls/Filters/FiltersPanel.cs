using System;
using System.Windows.Forms;

namespace Genesys.UI.Components.Controls.Filters
{
    public class FiltersPanel : UserControl
    {
        public event EventHandler FiltersChanged;

        public FiltersPanel()
        {
            this.Dock = DockStyle.Fill;
        }

        // PASO 🎯 2.2.1 — Soporte para controles hijos
        protected void RegisterControl(Control control)
        {
            if (control is TextBox tb)
                tb.TextChanged += (s, e) => OnFiltersChanged();

            else if (control is ComboBox cb)
                cb.SelectedIndexChanged += (s, e) => OnFiltersChanged();

            else if (control is DateTimePicker dt)
                dt.ValueChanged += (s, e) => OnFiltersChanged();
        }
        
        // 🎯 PASO 2.2.2 — API para agregar filtros
        public void AddFilter(Control control)
        {
            RegisterControl(control);
            this.Controls.Add(control);
        }

        // 🎯 PASO 2.2.3 — Evitar múltiples disparos(clave)
        private bool _suspendEvents = false;

        public void SuspendFiltering()
        {
            _suspendEvents = true;
        }

        public void ResumeFiltering()
        {
            _suspendEvents = false;
        }

        protected void OnFiltersChanged()
        {
            if (_suspendEvents)
                return;

            FiltersChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}