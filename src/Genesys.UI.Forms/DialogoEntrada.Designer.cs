namespace Genesys.UI.Forms.Dialogos
{
    partial class DialogoEntrada
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblIndicacion;
        private System.Windows.Forms.TextBox txtEntrada;
        private System.Windows.Forms.Button btnAceptar;
        private System.Windows.Forms.Button btnCancelar;
        private System.Windows.Forms.Label lblAdvertencia;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblIndicacion = new System.Windows.Forms.Label();
            this.txtEntrada = new System.Windows.Forms.TextBox();
            this.btnAceptar = new System.Windows.Forms.Button();
            this.btnCancelar = new System.Windows.Forms.Button();
            this.lblAdvertencia = new System.Windows.Forms.Label();
            this.SuspendLayout();

            // lblIndicacion
            this.lblIndicacion.AutoSize = true;
            this.lblIndicacion.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lblIndicacion.Location = new System.Drawing.Point(15, 20);
            this.lblIndicacion.Text = "Indicación:";

            // txtEntrada
            this.txtEntrada.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtEntrada.Location = new System.Drawing.Point(15, 50);
            this.txtEntrada.Size = new System.Drawing.Size(410, 28);

            // lblAdvertencia
            this.lblAdvertencia.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblAdvertencia.ForeColor = System.Drawing.Color.DarkOrange;
            this.lblAdvertencia.Location = new System.Drawing.Point(15, 85);
            this.lblAdvertencia.Size = new System.Drawing.Size(410, 40);
            this.lblAdvertencia.Visible = false;

            // btnAceptar
            this.btnAceptar.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            this.btnAceptar.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnAceptar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAceptar.ForeColor = System.Drawing.Color.White;
            this.btnAceptar.Location = new System.Drawing.Point(220, 135);
            this.btnAceptar.Size = new System.Drawing.Size(100, 35);
            this.btnAceptar.Text = "Aceptar";
            this.btnAceptar.UseVisualStyleBackColor = false;

            // btnCancelar
            this.btnCancelar.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancelar.Location = new System.Drawing.Point(330, 135);
            this.btnCancelar.Size = new System.Drawing.Size(95, 35);
            this.btnCancelar.Text = "Cancelar";

            // DialogoEntrada
            this.AcceptButton = this.btnAceptar;
            this.CancelButton = this.btnCancelar;
            this.ClientSize = new System.Drawing.Size(450, 190);
            this.Controls.Add(this.lblIndicacion);
            this.Controls.Add(this.txtEntrada);
            this.Controls.Add(this.lblAdvertencia);
            this.Controls.Add(this.btnAceptar);
            this.Controls.Add(this.btnCancelar);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DialogoEntrada";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Entrada de Datos";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}