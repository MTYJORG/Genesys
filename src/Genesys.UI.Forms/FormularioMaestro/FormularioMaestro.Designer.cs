namespace ComponentesComunes
{
    partial class FormularioMaestro
    {
        private System.ComponentModel.IContainer components = null;

        // Controles principales
        private System.Windows.Forms.Panel panelBotones;
        private System.Windows.Forms.Panel panelFiltros;
        private System.Windows.Forms.Panel panelContenido;
        private Syncfusion.WinForms.DataGrid.SfDataGrid mainGrid;
        private System.Windows.Forms.BindingNavigator navigator;
        private System.Windows.Forms.BindingSource bindingSource;
        private System.Windows.Forms.ToolStripLabel lblInfoRegistros;

        // Botones 
        protected System.Windows.Forms.Button btnNuevo;
        protected System.Windows.Forms.Button btnEditar;
        protected System.Windows.Forms.Button btnGuardarVista;
        protected System.Windows.Forms.Button btnCargarVista;
        protected System.Windows.Forms.Button btnExcel;
        protected System.Windows.Forms.Button btnPdf;
        protected System.Windows.Forms.Button btnSalir;

        // Controles de filtro (accesibles desde el código)
        private System.Windows.Forms.Label lblDesde;
        protected System.Windows.Forms.DateTimePicker dtpDesde;
        private System.Windows.Forms.Label lblHasta;
        protected System.Windows.Forms.DateTimePicker dtpHasta;
        private System.Windows.Forms.Label lblBuscar;
        private System.Windows.Forms.Label lblEstado;
        protected System.Windows.Forms.ComboBox cmbEstado;

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
            this.aTxtCodigo = new Genesys.UI.Controls.aTextBox();
            this.txtDescripcion = new System.Windows.Forms.TextBox();
            this.lblDesde = new System.Windows.Forms.Label();
            this.dtpDesde = new System.Windows.Forms.DateTimePicker();
            this.lblHasta = new System.Windows.Forms.Label();
            this.dtpHasta = new System.Windows.Forms.DateTimePicker();
            this.lblBuscar = new System.Windows.Forms.Label();
            this.lblEstado = new System.Windows.Forms.Label();
            this.cmbEstado = new System.Windows.Forms.ComboBox();
            this.panelContenido = new System.Windows.Forms.Panel();
            this.mainGrid = new Syncfusion.WinForms.DataGrid.SfDataGrid();
            this.navigator = new System.Windows.Forms.BindingNavigator(this.components);
            this.bindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.lblInfoRegistros = new System.Windows.Forms.ToolStripLabel();
            this.panelMensajes = new Juguera.MensajesErrores();
            this.panelBotones.SuspendLayout();
            this.panelFiltros.SuspendLayout();
            this.panelContenido.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mainGrid)).BeginInit();
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
            this.panelFiltros.Controls.Add(this.aTxtCodigo);
            this.panelFiltros.Controls.Add(this.txtDescripcion);
            this.panelFiltros.Controls.Add(this.lblDesde);
            this.panelFiltros.Controls.Add(this.dtpDesde);
            this.panelFiltros.Controls.Add(this.lblHasta);
            this.panelFiltros.Controls.Add(this.dtpHasta);
            this.panelFiltros.Controls.Add(this.lblBuscar);
            this.panelFiltros.Controls.Add(this.lblEstado);
            this.panelFiltros.Controls.Add(this.cmbEstado);
            this.panelFiltros.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelFiltros.Location = new System.Drawing.Point(0, 107);
            this.panelFiltros.Name = "panelFiltros";
            this.panelFiltros.Padding = new System.Windows.Forms.Padding(9, 7, 9, 4);
            this.panelFiltros.Size = new System.Drawing.Size(1029, 70);
            this.panelFiltros.TabIndex = 2;
            // 
            // aTxtCodigo
            // 
            this.aTxtCodigo.BackFocusColor = System.Drawing.Color.FromArgb(((int)(((byte)(217)))), ((int)(((byte)(235)))), ((int)(((byte)(249)))));
            this.aTxtCodigo.EsLookup = false;
            this.aTxtCodigo.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.aTxtCodigo.Location = new System.Drawing.Point(282, 27);
            this.aTxtCodigo.LookupAutoValidar = true;
            this.aTxtCodigo.LookupControl = null;
            this.aTxtCodigo.LookupImage = null;
            this.aTxtCodigo.LookupProvider = null;
            this.aTxtCodigo.Mask = null;
            this.aTxtCodigo.Mayusculas = false;
            this.aTxtCodigo.MostrarWatermark = true;
            this.aTxtCodigo.Name = "aTxtCodigo";
            this.aTxtCodigo.PermitirNegativos = false;
            this.aTxtCodigo.Size = new System.Drawing.Size(100, 23);
            this.aTxtCodigo.TabIndex = 11;
            // 
            // txtDescripcion
            // 
            this.txtDescripcion.Location = new System.Drawing.Point(388, 28);
            this.txtDescripcion.Name = "txtDescripcion";
            this.txtDescripcion.Size = new System.Drawing.Size(266, 20);
            this.txtDescripcion.TabIndex = 10;
            // 
            // lblDesde
            // 
            this.lblDesde.AutoSize = true;
            this.lblDesde.Location = new System.Drawing.Point(9, 10);
            this.lblDesde.Name = "lblDesde";
            this.lblDesde.Size = new System.Drawing.Size(41, 13);
            this.lblDesde.TabIndex = 0;
            this.lblDesde.Text = "Desde:";
            // 
            // dtpDesde
            // 
            this.dtpDesde.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpDesde.Location = new System.Drawing.Point(9, 28);
            this.dtpDesde.Name = "dtpDesde";
            this.dtpDesde.Size = new System.Drawing.Size(103, 20);
            this.dtpDesde.TabIndex = 1;
            this.dtpDesde.Value = new System.DateTime(2026, 3, 30, 18, 45, 31, 569);
            // 
            // lblHasta
            // 
            this.lblHasta.AutoSize = true;
            this.lblHasta.Location = new System.Drawing.Point(124, 10);
            this.lblHasta.Name = "lblHasta";
            this.lblHasta.Size = new System.Drawing.Size(38, 13);
            this.lblHasta.TabIndex = 2;
            this.lblHasta.Text = "Hasta:";
            // 
            // dtpHasta
            // 
            this.dtpHasta.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpHasta.Location = new System.Drawing.Point(124, 28);
            this.dtpHasta.Name = "dtpHasta";
            this.dtpHasta.Size = new System.Drawing.Size(103, 20);
            this.dtpHasta.TabIndex = 3;
            this.dtpHasta.Value = new System.DateTime(2026, 4, 30, 18, 45, 31, 571);
            // 
            // lblBuscar
            // 
            this.lblBuscar.AutoSize = true;
            this.lblBuscar.Location = new System.Drawing.Point(279, 11);
            this.lblBuscar.Name = "lblBuscar";
            this.lblBuscar.Size = new System.Drawing.Size(43, 13);
            this.lblBuscar.TabIndex = 4;
            this.lblBuscar.Text = "Buscar:";
            // 
            // lblEstado
            // 
            this.lblEstado.AutoSize = true;
            this.lblEstado.Location = new System.Drawing.Point(780, 7);
            this.lblEstado.Name = "lblEstado";
            this.lblEstado.Size = new System.Drawing.Size(43, 13);
            this.lblEstado.TabIndex = 6;
            this.lblEstado.Text = "Estado:";
            // 
            // cmbEstado
            // 
            this.cmbEstado.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbEstado.Items.AddRange(new object[] {
            "Todos",
            "Activo",
            "Inactivo"});
            this.cmbEstado.Location = new System.Drawing.Point(780, 25);
            this.cmbEstado.Name = "cmbEstado";
            this.cmbEstado.Size = new System.Drawing.Size(103, 21);
            this.cmbEstado.TabIndex = 7;
            // 
            // panelContenido
            // 
            this.panelContenido.Controls.Add(this.mainGrid);
            this.panelContenido.Controls.Add(this.navigator);
            this.panelContenido.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelContenido.Location = new System.Drawing.Point(0, 177);
            this.panelContenido.Name = "panelContenido";
            this.panelContenido.Padding = new System.Windows.Forms.Padding(3);
            this.panelContenido.Size = new System.Drawing.Size(1029, 347);
            this.panelContenido.TabIndex = 0;
            // 
            // mainGrid
            // 
            this.mainGrid.AccessibleName = "Table";
            this.mainGrid.AllowEditing = false;
            this.mainGrid.AllowFiltering = true;
            this.mainGrid.AllowResizingColumns = true;
            this.mainGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainGrid.Location = new System.Drawing.Point(3, 3);
            this.mainGrid.Name = "mainGrid";
            this.mainGrid.ShowRowHeader = true;
            this.mainGrid.Size = new System.Drawing.Size(1023, 316);
            this.mainGrid.TabIndex = 0;
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
            this.panelMensajes.Size = new System.Drawing.Size(1029, 55);
            this.panelMensajes.TabIndex = 4;
            // 
            // FormularioMaestro
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1029, 527);
            this.Controls.Add(this.panelContenido);
            this.Controls.Add(this.panelFiltros);
            this.Controls.Add(this.panelMensajes);
            this.Controls.Add(this.panelBotones);
            this.Name = "FormularioMaestro";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Formulario Maestro";
            this.panelBotones.ResumeLayout(false);
            this.panelFiltros.ResumeLayout(false);
            this.panelFiltros.PerformLayout();
            this.panelContenido.ResumeLayout(false);
            this.panelContenido.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mainGrid)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.navigator)).EndInit();
            this.navigator.ResumeLayout(false);
            this.navigator.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSource)).EndInit();
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.TextBox txtDescripcion;
        private Genesys.UI.Controls.aTextBox aTxtCodigo;
        private Juguera.MensajesErrores panelMensajes;
        //private ControlesPersonalizados.aTextBox aTxtCodigo;
    }
}