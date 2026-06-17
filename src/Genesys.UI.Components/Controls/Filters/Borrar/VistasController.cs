using Genesys.UI.Components.Controls.Filters;
using Syncfusion.Windows.Forms.Tools;
using Syncfusion.WinForms.DataGrid;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Windows.Forms;
using Genesys.UI.Components.Controls.GridViews;

namespace Genesys.UI.Components.Controls.GridViews.Vistas
{
    /// <summary>
    /// Fachada del módulo de Vistas para GenesysGridForm.
    /// Construye y coordina el administrador, selector horizontal, diseñador, botón de configuración
    /// y persistencia de filtros superiores asociados a cada vista.
    /// </summary>
    public sealed class VistasController : IDisposable
    {
        private const int ConfigButtonWidth = 40;
        private const int ConfigButtonHeight = 42;
        private const float ConfigButtonFontSize = 13F;

        private readonly Form owner;
        private readonly SfDataGrid grid;
        private readonly GenesysGridFiltersPanel filters;
        private readonly Panel toolbarConfigPanel;
        private ToolStripEx configToolStrip;
        private ToolStripButton configButton;
        private System.Drawing.Font configButtonFont;
        private readonly TableLayoutPanel workspacePanel;
        private readonly Panel gridPanel;
        private readonly Panel viewInfoPanel;
        private readonly float designerWidth;
        private readonly List<FilterControlSubscription> filterControlSubscriptions =
            new List<FilterControlSubscription>();
        private bool suppressFilterRuntimeTracking;

        private bool disposed;

        public VistasController(
            Form owner,
            SfDataGrid grid,
            GenesysGridFiltersPanel filters,
            Panel toolbarConfigPanel,
            TableLayoutPanel workspacePanel,
            Panel gridPanel,
            Panel viewInfoPanel,
            float designerWidth)
        {
            this.owner = owner;
            this.grid = grid;
            this.filters = filters;
            this.toolbarConfigPanel = toolbarConfigPanel;
            this.workspacePanel = workspacePanel;
            this.gridPanel = gridPanel;
            this.viewInfoPanel = viewInfoPanel;
            this.designerWidth = designerWidth;
        }

        public VistasAdministrador Manager { get; private set; }
        public VistasSelector ViewSelectorBar { get; private set; }
        public Panel ViewDesignerHostPanel { get; private set; }
        public VistasDesignerPanel ViewDesigner { get; private set; }

        /// <summary>
        /// Construye selector, diseñador y administrador de vistas.
        /// </summary>
        public void Initialize()
        {
            BuildConfigToolStrip();
            BuildDesignerHost();
            BuildManager();
            BuildSelectorBar();
            HookConfigButton();
        }

        /// <summary>
        /// Devuelve el layout activo cuando la vista actual no es Predeterminada.
        /// </summary>
        public GridViewLayout GetCurrentViewLayout()
        {
            if (Manager == null || Manager.IsCurrentViewDefault)
                return null;

            return Manager.GetCurrentViewLayout();
        }

        /// <summary>
        /// Enlaza datos al grid usando el administrador de vistas para aplicar en un
        /// mismo pipeline columnas, agrupamiento, ordenamiento, filtros internos y resúmenes.
        /// </summary>
        public void BindGrid(DataTable table)
        {
            if (table == null)
                return;

            if (Manager != null)
                Manager.BindDataTable(table);
            else if (grid != null)
                GridConfigurator.BindDataTable(grid, table);

            if (ViewDesigner != null)
                ViewDesigner.ReloadColumns();

            // Refrescar datos no equivale a guardar ni descartar cambios runtime de la vista.
        }
        /// <summary>
        /// Punto de compatibilidad para flujos antiguos que notificaban el bind desde fuera.
        /// El flujo actual usa BindGrid, pero se conserva para formularios derivados existentes.
        /// </summary>
        public void AfterGridBound()
        {
            if (ViewDesigner != null)
                ViewDesigner.ReloadColumns();

            if (Manager != null)
                Manager.MarkClean();
        }

        public void ShowGridConfigMenu()
        {
            if (Manager != null)
            {
                Manager.ShowMenu();
                return;
            }

            MessageBox.Show(
                owner,
                "Configuración del grid",
                "",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        public void ToggleViewDesigner()
        {
            if (ViewDesignerHostPanel == null || ViewDesigner == null || Manager == null)
                return;

            if (ViewDesignerHostPanel.Visible)
                HideViewDesigner();
            else
                ShowViewDesigner();
        }

        public void ShowViewDesigner()
        {
            if (ViewDesignerHostPanel == null || ViewDesigner == null || Manager == null)
                return;

            ViewDesigner.Attach(Manager);
            ViewDesignerHostPanel.Visible = true;

            if (workspacePanel != null && workspacePanel.ColumnStyles.Count > 1)
            {
                workspacePanel.ColumnStyles[1].Width = designerWidth;
                workspacePanel.PerformLayout();
            }
        }

        public void HideViewDesigner()
        {
            if (ViewDesignerHostPanel == null || workspacePanel == null)
                return;

            ViewDesignerHostPanel.Visible = false;

            if (workspacePanel.ColumnStyles.Count > 1)
            {
                workspacePanel.ColumnStyles[1].Width = 0F;
                workspacePanel.PerformLayout();
            }
        }

        public void ShowViewDesignerForm()
        {
            if (Manager == null)
                return;

            using (var designerForm = new VistasDesignerForm(Manager))
            {
                designerForm.StartPosition = FormStartPosition.CenterParent;
                designerForm.ShowDialog(owner);
            }

            if (ViewDesigner != null)
                ViewDesigner.ReloadColumns();

            if (ViewSelectorBar != null)
                ViewSelectorBar.RefreshViews();
        }

        public bool ConfirmPendingChangesBeforeClose()
        {
            return Manager == null || Manager.ConfirmPendingChangesBeforeClose(owner);
        }

        public void PersistCurrentViewName()
        {
            if (Manager != null)
                Manager.PersistCurrentViewName();
        }

        /// <summary>
        /// Construye el botón y contenedor visual de configuración de vistas.
        /// El formulario host solo reserva el panel; el módulo de vistas decide
        /// cómo presentar y conectar su acción de configuración.
        /// </summary>
        private void BuildConfigToolStrip()
        {
            if (toolbarConfigPanel == null)
                return;

            configToolStrip = new ToolStripEx
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.White,
                ForeColor = System.Drawing.Color.MidnightBlue,
                ImageScalingSize = new System.Drawing.Size(28, 28),
                LauncherStyle = LauncherStyle.Office2007,
                LayoutStyle = ToolStripLayoutStyle.Flow,
                ShowCaption = false,
                ShowItemToolTips = true,
                VisualStyle = ToolStripExStyle.Metro,
                Office12Mode = false,
                OfficeColorScheme = ToolStripEx.ColorScheme.Managed,
                ThemeName = "Metro",
                Padding = new Padding(0, 0, 1, 0),
                TabStop = false
            };

            configToolStrip.ThemeStyle.DropDownStyle.HoverItemBackColor =
                System.Drawing.Color.FromArgb(230, 230, 230);

            configToolStrip.ThemeStyle.HoverItemBackColor =
                System.Drawing.Color.FromArgb(218, 218, 218);

            configToolStrip.Paint += ConfigToolStrip_Paint;

            configButtonFont = new System.Drawing.Font("Segoe UI Symbol", ConfigButtonFontSize);

            configButton = new ToolStripButton
            {
                Name = "btnConfig",
                Text = "⋮",
                ToolTipText = "Vistas del grid",
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                AutoSize = false,
                Width = ConfigButtonWidth,
                Height = ConfigButtonHeight,
                Font = configButtonFont,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };

            configToolStrip.Items.Add(configButton);
            toolbarConfigPanel.Controls.Add(configToolStrip);
        }

        private void BuildDesignerHost()
        {
            ViewDesignerHostPanel = new Panel
            {
                Name = "ViewDesignerHostPanel",
                Dock = DockStyle.Fill,
                Visible = false,
                BackColor = System.Drawing.Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0),
                TabStop = false
            };

            ViewDesigner = new VistasDesignerPanel
            {
                Dock = DockStyle.Fill
            };

            ViewDesigner.CloseRequested += ViewDesigner_CloseRequested;
            ViewDesignerHostPanel.Controls.Add(ViewDesigner);

            if (workspacePanel != null)
            {
                workspacePanel.Controls.Add(gridPanel, 0, 0);
                workspacePanel.Controls.Add(ViewDesignerHostPanel, 1, 0);
            }
        }

        private void BuildManager()
        {
            Manager = new VistasAdministrador(owner, grid, configButton, BuildGridViewKey());

            Manager.RefreshViewsRequested = delegate
            {
                if (ViewSelectorBar != null)
                    ViewSelectorBar.RefreshViews();
            };

            Manager.Initialize();

            Manager.AttachFilters(
                CaptureFiltersXml,
                ApplyFiltersXml,
                ExecuteFiltersSearch);

            HookFilterRuntimeTracking();

            Manager.DesignerRequested += Manager_DesignerRequested;
            Manager.ViewChanged += Manager_ViewChanged;

            if (ViewDesigner != null)
                ViewDesigner.Attach(Manager);
        }

        private void BuildSelectorBar()
        {
            if (viewInfoPanel == null)
                return;

            viewInfoPanel.SuspendLayout();

            ViewSelectorBar = new VistasSelector
            {
                Dock = DockStyle.Fill
            };

            viewInfoPanel.Controls.Add(ViewSelectorBar);
            ViewSelectorBar.Attach(Manager);

            viewInfoPanel.ResumeLayout(true);
        }

        private void HookConfigButton()
        {
            if (configButton != null)
                configButton.Click += ConfigButton_Click;
        }

        private string BuildGridViewKey()
        {
            if (owner == null || grid == null)
                return string.Empty;

            return owner.GetType().FullName + "." + grid.Name;
        }

        private string CaptureFiltersXml()
        {
            if (filters == null)
                return null;

            GenesysGridFilterState state = filters.GetState();

            if (state == null)
                return null;

            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(GenesysGridFilterState));

            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, state);
                return writer.ToString();
            }
        }

        private void ApplyFiltersXml(string xml)
        {
            if (filters == null)
                return;

            suppressFilterRuntimeTracking = true;

            try
            {
                if (string.IsNullOrWhiteSpace(xml))
                {
                    filters.ApplyState(new GenesysGridFilterState());
                    return;
                }

                var serializer = new System.Xml.Serialization.XmlSerializer(typeof(GenesysGridFilterState));

                using (var reader = new StringReader(xml))
                {
                    GenesysGridFilterState state = serializer.Deserialize(reader) as GenesysGridFilterState;

                    if (state != null)
                        filters.ApplyState(state);
                }
            }
            finally
            {
                suppressFilterRuntimeTracking = false;
            }
        }

        private void HookFilterRuntimeTracking()
        {
            UnhookFilterRuntimeTracking();

            if (filters == null)
                return;

            HookFilterControlRecursive(filters);
        }

        private void HookFilterControlRecursive(Control control)
        {
            if (control == null)
                return;

            HookFilterControl(control);

            foreach (Control child in control.Controls)
                HookFilterControlRecursive(child);
        }

        private void HookFilterControl(Control control)
        {
            if (control == null)
                return;

            control.TextChanged += FilterControl_Changed;
            filterControlSubscriptions.Add(new FilterControlSubscription(control, "TextChanged", FilterControl_Changed));

            DateTimePicker dateTimePicker = control as DateTimePicker;
            if (dateTimePicker != null)
            {
                dateTimePicker.ValueChanged += FilterControl_Changed;
                filterControlSubscriptions.Add(new FilterControlSubscription(dateTimePicker, "ValueChanged", FilterControl_Changed));
            }

            ComboBox comboBox = control as ComboBox;
            if (comboBox != null)
            {
                comboBox.SelectedIndexChanged += FilterControl_Changed;
                filterControlSubscriptions.Add(new FilterControlSubscription(comboBox, "SelectedIndexChanged", FilterControl_Changed));
            }

            CheckBox checkBox = control as CheckBox;
            if (checkBox != null)
            {
                checkBox.CheckedChanged += FilterControl_Changed;
                filterControlSubscriptions.Add(new FilterControlSubscription(checkBox, "CheckedChanged", FilterControl_Changed));
            }

            RadioButton radioButton = control as RadioButton;
            if (radioButton != null)
            {
                radioButton.CheckedChanged += FilterControl_Changed;
                filterControlSubscriptions.Add(new FilterControlSubscription(radioButton, "CheckedChanged", FilterControl_Changed));
            }

            NumericUpDown numericUpDown = control as NumericUpDown;
            if (numericUpDown != null)
            {
                numericUpDown.ValueChanged += FilterControl_Changed;
                filterControlSubscriptions.Add(new FilterControlSubscription(numericUpDown, "ValueChanged", FilterControl_Changed));
            }
        }

        private void UnhookFilterRuntimeTracking()
        {
            foreach (FilterControlSubscription subscription in filterControlSubscriptions.ToArray())
                subscription.Detach();

            filterControlSubscriptions.Clear();
        }

        private void FilterControl_Changed(object sender, EventArgs e)
        {
            NotifyFilterPanelRuntimeChanged();
        }

        private void NotifyFilterPanelRuntimeChanged()
        {
            if (suppressFilterRuntimeTracking || disposed || Manager == null)
                return;

            Manager.MarkUpperFiltersChanged(CaptureFiltersXml());
        }

        private sealed class FilterControlSubscription
        {
            private readonly Control control;
            private readonly string eventName;
            private readonly EventHandler handler;

            public FilterControlSubscription(Control control, string eventName, EventHandler handler)
            {
                this.control = control;
                this.eventName = eventName;
                this.handler = handler;
            }

            public void Detach()
            {
                if (control == null || handler == null)
                    return;

                if (eventName == "TextChanged")
                    control.TextChanged -= handler;

                DateTimePicker dateTimePicker = control as DateTimePicker;
                if (dateTimePicker != null && eventName == "ValueChanged")
                    dateTimePicker.ValueChanged -= handler;

                ComboBox comboBox = control as ComboBox;
                if (comboBox != null && eventName == "SelectedIndexChanged")
                    comboBox.SelectedIndexChanged -= handler;

                CheckBox checkBox = control as CheckBox;
                if (checkBox != null && eventName == "CheckedChanged")
                    checkBox.CheckedChanged -= handler;

                RadioButton radioButton = control as RadioButton;
                if (radioButton != null && eventName == "CheckedChanged")
                    radioButton.CheckedChanged -= handler;

                NumericUpDown numericUpDown = control as NumericUpDown;
                if (numericUpDown != null && eventName == "ValueChanged")
                    numericUpDown.ValueChanged -= handler;
            }
        }

        private void ExecuteFiltersSearch()
        {
            if (filters != null)
                filters.RefreshSearch();
        }

        private void ConfigToolStrip_Paint(object sender, PaintEventArgs e)
        {
            if (configToolStrip == null)
                return;

            using (var pen = new System.Drawing.Pen(System.Drawing.Color.LightGray, 1))
            {
                e.Graphics.DrawLine(pen, 0, 0, configToolStrip.Width, 0);
            }
        }

        private void ConfigButton_Click(object sender, EventArgs e)
        {
            ShowGridConfigMenu();
        }

        private void ViewDesigner_CloseRequested(object sender, EventArgs e)
        {
            HideViewDesigner();
        }

        private void Manager_DesignerRequested(object sender, EventArgs e)
        {
            ShowViewDesignerForm();
        }

        /// <summary>
        /// Sincroniza la barra horizontal cuando cambia la vista activa.
        /// El administrador ya protege el grid durante bind/layout; aquí solo se
        /// repinta el selector para reflejar la vista actualmente seleccionada.
        /// </summary>
        private void Manager_ViewChanged(object sender, EventArgs e)
        {
            if (ViewSelectorBar != null)
                ViewSelectorBar.RefreshViews();
        }

        public void Dispose()
        {
            if (disposed)
                return;

            if (configButton != null)
                configButton.Click -= ConfigButton_Click;

            UnhookFilterRuntimeTracking();

            if (configToolStrip != null)
                configToolStrip.Paint -= ConfigToolStrip_Paint;

            if (ViewDesigner != null)
                ViewDesigner.CloseRequested -= ViewDesigner_CloseRequested;

            if (Manager != null)
            {
                Manager.DesignerRequested -= Manager_DesignerRequested;
                Manager.ViewChanged -= Manager_ViewChanged;
                Manager.Dispose();
                Manager = null;
            }

            if (configButtonFont != null)
            {
                configButtonFont.Dispose();
                configButtonFont = null;
            }

            disposed = true;
        }
    }
}
