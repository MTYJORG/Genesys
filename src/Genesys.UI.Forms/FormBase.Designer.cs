namespace Genesys.UI.Forms
{
    partial class FormBase
    {
        private System.ComponentModel.IContainer components = null;

        // Controles principales
        protected System.Windows.Forms.Panel panelBotones;
        protected System.Windows.Forms.Panel panelFiltros;
        protected System.Windows.Forms.Panel panelContenido;
        protected Syncfusion.WinForms.DataGrid.SfDataGrid syncGrid;
        protected System.Windows.Forms.BindingNavigator navigator;
        protected System.Windows.Forms.BindingSource bindingSource;
        protected System.Windows.Forms.ToolStripLabel lblInfoRegistros;

        // Botones 
        protected System.Windows.Forms.Button btnNuevo;
        protected System.Windows.Forms.Button btnEditar;
        protected System.Windows.Forms.Button btnGuardarVista;
        protected System.Windows.Forms.Button btnCargarVista;
        protected System.Windows.Forms.Button btnExcel;
        protected System.Windows.Forms.Button btnPdf;
        protected System.Windows.Forms.Button btnSalir;
        protected System.Windows.Forms.DateTimePicker datFechaInicio;
        protected System.Windows.Forms.DateTimePicker datFechaFinal;
        protected System.Windows.Forms.ComboBox cbxFiltroStatus;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.panelBotones = new System.Windows.Forms.Panel();
            this.btnNuevo = new System.Windows.Forms.Button();
            this.btnEditar = new System.Windows.Forms.Button();
            this.btnGuardarVista = new System.Windows.Forms.Button();
            this.btnCargarVista = new System.Windows.Forms.Button();
            this.btnExcel = new System.Windows.Forms.Button();
            this.btnPdf = new System.Windows.Forms.Button();
            this.btnSalir = new System.Windows.Forms.Button();
            this.panelFiltros = new System.Windows.Forms.Panel();
            this.FiltroHistoria_L = new System.Windows.Forms.Label();
            this.lblCampoFiltro = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.aTxtCampoFiltro = new Genesys.UI.Controls.aTextBox();
            this.txtDescripcion = new System.Windows.Forms.TextBox();
            this.datFechaInicio = new System.Windows.Forms.DateTimePicker();
            this.datFechaFinal = new System.Windows.Forms.DateTimePicker();
            this.cbxFiltroStatus = new System.Windows.Forms.ComboBox();
            this.panelContenido = new System.Windows.Forms.Panel();
            this.syncGrid = new Syncfusion.WinForms.DataGrid.SfDataGrid();
            this.navigator = new System.Windows.Forms.BindingNavigator(this.components);
            this.bindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.lblInfoRegistros = new System.Windows.Forms.ToolStripLabel();
            this.panelMensajes = new Genesys.UI.Components.MensajesErrores();
            this.panelBotones.SuspendLayout();
            this.panelFiltros.SuspendLayout();
            this.panelContenido.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.syncGrid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.navigator)).BeginInit();
            this.navigator.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // panelBotones
            // 
            this.panelBotones.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(248)))));
            this.panelBotones.Controls.Add(this.btnNuevo);
            this.panelBotones.Controls.Add(this.btnEditar);
            this.panelBotones.Controls.Add(this.btnGuardarVista);
            this.panelBotones.Controls.Add(this.btnCargarVista);
            this.panelBotones.Controls.Add(this.btnExcel);
            this.panelBotones.Controls.Add(this.btnPdf);
            this.panelBotones.Controls.Add(this.btnSalir);
            this.panelBotones.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelBotones.Location = new System.Drawing.Point(0, 0);
            this.panelBotones.Name = "panelBotones";
            this.panelBotones.Padding = new System.Windows.Forms.Padding(4, 7, 4, 4);
            this.panelBotones.Size = new System.Drawing.Size(1029, 52);
            this.panelBotones.TabIndex = 3;
            // 
            // btnNuevo
            // 
            this.btnNuevo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(125)))), ((int)(((byte)(50)))));
            this.btnNuevo.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNuevo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNuevo.ForeColor = System.Drawing.Color.White;
            this.btnNuevo.Location = new System.Drawing.Point(9, 10);
            this.btnNuevo.Name = "btnNuevo";
            this.btnNuevo.Size = new System.Drawing.Size(77, 33);
            this.btnNuevo.TabIndex = 0;
            this.btnNuevo.Text = "➕ Nuevo";
            this.btnNuevo.UseVisualStyleBackColor = false;
            // 
            // btnEditar
            // 
            this.btnEditar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            this.btnEditar.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnEditar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEditar.ForeColor = System.Drawing.Color.White;
            this.btnEditar.Location = new System.Drawing.Point(90, 10);
            this.btnEditar.Name = "btnEditar";
            this.btnEditar.Size = new System.Drawing.Size(77, 33);
            this.btnEditar.TabIndex = 1;
            this.btnEditar.Text = "✏️ Editar";
            this.btnEditar.UseVisualStyleBackColor = false;
            // 
            // btnGuardarVista
            // 
            this.btnGuardarVista.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(156)))), ((int)(((byte)(39)))), ((int)(((byte)(176)))));
            this.btnGuardarVista.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnGuardarVista.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnGuardarVista.ForeColor = System.Drawing.Color.White;
            this.btnGuardarVista.Location = new System.Drawing.Point(171, 10);
            this.btnGuardarVista.Name = "btnGuardarVista";
            this.btnGuardarVista.Size = new System.Drawing.Size(86, 33);
            this.btnGuardarVista.TabIndex = 2;
            this.btnGuardarVista.Text = "💾 Guardar Vista";
            this.btnGuardarVista.UseVisualStyleBackColor = false;
            // 
            // btnCargarVista
            // 
            this.btnCargarVista.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(103)))), ((int)(((byte)(58)))), ((int)(((byte)(183)))));
            this.btnCargarVista.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCargarVista.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCargarVista.ForeColor = System.Drawing.Color.White;
            this.btnCargarVista.Location = new System.Drawing.Point(261, 10);
            this.btnCargarVista.Name = "btnCargarVista";
            this.btnCargarVista.Size = new System.Drawing.Size(81, 33);
            this.btnCargarVista.TabIndex = 3;
            this.btnCargarVista.Text = "📂 Cargar Vista";
            this.btnCargarVista.UseVisualStyleBackColor = false;
            // 
            // btnExcel
            // 
            this.btnExcel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(115)))), ((int)(((byte)(70)))));
            this.btnExcel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnExcel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExcel.ForeColor = System.Drawing.Color.White;
            this.btnExcel.Location = new System.Drawing.Point(347, 10);
            this.btnExcel.Name = "btnExcel";
            this.btnExcel.Size = new System.Drawing.Size(69, 33);
            this.btnExcel.TabIndex = 4;
            this.btnExcel.Text = "📊 Excel";
            this.btnExcel.UseVisualStyleBackColor = false;
            // 
            // btnPdf
            // 
            this.btnPdf.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(183)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.btnPdf.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPdf.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPdf.ForeColor = System.Drawing.Color.White;
            this.btnPdf.Location = new System.Drawing.Point(420, 10);
            this.btnPdf.Name = "btnPdf";
            this.btnPdf.Size = new System.Drawing.Size(69, 33);
            this.btnPdf.TabIndex = 5;
            this.btnPdf.Text = "📄 PDF";
            this.btnPdf.UseVisualStyleBackColor = false;
            // 
            // btnSalir
            // 
            this.btnSalir.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(120)))), ((int)(((byte)(120)))));
            this.btnSalir.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSalir.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSalir.ForeColor = System.Drawing.Color.White;
            this.btnSalir.Location = new System.Drawing.Point(493, 10);
            this.btnSalir.Name = "btnSalir";
            this.btnSalir.Size = new System.Drawing.Size(69, 33);
            this.btnSalir.TabIndex = 6;
            this.btnSalir.Text = "🚪 Salir";
            this.btnSalir.UseVisualStyleBackColor = false;
            // 
            // panelFiltros
            // 
            this.panelFiltros.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.panelFiltros.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelFiltros.Controls.Add(this.FiltroHistoria_L);
            this.panelFiltros.Controls.Add(this.lblCampoFiltro);
            this.panelFiltros.Controls.Add(this.label2);
            this.panelFiltros.Controls.Add(this.label1);
            this.panelFiltros.Controls.Add(this.aTxtCampoFiltro);
            this.panelFiltros.Controls.Add(this.txtDescripcion);
            this.panelFiltros.Controls.Add(this.datFechaInicio);
            this.panelFiltros.Controls.Add(this.datFechaFinal);
            this.panelFiltros.Controls.Add(this.cbxFiltroStatus);
            this.panelFiltros.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelFiltros.Location = new System.Drawing.Point(0, 101);
            this.panelFiltros.Name = "panelFiltros";
            this.panelFiltros.Padding = new System.Windows.Forms.Padding(9, 7, 9, 4);
            this.panelFiltros.Size = new System.Drawing.Size(1029, 74);
            this.panelFiltros.TabIndex = 2;
            // 
            // FiltroHistoria_L
            // 
            this.FiltroHistoria_L.AutoSize = true;
            this.FiltroHistoria_L.Location = new System.Drawing.Point(734, 22);
            this.FiltroHistoria_L.Name = "FiltroHistoria_L";
            this.FiltroHistoria_L.Size = new System.Drawing.Size(40, 13);
            this.FiltroHistoria_L.TabIndex = 15;
            this.FiltroHistoria_L.Text = "Status:";
            // 
            // lblCampoFiltro
            // 
            this.lblCampoFiltro.Location = new System.Drawing.Point(206, 21);
            this.lblCampoFiltro.Name = "lblCampoFiltro";
            this.lblCampoFiltro.Size = new System.Drawing.Size(73, 15);
            this.lblCampoFiltro.TabIndex = 14;
            this.lblCampoFiltro.Text = "Campo Filtro:";
            this.lblCampoFiltro.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(8, 39);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(32, 13);
            this.label2.TabIndex = 13;
            this.label2.Text = "Final:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 13);
            this.label1.TabIndex = 12;
            this.label1.Text = "Inicio:";
            // 
            // aTxtCampoFiltro
            // 
            this.aTxtCampoFiltro.BackFocusColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(235)))), ((int)(((byte)(249)))));
            this.aTxtCampoFiltro.EsLookup = false;
            this.aTxtCampoFiltro.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.aTxtCampoFiltro.Location = new System.Drawing.Point(282, 17);
            this.aTxtCampoFiltro.LookupAutoValidar = true;
            this.aTxtCampoFiltro.LookupControl = null;
            this.aTxtCampoFiltro.LookupImage = null;
            this.aTxtCampoFiltro.LookupProvider = null;
            this.aTxtCampoFiltro.Mask = null;
            this.aTxtCampoFiltro.Mayusculas = false;
            this.aTxtCampoFiltro.MostrarWatermark = true;
            this.aTxtCampoFiltro.Name = "aTxtCampoFiltro";
            this.aTxtCampoFiltro.PermitirNegativos = false;
            this.aTxtCampoFiltro.Size = new System.Drawing.Size(100, 23);
            this.aTxtCampoFiltro.TabIndex = 11;
            // 
            // txtDescripcion
            // 
            this.txtDescripcion.Location = new System.Drawing.Point(388, 18);
            this.txtDescripcion.Name = "txtDescripcion";
            this.txtDescripcion.Size = new System.Drawing.Size(266, 20);
            this.txtDescripcion.TabIndex = 10;
            // 
            // datFechaInicio
            // 
            this.datFechaInicio.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.datFechaInicio.Location = new System.Drawing.Point(49, 9);
            this.datFechaInicio.Name = "datFechaInicio";
            this.datFechaInicio.Size = new System.Drawing.Size(103, 20);
            this.datFechaInicio.TabIndex = 1;
            this.datFechaInicio.Value = new System.DateTime(2026, 3, 30, 18, 45, 31, 569);
            // 
            // datFechaFinal
            // 
            this.datFechaFinal.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.datFechaFinal.Location = new System.Drawing.Point(49, 35);
            this.datFechaFinal.Name = "datFechaFinal";
            this.datFechaFinal.Size = new System.Drawing.Size(103, 20);
            this.datFechaFinal.TabIndex = 3;
            this.datFechaFinal.Value = new System.DateTime(2026, 4, 30, 18, 45, 31, 571);
            // 
            // cbxFiltroStatus
            // 
            this.cbxFiltroStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbxFiltroStatus.Items.AddRange(new object[] {
            "Todos",
            "Activo",
            "Inactivo"});
            this.cbxFiltroStatus.Location = new System.Drawing.Point(780, 18);
            this.cbxFiltroStatus.Name = "cbxFiltroStatus";
            this.cbxFiltroStatus.Size = new System.Drawing.Size(103, 21);
            this.cbxFiltroStatus.TabIndex = 7;
            // 
            // panelContenido
            // 
            this.panelContenido.Controls.Add(this.syncGrid);
            this.panelContenido.Controls.Add(this.navigator);
            this.panelContenido.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelContenido.Location = new System.Drawing.Point(0, 175);
            this.panelContenido.Name = "panelContenido";
            this.panelContenido.Padding = new System.Windows.Forms.Padding(3);
            this.panelContenido.Size = new System.Drawing.Size(1029, 347);
            this.panelContenido.TabIndex = 0;
            // 
            // syncGrid
            // 
            this.syncGrid.AccessibleName = "Table";
            this.syncGrid.AllowEditing = false;
            this.syncGrid.AllowFiltering = true;
            this.syncGrid.AllowResizingColumns = true;
            this.syncGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.syncGrid.Location = new System.Drawing.Point(3, 3);
            this.syncGrid.Name = "syncGrid";
            this.syncGrid.ShowRowHeader = true;
            this.syncGrid.Size = new System.Drawing.Size(1023, 316);
            this.syncGrid.TabIndex = 0;
            // 
            // navigator
            // 
            this.navigator.AddNewItem = null;
            this.navigator.BindingSource = this.bindingSource;
            this.navigator.CountItem = null;
            this.navigator.DeleteItem = null;
            this.navigator.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.navigator.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.navigator.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblInfoRegistros});
            this.navigator.Location = new System.Drawing.Point(3, 319);
            this.navigator.MoveFirstItem = null;
            this.navigator.MoveLastItem = null;
            this.navigator.MoveNextItem = null;
            this.navigator.MovePreviousItem = null;
            this.navigator.Name = "navigator";
            this.navigator.PositionItem = null;
            this.navigator.Size = new System.Drawing.Size(1023, 25);
            this.navigator.TabIndex = 1;
            // 
            // lblInfoRegistros
            // 
            this.lblInfoRegistros.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.lblInfoRegistros.Name = "lblInfoRegistros";
            this.lblInfoRegistros.Size = new System.Drawing.Size(55, 22);
            this.lblInfoRegistros.Text = "Sin datos";
            // 
            // panelMensajes
            // 
            this.panelMensajes.AutoScroll = true;
            this.panelMensajes.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panelMensajes.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelMensajes.Location = new System.Drawing.Point(0, 52);
            this.panelMensajes.Name = "panelMensajes";
            this.panelMensajes.Size = new System.Drawing.Size(1029, 49);
            this.panelMensajes.TabIndex = 4;
            // 
            // FormBase
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1029, 527);
            this.Controls.Add(this.panelContenido);
            this.Controls.Add(this.panelFiltros);
            this.Controls.Add(this.panelMensajes);
            this.Controls.Add(this.panelBotones);
            this.Name = "FormBase";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Formulario Maestro";
            this.panelBotones.ResumeLayout(false);
            this.panelFiltros.ResumeLayout(false);
            this.panelFiltros.PerformLayout();
            this.panelContenido.ResumeLayout(false);
            this.panelContenido.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.syncGrid)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.navigator)).EndInit();
            this.navigator.ResumeLayout(false);
            this.navigator.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSource)).EndInit();
            this.ResumeLayout(false);

        }

        protected System.Windows.Forms.TextBox txtDescripcion;
        protected Genesys.UI.Controls.aTextBox aTxtCampoFiltro;
        protected Genesys.UI.Components.MensajesErrores panelMensajes;
        protected System.Windows.Forms.Label label2;
        protected System.Windows.Forms.Label label1;
        protected System.Windows.Forms.Label lblCampoFiltro;
        protected System.Windows.Forms.Label FiltroHistoria_L;
        //private ControlesPersonalizados.aTextBox aTxtCodigo;
    }
}