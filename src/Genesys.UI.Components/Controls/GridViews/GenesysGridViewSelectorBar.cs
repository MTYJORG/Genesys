using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Genesys.UI.Components.Controls.GridViews
{
    public class GenesysGridViewSelectorBar : UserControl
    {
        private readonly Label captionLabel;
        private readonly FlowLayoutPanel viewsPanel;
        private readonly ToolTip toolTip;

        private GenesysGridViewManager manager;
        private bool refreshing;

        public GenesysGridViewSelectorBar()
        {
            Dock = DockStyle.Fill;
            Height = 32;
            BackColor = Color.White;
            TabStop = false;

            captionLabel = new Label
            {
                Dock = DockStyle.Left,
                Width = 48,
                Text = "Vista:",
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 6, 0),
                ForeColor = Color.DimGray,
                Font = new Font("Segoe UI", 8.75F, FontStyle.Regular),
                TabStop = false
            };

            viewsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(4, 4, 2, 0),
                Margin = new Padding(0),
                TabStop = false
            };

            toolTip = new ToolTip
            {
                ShowAlways = false,
                InitialDelay = 600,
                ReshowDelay = 150
            };

            Controls.Add(viewsPanel);
            Controls.Add(captionLabel);
        }

        public void Attach(GenesysGridViewManager manager)
        {
            this.manager = manager;
            RefreshViews();
        }

        public void RefreshViews()
        {
            if (refreshing)
                return;

            refreshing = true;

            try
            {
                viewsPanel.SuspendLayout();
                viewsPanel.Controls.Clear();

                if (manager == null)
                    return;

                IList<string> views =
                    manager.GetAvailableViewNames();

                if (views == null || views.Count == 0)
                    return;

                string currentViewName =
                    manager.CurrentViewName;

                foreach (string viewName in views)
                {
                    if (string.IsNullOrWhiteSpace(viewName))
                        continue;

                    bool selected =
                        string.Equals(
                            viewName,
                            currentViewName,
                            StringComparison.OrdinalIgnoreCase);

                    viewsPanel.Controls.Add(
                        CreateViewRadioButton(
                            viewName,
                            selected));
                }
            }
            finally
            {
                viewsPanel.ResumeLayout(true);
                refreshing = false;
            }
        }

        private Control CreateViewRadioButton(
            string viewName,
            bool selected)
        {
            string text = viewName;

            if (selected &&
                manager != null &&
                manager.HasChanges)
            {
                text += " *";
            }

            var radio = new RadioButton
            {
                Text = text,
                Tag = viewName,
                AutoSize = true,
                Checked = selected,
                Cursor = Cursors.Hand,
                Margin = new Padding(2, 3, 10, 0),
                Padding = new Padding(0),
                FlatStyle = FlatStyle.System,
                Font = new Font(
                    "Segoe UI",
                    9F,
                    selected
                        ? FontStyle.Bold
                        : FontStyle.Regular),
                ForeColor = selected
                    ? Color.MidnightBlue
                    : Color.DimGray,
                TabStop = false
            };

            toolTip.SetToolTip(
                radio,
                selected
                    ? "Vista activa: " + viewName
                    : "Aplicar vista: " + viewName);

            radio.CheckedChanged +=
                ViewRadio_CheckedChanged;

            return radio;
        }

        private void ViewRadio_CheckedChanged(
            object sender,
            EventArgs e)
        {
            if (manager == null)
                return;

            RadioButton radio =
                sender as RadioButton;

            if (radio == null)
                return;

            if (!radio.Checked)
                return;

            string viewName =
                radio.Tag as string;

            if (string.IsNullOrWhiteSpace(viewName))
                return;

            if (string.Equals(
                viewName,
                manager.CurrentViewName,
                StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            manager.ApplyViewByName(viewName);
        }

        protected override void Dispose(
            bool disposing)
        {
            if (disposing)
                toolTip.Dispose();

            base.Dispose(disposing);
        }
    }
}