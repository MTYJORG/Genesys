using System;
using System.Drawing;
using System.Windows.Forms;

namespace Genesys.UI.Components.Controls.GridViews
{
    public static class GenesysGridViewPrompt
    {
        public static string Ask(string title, string label, string defaultValue)
        {
            using (var form = new Form())
            using (var textBox = new TextBox())
            using (var lbl = new Label())
            using (var ok = new Button())
            using (var cancel = new Button())
            {
                form.Text = title;
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.ClientSize = new Size(360, 120);
                form.Font = new Font("Segoe UI", 9F);

                lbl.Text = label;
                lbl.Left = 12;
                lbl.Top = 12;
                lbl.Width = 330;

                textBox.Left = 12;
                textBox.Top = 38;
                textBox.Width = 330;
                textBox.Text = defaultValue ?? string.Empty;

                ok.Text = "Aceptar";
                ok.Left = 186;
                ok.Top = 78;
                ok.Width = 75;
                ok.DialogResult = DialogResult.OK;

                cancel.Text = "Cancelar";
                cancel.Left = 267;
                cancel.Top = 78;
                cancel.Width = 75;
                cancel.DialogResult = DialogResult.Cancel;

                form.Controls.Add(lbl);
                form.Controls.Add(textBox);
                form.Controls.Add(ok);
                form.Controls.Add(cancel);
                form.AcceptButton = ok;
                form.CancelButton = cancel;

                return form.ShowDialog() == DialogResult.OK
                    ? textBox.Text.Trim()
                    : null;
            }
        }
    }
}
