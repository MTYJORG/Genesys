namespace ComponentesComunes.Dialogos
{
    partial class DialogoSeleccion
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ListBox lstOpciones;
        private System.Windows.Forms.Button btnAceptar;
        private System.Windows.Forms.Button btnCancelar;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lstOpciones = new System.Windows.Forms.ListBox();
            this.btnAceptar = new System.Windows.Forms.Button();
            this.btnCancelar = new System.Windows.Forms.Button();
            this.SuspendLayout();

            // lstOpciones
            this.lstOpciones.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstOpciones.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.lstOpciones.FormattingEnabled = true;
            this.lstOpciones.ItemHeight = 23;

            // btnAceptar
            this.btnAceptar.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnAceptar.Location = new System.Drawing.Point(10, 5);
            this.btnAceptar.Size = new System.Drawing.Size(85, 35);
            this.btnAceptar.Text = "Aceptar";

            // btnCancelar
            this.btnCancelar.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancelar.Location = new System.Drawing.Point(105, 5);
            this.btnCancelar.Size = new System.Drawing.Size(85, 35);
            this.btnCancelar.Text = "Cancelar";

            // Panel de botones
            var panelBotones = new System.Windows.Forms.Panel();
            panelBotones.Dock = System.Windows.Forms.DockStyle.Bottom;
            panelBotones.Height = 50;
            panelBotones.Controls.Add(this.btnAceptar);
            panelBotones.Controls.Add(this.btnCancelar);

            // DialogoSeleccion
            this.AcceptButton = this.btnAceptar;
            this.CancelButton = this.btnCancelar;
            this.ClientSize = new System.Drawing.Size(350, 450);
            this.Controls.Add(this.lstOpciones);
            this.Controls.Add(panelBotones);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Seleccionar";
            this.ResumeLayout(false);
        }
    }
}