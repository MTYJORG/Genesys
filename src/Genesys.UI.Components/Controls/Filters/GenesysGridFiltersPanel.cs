using Genesys.UI.Controls;
using Genesys.UI.Data;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Genesys.UI.Components.Controls.Filters
{
    public class GenesysGridFiltersPanel : UserControl
    {
        private readonly FlowLayoutPanel layout;

        private readonly Label lblFecha;
        private readonly ComboBox cboRangoFecha;
        private readonly DateTimePicker dtpInicio;
        private readonly DateTimePicker dtpFinal;

        private readonly Label lblLookup;
        private readonly TextBox txtLookupDescripcion;

        private readonly Label lblCombo;
        private readonly ComboBox cboFiltro;

        private readonly GenesysGridDataProvider dataProvider;

        private bool initializing;
        private bool restoringState;

        
        public aTextBox LookupTextBox { get; private set; }

        public string StoredProcedureName { get; set; }
        public string TipoDeAccion { get; set; }
        public string LookupParameterName { get; set; }
        public string ComboParameterName { get; set; }


        public string PersistenceKey { get; set; }

        public bool AutoSearch { get; set; }

        public event EventHandler<GenesysGridFilterResult> SearchCompleted;

        public GenesysGridFiltersPanel()
        {
            LookupParameterName = "@LookupValue";
            ComboParameterName = "@ComboValue";

            initializing = true;

            dataProvider = new GenesysGridDataProvider();

            AutoSearch = true;

            Dock = DockStyle.Fill;
            Height = 70;
            BackColor = Color.WhiteSmoke;

            layout = new FlowLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.FlowDirection = FlowDirection.LeftToRight;
            layout.WrapContents = false;
            layout.AutoScroll = true;
            layout.Padding = new Padding(6, 4, 6, 2);
            layout.BackColor = Color.WhiteSmoke;

            lblFecha = new Label();
            cboRangoFecha = new ComboBox();
            dtpInicio = new DateTimePicker();
            dtpFinal = new DateTimePicker();

            lblLookup = new Label();
            LookupTextBox = new aTextBox();
            txtLookupDescripcion = new TextBox();

            lblCombo = new Label();
            cboFiltro = new ComboBox();

            Controls.Add(layout);

            BuildLayout();
            WireEvents();
            ApplyRangoFecha();

            initializing = false;
        }

        private void BuildLayout()
        {
            layout.Controls.Add(BuildFechaPanel());
            layout.Controls.Add(BuildLookupPanel());
            layout.Controls.Add(BuildComboPanel());
        }

        private Panel BuildFechaPanel()
        {
            var panel = new Panel();
            panel.Width = 185;
            panel.Height = 62;
            panel.Margin = new Padding(0, 0, 12, 0);
            panel.BackColor = Color.WhiteSmoke;

            lblFecha.Text = "Fecha";
            lblFecha.TextAlign = ContentAlignment.MiddleLeft;
            lblFecha.SetBounds(0, 42, 70, 16);

            cboRangoFecha.DropDownStyle = ComboBoxStyle.DropDownList;
            cboRangoFecha.Items.Add("<Todas>");
            cboRangoFecha.Items.Add("Rango");
            cboRangoFecha.Items.Add("Hoy");
            cboRangoFecha.Items.Add("Ayer");
            cboRangoFecha.Items.Add("Semana");
            cboRangoFecha.Items.Add("Mes");
            cboRangoFecha.Items.Add("Ejercicio");
            cboRangoFecha.SelectedIndex = 0;
            cboRangoFecha.SetBounds(0, 18, 72, 22);

            dtpInicio.Format = DateTimePickerFormat.Short;
            dtpFinal.Format = DateTimePickerFormat.Short;

            dtpInicio.SetBounds(78, 6, 100, 22);
            dtpFinal.SetBounds(78, 32, 100, 22);

            panel.Controls.Add(cboRangoFecha);
            panel.Controls.Add(lblFecha);
            panel.Controls.Add(dtpInicio);
            panel.Controls.Add(dtpFinal);

            return panel;
        }

        private Panel BuildLookupPanel()
        {
            var panel = new Panel();
            panel.Width = 275;
            panel.Height = 62;
            panel.Margin = new Padding(0, 0, 12, 0);
            panel.BackColor = Color.WhiteSmoke;

            lblLookup.Text = "Lookup";
            lblLookup.TextAlign = ContentAlignment.MiddleLeft;
            lblLookup.SetBounds(0, 6, 65, 22);

            LookupTextBox.SetBounds(70, 6, 90, 22);

            txtLookupDescripcion.SetBounds(70, 31, 195, 20);
            txtLookupDescripcion.ReadOnly = true;
            txtLookupDescripcion.BorderStyle = BorderStyle.None;
            txtLookupDescripcion.BackColor = Color.WhiteSmoke;
            txtLookupDescripcion.ForeColor = Color.Black;

            panel.Controls.Add(lblLookup);
            panel.Controls.Add(LookupTextBox);
            panel.Controls.Add(txtLookupDescripcion);

            return panel;
        }

        private Panel BuildComboPanel()
        {
            var panel = new Panel();
            panel.Width = 220;
            panel.Height = 62;
            panel.Margin = new Padding(0, 0, 12, 0);
            panel.BackColor = Color.WhiteSmoke;

            lblCombo.Text = "Estado";
            lblCombo.TextAlign = ContentAlignment.MiddleLeft;
            lblCombo.SetBounds(0, 6, 55, 22);

            cboFiltro.DropDownStyle = ComboBoxStyle.DropDownList;
            cboFiltro.SetBounds(60, 6, 150, 22);

            panel.Controls.Add(lblCombo);
            panel.Controls.Add(cboFiltro);

            return panel;
        }

        private void WireEvents()
        {
            cboRangoFecha.SelectedIndexChanged += delegate
            {
                ApplyRangoFecha();
                ExecuteSearchIfEnabled();
            };

            dtpInicio.ValueChanged += delegate
            {
                ExecuteSearchIfEnabled();
            };

            dtpFinal.ValueChanged += delegate
            {
                ExecuteSearchIfEnabled();
            };

            LookupTextBox.Leave += delegate
            {
                ExecuteSearchIfEnabled();
            };

            cboFiltro.SelectedValueChanged += delegate
            {
                ExecuteSearchIfEnabled();
            };
        }

        private void ApplyRangoFecha()
        {
            string value = cboRangoFecha.SelectedItem == null
                ? "<Todas>"
                : cboRangoFecha.SelectedItem.ToString();

            DateTime today = DateTime.Today;

            if (value == "<Todas>")
            {
                dtpInicio.Visible = false;
                dtpFinal.Visible = false;
                return;
            }

            dtpInicio.Visible = true;
            dtpFinal.Visible = true;

            if (value == "Rango")
                return;

            if (value == "Hoy")
            {
                dtpInicio.Value = today;
                dtpFinal.Value = today;
                return;
            }

            if (value == "Ayer")
            {
                dtpInicio.Value = today.AddDays(-1);
                dtpFinal.Value = today.AddDays(-1);
                return;
            }

            if (value == "Semana")
            {
                int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                DateTime start = today.AddDays(-1 * diff);

                dtpInicio.Value = start;
                dtpFinal.Value = start.AddDays(6);
                return;
            }

            if (value == "Mes")
            {
                dtpInicio.Value = new DateTime(today.Year, today.Month, 1);
                dtpFinal.Value = dtpInicio.Value.AddMonths(1).AddDays(-1);
                return;
            }

            if (value == "Ejercicio")
            {
                dtpInicio.Value = new DateTime(today.Year, 1, 1);
                dtpFinal.Value = new DateTime(today.Year, 12, 31);
            }
        }

        private void ExecuteSearchIfEnabled()
        {
            if (initializing)
                return;

            if (restoringState)
                return;

            if (!AutoSearch)
                return;

            ExecuteSearch();
        }

        public void SetFechaTitle(string title)
        {
            lblFecha.Text = title;
        }

        public void SetLookupTitle(string title)
        {
            lblLookup.Text = title;
        }

        public void SetLookupProvider(string parametroValor)
        {
            LookupTextBox.EsLookup = true;

            LookupTextBox.LookupProvider =
                new StoredProcedureLookupProvider
                {
                    ParametroValor = parametroValor
                };
        }

        public void SetLookupDescription(string description)
        {
            txtLookupDescripcion.Text = description;
        }

        public void SetComboTitle(string title)
        {
            lblCombo.Text = title;
        }

        public void SetComboItems(params GenesysComboFilterItem[] items)
        {
            cboFiltro.DataSource = null;
            cboFiltro.Items.Clear();

            if (items == null)
                return;

            cboFiltro.DisplayMember = "Text";
            cboFiltro.ValueMember = "Value";
            cboFiltro.DataSource = items;

            if (items.Length > 0)
                cboFiltro.SelectedIndex = 0;
        }

        public void SetComboDataSource(object dataSource, string displayMember, string valueMember)
        {
            cboFiltro.DataSource = null;
            cboFiltro.Items.Clear();

            cboFiltro.DisplayMember = displayMember;
            cboFiltro.ValueMember = valueMember;
            cboFiltro.DataSource = dataSource;
        }

        public GenesysGridFilterRequest BuildRequest()
        {
            bool todas = cboRangoFecha.SelectedItem != null &&
                         cboRangoFecha.SelectedItem.ToString() == "<Todas>";

            var request = new GenesysGridFilterRequest();

            request.StoredProcedureName = StoredProcedureName;
            request.TipoDeAccion = TipoDeAccion;

            request.FechaInicio = todas ? (DateTime?)null : dtpInicio.Value.Date;
            request.FechaFinal = todas ? (DateTime?)null : dtpFinal.Value.Date;
            request.ComboParameterName = ComboParameterName;
            request.LookupValue = LookupTextBox.Text;

            request.LookupParameterName = LookupParameterName;

            request.ComboValue =
                cboFiltro.SelectedValue == null
                    ? null
                    : cboFiltro.SelectedValue;

            return request;
        }

        public GenesysGridFilterState GetState()
        {
            var state = new GenesysGridFilterState();

            state.RangoFecha = cboRangoFecha.SelectedItem == null
                ? "<Todas>"
                : cboRangoFecha.SelectedItem.ToString();

            state.FechaInicio = dtpInicio.Visible ? (DateTime?)dtpInicio.Value.Date : null;
            state.FechaFinal = dtpFinal.Visible ? (DateTime?)dtpFinal.Value.Date : null;

            state.LookupValue = LookupTextBox.Text;

            state.ComboValue =
                cboFiltro.SelectedValue == null
                    ? null
                    : cboFiltro.SelectedValue;

            return state;
        }

        public void ApplyState(GenesysGridFilterState state)
        {
            if (state == null)
                return;

            restoringState = true;

            try
            {
                if (!string.IsNullOrWhiteSpace(state.RangoFecha) &&
                    cboRangoFecha.Items.Contains(state.RangoFecha))
                {
                    cboRangoFecha.SelectedItem = state.RangoFecha;
                }

                ApplyRangoFecha();

                if (state.FechaInicio.HasValue)
                    dtpInicio.Value = state.FechaInicio.Value;

                if (state.FechaFinal.HasValue)
                    dtpFinal.Value = state.FechaFinal.Value;

                LookupTextBox.Text = state.LookupValue ?? string.Empty;

                if (state.ComboValue != null)
                    cboFiltro.SelectedValue = state.ComboValue;
            }
            finally
            {
                restoringState = false;
            }
        }

        public void ExecuteSearch()
        {
            if (string.IsNullOrWhiteSpace(StoredProcedureName))
                return;

            GenesysGridFilterRequest request = BuildRequest();
            GenesysGridFilterResult result = dataProvider.Execute(request);

            if (SearchCompleted != null)
                SearchCompleted(this, result);
        }
    }
}