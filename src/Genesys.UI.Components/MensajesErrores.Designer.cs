namespace Juguera
{
    partial class MensajesErrores
    {
        /// <summary> 
        /// Variable del diseñador necesaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Limpiar los recursos que se estén usando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados se deben desechar; false en caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de componentes

        /// <summary> 
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido de este método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblErroresIndicador = new Syncfusion.Windows.Forms.Tools.GradientLabel();
            this.lblErrores = new Syncfusion.Windows.Forms.Tools.GradientLabel();
            this.SuspendLayout();
            // 
            // lblErroresIndicador
            // 
            this.lblErroresIndicador.BackgroundColor = new Syncfusion.Drawing.BrushInfo();
            this.lblErroresIndicador.BeforeTouchSize = new System.Drawing.Size(10, 31);
            this.lblErroresIndicador.BorderAppearance = System.Windows.Forms.BorderStyle.None;
            this.lblErroresIndicador.BorderSides = ((System.Windows.Forms.Border3DSide)(((((System.Windows.Forms.Border3DSide.Left | System.Windows.Forms.Border3DSide.Top) 
            | System.Windows.Forms.Border3DSide.Right) 
            | System.Windows.Forms.Border3DSide.Bottom) 
            | System.Windows.Forms.Border3DSide.Middle)));
            this.lblErroresIndicador.Dock = System.Windows.Forms.DockStyle.Left;
            this.lblErroresIndicador.Font = new System.Drawing.Font("Segoe UI Emoji", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblErroresIndicador.Location = new System.Drawing.Point(0, 0);
            this.lblErroresIndicador.Margin = new System.Windows.Forms.Padding(0);
            this.lblErroresIndicador.Name = "lblErroresIndicador";
            this.lblErroresIndicador.Size = new System.Drawing.Size(10, 31);
            this.lblErroresIndicador.TabIndex = 1;
            this.lblErroresIndicador.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblErrores
            // 
            this.lblErrores.BackgroundColor = new Syncfusion.Drawing.BrushInfo(Syncfusion.Drawing.GradientStyle.Horizontal, new System.Drawing.Color[] {
            System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(255))))),
            System.Drawing.Color.White,
            System.Drawing.Color.White,
            System.Drawing.Color.White,
            System.Drawing.Color.White});
            this.lblErrores.BeforeTouchSize = new System.Drawing.Size(459, 34);
            this.lblErrores.BorderAppearance = System.Windows.Forms.BorderStyle.None;
            this.lblErrores.BorderSides = ((System.Windows.Forms.Border3DSide)(((((System.Windows.Forms.Border3DSide.Left | System.Windows.Forms.Border3DSide.Top) 
            | System.Windows.Forms.Border3DSide.Right) 
            | System.Windows.Forms.Border3DSide.Bottom) 
            | System.Windows.Forms.Border3DSide.Middle)));
            this.lblErrores.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblErrores.Location = new System.Drawing.Point(10, 0);
            this.lblErrores.Margin = new System.Windows.Forms.Padding(0);
            this.lblErrores.Name = "lblErrores";
            this.lblErrores.Size = new System.Drawing.Size(459, 34);
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

        private Syncfusion.Windows.Forms.Tools.GradientLabel lblErroresIndicador;
        private Syncfusion.Windows.Forms.Tools.GradientLabel lblErrores;
    }
}