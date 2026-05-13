namespace Genesys.UI.Components
{
    partial class MensajesErrores
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Código generado

        private void InitializeComponent()
        {
            this.lblErroresIndicador = new Genesys.UI.Components.GradientLabel();
            this.lblErrores = new Genesys.UI.Components.GradientLabel();
            this.SuspendLayout();
            // 
            // lblErroresIndicador
            // 
            this.lblErroresIndicador.ColorFin = System.Drawing.Color.White;
            this.lblErroresIndicador.ColorInicio = System.Drawing.Color.White;
            this.lblErroresIndicador.Dock = System.Windows.Forms.DockStyle.Left;
            this.lblErroresIndicador.Font = new System.Drawing.Font("Segoe UI Emoji", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblErroresIndicador.GradientMode = System.Drawing.Drawing2D.LinearGradientMode.Horizontal;
            this.lblErroresIndicador.Location = new System.Drawing.Point(0, 0);
            this.lblErroresIndicador.Margin = new System.Windows.Forms.Padding(0);
            this.lblErroresIndicador.Name = "lblErroresIndicador";
            this.lblErroresIndicador.Size = new System.Drawing.Size(24, 31);
            this.lblErroresIndicador.TabIndex = 1;
            this.lblErroresIndicador.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblErrores
            // 
            this.lblErrores.ColorFin = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.lblErrores.ColorInicio = System.Drawing.Color.White;
            this.lblErrores.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblErrores.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblErrores.GradientMode = System.Drawing.Drawing2D.LinearGradientMode.Horizontal;
            this.lblErrores.Location = new System.Drawing.Point(24, 0);
            this.lblErrores.Margin = new System.Windows.Forms.Padding(0);
            this.lblErrores.Name = "lblErrores";
            this.lblErrores.Size = new System.Drawing.Size(445, 31);
            this.lblErrores.TabIndex = 2;
            this.lblErrores.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // MensajesErrores
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.Controls.Add(this.lblErrores);
            this.Controls.Add(this.lblErroresIndicador);
            this.Name = "MensajesErrores";
            this.Size = new System.Drawing.Size(469, 31);
            this.ResumeLayout(false);

        }

        #endregion

        private GradientLabel lblErroresIndicador;
        private GradientLabel lblErrores;
    }
}