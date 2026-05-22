using Genesys.UI.Controls;
using Genesys.UI.Data;
using System;
using System.Collections.Generic;
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

        private bool hasExecutedSearch;
        private string lastSearchSignature;

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
            initializing = true;

            LookupParameterName = "@LookupValue";
            ComboParameterName = "@ComboValue";

            dataProvider = new GenesysGridDataProvider();

            AutoSearch = true;

            Dock = DockStyle.Fill;
            Height = 70;
            BackColor = Color.WhiteSmoke;
            TabStop = false;

            layout = new FlowLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.FlowDirection = FlowDirection.LeftToRight;
            layout.WrapContents = false;
            layout.AutoScroll = true;
            layout.BackColor = Color.WhiteSmoke;
            layout.TabStop = false;

            lblFecha = new Label();
            cboRangoFecha = new ComboBox();
            dtpInicio = new DateTimePicker();
            dtpFinal = new DateTimePicker();

            lblLookup = new Label();
            LookupTextBox = new aTextBox();
            txtLookupDescripcion = new aTextBox();

            lblCombo = new Label();
            cboFiltro = new ComboBox();

            Controls.Add(layout);

            BuildLayout();
            ConfigureTabOrder();
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
            panel.Margin = new Padding(5, 0, 12, 0);
            panel.BackColor = Color.WhiteSmoke;
            panel.TabStop = false;

            lblFecha.Text = "Fecha";
            lblFecha.TextAlign = ContentAlignment.MiddleLeft;
            lblFecha.SetBounds(0, 0, 70, 16);
            lblFecha.TabStop = false;

            cboRangoFecha.Name = "cboRangoFecha";
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

            dtpInicio.Name = "dtpInicio";
            dtpInicio.Format = DateTimePickerFormat.Short;
            dtpInicio.SetBounds(78, 6, 100, 22);

            dtpFinal.Name = "dtpFinal";
            dtpFinal.Format = DateTimePickerFormat.Short;
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
            panel.Width = 450;
            panel.Height = 62;
            panel.Margin = new Padding(0, 0, 12, 0);
            panel.BackColor = Color.WhiteSmoke;
            panel.TabStop = false;

            lblLookup.Text = "Lookup";
            lblLookup.TextAlign = ContentAlignment.MiddleRight;
            lblLookup.SetBounds(5, 6, 65, 22);
            lblLookup.TabStop = false;

            LookupTextBox.Name = "LookupTextBox";
            LookupTextBox.SetBounds(70, 6, 100, 22);
            LookupTextBox.Mayusculas = true;
            LookupTextBox.TabStop = true;
            LookupTextBox.CausesValidation = true;

            txtLookupDescripcion.Name = "txtLookupDescripcion";
            txtLookupDescripcion.SetBounds(180, 6, 300, 22);
            txtLookupDescripcion.Enabled = false;
            txtLookupDescripcion.BackColor = Color.WhiteSmoke;
            txtLookupDescripcion.ForeColor = Color.Black;
            txtLookupDescripcion.TabStop = false;

            panel.Controls.Add(lblLookup);
            panel.Controls.Add(LookupTextBox);
            panel.Controls.Add(txtLookupDescripcion);

            return panel;
        }

        private Panel BuildComboPanel()
        {
            var panel = new Panel();
            panel.Width = 200;
            panel.Height = 62;
            panel.Margin = new Padding(0, 0, 12, 0);
            panel.BackColor = Color.WhiteSmoke;
            panel.TabStop = false;

            lblCombo.Text = "Estado";
            lblCombo.TextAlign = ContentAlignment.MiddleRight;
            lblCombo.SetBounds(5, 6, 55, 22);
            lblCombo.TabStop = false;

            cboFiltro.Name = "cboFiltro";
            cboFiltro.DropDownStyle = ComboBoxStyle.DropDownList;
            cboFiltro.SetBounds(60, 6, 130, 22);

            panel.Controls.Add(lblCombo);
            panel.Controls.Add(cboFiltro);

            return panel;
        }

        private void ConfigureTabOrder()
        {
            cboRangoFecha.TabStop = true;
            dtpInicio.TabStop = true;
            dtpFinal.TabStop = true;
            LookupTextBox.TabStop = true;
            cboFiltro.TabStop = true;

            cboRangoFecha.TabIndex = 0;
            dtpInicio.TabIndex = 1;
            dtpFinal.TabIndex = 2;
            LookupTextBox.TabIndex = 3;
            cboFiltro.TabIndex = 4;
        }

        private void WireEvents()
        {
            cboRangoFecha.SelectionChangeCommitted += delegate
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
                BeginInvoke(new MethodInvoker(delegate
                {
                    ExecuteSearchIfEnabled();
                }));
            };

            cboFiltro.SelectionChangeCommitted += delegate
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
                SetDateValueIfDifferent(dtpInicio, today);
                SetDateValueIfDifferent(dtpFinal, today);
                return;
            }

            if (value == "Ayer")
            {
                DateTime yesterday = today.AddDays(-1);
                SetDateValueIfDifferent(dtpInicio, yesterday);
                SetDateValueIfDifferent(dtpFinal, yesterday);
                return;
            }

            if (value == "Semana")
            {
                int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                DateTime start = today.AddDays(-1 * diff);

                SetDateValueIfDifferent(dtpInicio, start);
                SetDateValueIfDifferent(dtpFinal, start.AddDays(6));
                return;
            }

            if (value == "Mes")
            {
                DateTime start = new DateTime(today.Year, today.Month, 1);
                DateTime end = start.AddMonths(1).AddDays(-1);

                SetDateValueIfDifferent(dtpInicio, start);
                SetDateValueIfDifferent(dtpFinal, end);
                return;
            }

            if (value == "Ejercicio")
            {
                SetDateValueIfDifferent(dtpInicio, new DateTime(today.Year, 1, 1));
                SetDateValueIfDifferent(dtpFinal, new DateTime(today.Year, 12, 31));
            }
        }

        private void SetDateValueIfDifferent(DateTimePicker picker, DateTime value)
        {
            if (picker == null)
                return;

            if (picker.Value.Date == value.Date)
                return;

            picker.Value = value;
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
            LookupTextBox.LookupControl = txtLookupDescripcion;

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

            request.LookupParameterName = LookupParameterName;
            request.LookupValue = LookupTextBox.Text;

            request.ComboParameterName = ComboParameterName;
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
                    SetDateValueIfDifferent(dtpInicio, state.FechaInicio.Value);

                if (state.FechaFinal.HasValue)
                    SetDateValueIfDifferent(dtpFinal, state.FechaFinal.Value);

                LookupTextBox.Text = state.LookupValue ?? string.Empty;

                if (state.ComboValue != null)
                    cboFiltro.SelectedValue = state.ComboValue;

                ResetSearchSignature();
            }
            finally
            {
                restoringState = false;
            }
        }

        public void ExecuteSearch()
        {
            ExecuteSearch(force: false);
        }

        public void ExecuteSearch(bool force)
        {
            if (string.IsNullOrWhiteSpace(StoredProcedureName))
                return;

            string currentSignature = BuildSearchSignature();

            if (!force &&
                hasExecutedSearch &&
                string.Equals(currentSignature, lastSearchSignature, StringComparison.Ordinal))
            {
                return;
            }

            hasExecutedSearch = true;
            lastSearchSignature = currentSignature;

            GenesysGridFilterRequest request = BuildRequest();
            GenesysGridFilterResult result = dataProvider.Execute(request);

            if (SearchCompleted != null)
                SearchCompleted(this, result);
        }

        public void ResetSearchSignature()
        {
            hasExecutedSearch = false;
            lastSearchSignature = null;
        }

        private string BuildSearchSignature()
        {
            GenesysGridFilterRequest request = BuildRequest();

            string comboValue = request.ComboValue == null
                ? string.Empty
                : Convert.ToString(request.ComboValue);

            return string.Join("|", new[]
            {
                request.StoredProcedureName ?? string.Empty,
                request.TipoDeAccion ?? string.Empty,
                request.FechaInicio.HasValue ? request.FechaInicio.Value.ToString("yyyyMMdd") : string.Empty,
                request.FechaFinal.HasValue ? request.FechaFinal.Value.ToString("yyyyMMdd") : string.Empty,
                request.LookupParameterName ?? string.Empty,
                request.LookupValue ?? string.Empty,
                request.ComboParameterName ?? string.Empty,
                comboValue
            });
        }

        public bool FocusFirstFilter()
        {
            Control[] controls = GetFocusableFilterControls();

            if (controls.Length == 0)
                return false;

            Control control = controls[0];

            if (control == null)
                return false;

            control.Focus();
            return true;
        }

        public bool FocusLastFilter()
        {
            Control[] controls = GetFocusableFilterControls();

            if (controls.Length == 0)
                return false;

            controls[controls.Length - 1].Focus();
            return true;
        }

        public bool MoveFilterFocus(bool forward)
        {
            Control[] controls = GetFocusableFilterControls();

            if (controls.Length == 0)
                return false;

            Control current = GetFocusedChildControl();

            if (current == null)
            {
                return forward ? FocusFirstFilter() : FocusLastFilter();
            }

            int index = Array.IndexOf(controls, current);

            if (index < 0)
            {
                return forward ? FocusFirstFilter() : FocusLastFilter();
            }

            int nextIndex = forward ? index + 1 : index - 1;

            if (nextIndex < 0 || nextIndex >= controls.Length)
                return false;

            controls[nextIndex].Focus();
            return true;
        }

        public bool ContainsFilterFocus()
        {
            return ContainsFocus;
        }

        private Control[] GetFocusableFilterControls()
        {
            var list = new List<Control>();

            if (cboRangoFecha.Visible && cboRangoFecha.Enabled)
                list.Add(cboRangoFecha);

            if (dtpInicio.Visible && dtpInicio.Enabled)
                list.Add(dtpInicio);

            if (dtpFinal.Visible && dtpFinal.Enabled)
                list.Add(dtpFinal);

            if (LookupTextBox.Visible && LookupTextBox.Enabled)
                list.Add(LookupTextBox);

            if (cboFiltro.Visible && cboFiltro.Enabled)
                list.Add(cboFiltro);

            return list.ToArray();
        }

        private Control GetFocusedChildControl()
        {
            if (cboRangoFecha.Focused)
                return cboRangoFecha;

            if (dtpInicio.Focused)
                return dtpInicio;

            if (dtpFinal.Focused)
                return dtpFinal;

            if (LookupTextBox.Focused || LookupTextBox.ContainsFocus)
                return LookupTextBox;

            if (cboFiltro.Focused)
                return cboFiltro;

            return null;
        }
        
        public void ForceSearch()
        {
            ExecuteSearch(force: true);
        }

        public void RefreshSearch()
        {
            ForceSearch();
        }
    }
}
