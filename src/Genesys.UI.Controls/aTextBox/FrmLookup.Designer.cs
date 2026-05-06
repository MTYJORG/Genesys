namespace Genesys.UI.Controls
{
    partial class FrmLookup
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblBuscar;
        private aTextBox txtBuscar;
        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.Label lblRegistros;
        private System.Windows.Forms.Button btnCancelar;
        private System.Windows.Forms.BindingSource bindingSource1;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            this.lblBuscar = new System.Windows.Forms.Label();
            this.txtBuscar = new Genesys.UI.Controls.aTextBox();
            this.dgv = new System.Windows.Forms.DataGridView();
            this.lblRegistros = new System.Windows.Forms.Label();
            this.btnCancelar = new System.Windows.Forms.Button();
            this.bindingSource1 = new System.Windows.Forms.BindingSource(this.components);

            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSource1)).BeginInit();

            this.SuspendLayout();

            // lblBuscar
            this.lblBuscar.AutoSize = true;
            this.lblBuscar.Location = new System.Drawing.Point(10, 15);
            this.lblBuscar.Name = "lblBuscar";
            this.lblBuscar.Size = new System.Drawing.Size(45, 15);
            this.lblBuscar.Text = "Buscar:";

            // txtBuscar
            this.txtBuscar.Location = new System.Drawing.Point(60, 12);
            this.txtBuscar.Name = "txtBuscar";
            this.txtBuscar.Size = new System.Drawing.Size(200, 23);
            this.txtBuscar._TextChanged += new System.EventHandler(this.txtBuscar_TextChanged);

            // dgv
            this.dgv.AllowUserToAddRows = false;
            this.dgv.AllowUserToDeleteRows = false;
            this.dgv.AllowUserToOrderColumns = true;
            this.dgv.BackgroundColor = System.Drawing.SystemColors.ControlDark;
            this.dgv.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgv.Location = new System.Drawing.Point(10, 45);
            this.dgv.Name = "dgv";
            this.dgv.ReadOnly = true;
            this.dgv.MultiSelect = false;
            this.dgv.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgv.RowHeadersVisible = false;
            this.dgv.Size = new System.Drawing.Size(300, 150);

            this.dgv.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgv_CellDoubleClick);
            this.dgv.KeyDown += new System.Windows.Forms.KeyEventHandler(this.dgv_KeyDown);

            // lblRegistros
            this.lblRegistros.Location = new System.Drawing.Point(10, 205);
            this.lblRegistros.Name = "lblRegistros";
            this.lblRegistros.Size = new System.Drawing.Size(200, 20);
            this.lblRegistros.Text = "0 Registros";

            // btnCancelar
            this.btnCancelar.Location = new System.Drawing.Point(210, 200);
            this.btnCancelar.Name = "btnCancelar";
            this.btnCancelar.Size = new System.Drawing.Size(100, 25);
            this.btnCancelar.Text = "Cancelar";
            this.btnCancelar.DialogResult = System.Windows.Forms.DialogResult.Cancel;

            // FrmLookup
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(320, 240);

            this.Controls.Add(this.lblBuscar);
            this.Controls.Add(this.txtBuscar);
            this.Controls.Add(this.dgv);
            this.Controls.Add(this.lblRegistros);
            this.Controls.Add(this.btnCancelar);

            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowIcon = false;
            this.ShowInTaskbar = false;

            this.Name = "FrmLookup";
            this.Text = "Buscar";

            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSource1)).EndInit();

            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}