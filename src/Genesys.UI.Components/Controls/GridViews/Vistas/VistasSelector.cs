using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Genesys.UI.Components.Controls.GridViews;
using Syncfusion.Windows.Forms.Tools;

namespace Genesys.UI.Components.Controls.GridViews.Vistas
{
    public class VistasSelector : UserControl
    {
        private readonly Label captionLabel;
        private readonly ToolStripEx viewsToolStrip;
        private readonly ToolTip toolTip;

        private VistasAdministrador manager;
        private bool refreshing;
        private ContextMenuStrip currentMenu;

        public VistasSelector()
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

            viewsToolStrip = new ToolStripEx
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                ForeColor = Color.DimGray,
                ImageScalingSize = new Size(16, 16),
                LauncherStyle = LauncherStyle.Office2007,
                LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow,
                ShowCaption = false,
                ShowItemToolTips = true,
                VisualStyle = ToolStripExStyle.Metro,
                Office12Mode = false,
                OfficeColorScheme = ToolStripEx.ColorScheme.Managed,
                ThemeName = "Metro",
                Padding = new Padding(4, 2, 2, 0),
                Margin = new Padding(0),
                TabStop = false,
                GripStyle = ToolStripGripStyle.Hidden
            };

            viewsToolStrip.ThemeStyle.HoverItemBackColor = Color.FromArgb(238, 238, 238);
            viewsToolStrip.ThemeStyle.DropDownStyle.HoverItemBackColor = Color.FromArgb(230, 230, 230);

            toolTip = new ToolTip
            {
                ShowAlways = false,
                InitialDelay = 600,
                ReshowDelay = 150
            };

            Controls.Add(viewsToolStrip);
            Controls.Add(captionLabel);
        }

        public void Attach(VistasAdministrador manager)
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
                viewsToolStrip.SuspendLayout();
                DetachViewButtonEvents();
                viewsToolStrip.Items.Clear();

                if (manager == null)
                    return;

                IList<string> views = manager.GetAvailableViewNames();

                if (views == null || views.Count == 0)
                    return;

                string currentViewName = manager.CurrentViewName;

                foreach (string viewName in views)
                {
                    if (string.IsNullOrWhiteSpace(viewName))
                        continue;

                    bool selected = string.Equals(
                        viewName,
                        currentViewName,
                        StringComparison.OrdinalIgnoreCase);

                    viewsToolStrip.Items.Add(CreateViewButton(viewName, selected));
                }
            }
            finally
            {
                viewsToolStrip.ResumeLayout(true);
                refreshing = false;
            }
        }

        private ToolStripButton CreateViewButton(string viewName, bool selected)
        {
            string text = viewName;

            if (selected && manager != null && manager.HasChanges)
                text += " *";

            var button = new ToolStripButton
            {
                Text = text,
                Tag = viewName,
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                AutoSize = true,
                Margin = new Padding(0, 1, 0, 1),
                Padding = new Padding(8, 2, 8, 2),
                Font = new Font(
                    "Segoe UI",
                    9F,
                    selected ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = selected ? Color.MidnightBlue : Color.DimGray,
                BackColor = selected ? Color.FromArgb(232, 240, 254) : Color.White,
                Checked = selected,
                CheckOnClick = false,
                ToolTipText = selected
                    ? "Vista activa: " + viewName + (manager != null && manager.HasChanges ? " - cambios sin guardar" : string.Empty) + "\nClick derecho para opciones"
                    : "Aplicar vista: " + viewName + "\nClick derecho para opciones",
                AccessibleName = "Vista " + viewName,
                AccessibleDescription = selected
                    ? "Vista activa" + (manager != null && manager.HasChanges ? " con cambios sin guardar" : string.Empty)
                    : "Aplicar vista " + viewName
            };

            button.Click += ViewButton_Click;
            button.MouseUp += ViewButton_MouseUp;
            button.Paint += ViewButton_Paint;

            return button;
        }

        private void ViewButton_Paint(object sender, PaintEventArgs e)
        {
            ToolStripButton button = sender as ToolStripButton;

            if (button == null)
                return;

            bool selected = button.Checked;
            Color borderColor = selected
                ? Color.FromArgb(118, 145, 190)
                : Color.FromArgb(210, 210, 210);

            Rectangle bounds = new Rectangle(0, 0, button.Bounds.Width - 1, button.Bounds.Height - 1);

            using (Pen pen = new Pen(borderColor, 1))
                e.Graphics.DrawRectangle(pen, bounds);
        }

        private void ViewButton_Click(object sender, EventArgs e)
        {
            if (manager == null)
                return;

            ToolStripButton button = sender as ToolStripButton;

            if (button == null)
                return;

            string viewName = button.Tag as string;

            if (string.IsNullOrWhiteSpace(viewName))
                return;

            if (string.Equals(viewName, manager.CurrentViewName, StringComparison.OrdinalIgnoreCase))
                return;

            manager.ApplyViewByName(viewName);
            RefreshViews();
        }

        private void ViewButton_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || manager == null)
                return;

            ToolStripButton button = sender as ToolStripButton;

            if (button == null)
                return;

            string viewName = button.Tag as string;

            if (string.IsNullOrWhiteSpace(viewName))
                return;

            Point location = new Point(button.Bounds.Left + e.X, button.Bounds.Top + e.Y);
            ShowViewContextMenu(viewsToolStrip, viewName, location);
        }

        private void ShowViewContextMenu(Control ownerControl, string viewName, Point location)
        {
            if (currentMenu != null && !currentMenu.IsDisposed)
            {
                currentMenu.Close();
                currentMenu.Items.Clear();
            }
            else
            {
                currentMenu = new ContextMenuStrip();
                currentMenu.ShowImageMargin = false;
            }

            bool isCurrent = string.Equals(viewName, manager.CurrentViewName, StringComparison.OrdinalIgnoreCase);
            bool isDefault = manager.IsDefaultViewName(viewName);
            bool hasChanges = isCurrent && manager.HasChanges;

            AddHeaderItem(viewName, isCurrent, hasChanges);

            if (!isCurrent)
            {
                ToolStripMenuItem apply = new ToolStripMenuItem("Aplicar vista");
                apply.Click += delegate
                {
                    manager.ApplyViewByName(viewName);
                    RefreshViews();
                };
                currentMenu.Items.Add(apply);
                currentMenu.Items.Add(new ToolStripSeparator());
            }

            if (isCurrent)
            {
                ToolStripMenuItem save = new ToolStripMenuItem(
                    isDefault ? "Guardar estado actual como nueva vista..." : "Guardar cambios");
                save.ToolTipText = isDefault
                    ? "La vista Predeterminada no se sobrescribe; se guarda como una vista nueva."
                    : "Guarda los cambios runtime de la vista activa.";
                save.Click += delegate
                {
                    if (isDefault)
                        manager.SaveAsNewViewFromSelector();
                    else
                        manager.SaveCurrentViewFromSelector();

                    RefreshViews();
                };
                currentMenu.Items.Add(save);
            }

            ToolStripMenuItem saveAs = new ToolStripMenuItem("Guardar estado actual como nueva vista...");
            saveAs.ToolTipText = "Crea una vista nueva tomando el estado actual del grid.";
            saveAs.Click += delegate
            {
                if (!isCurrent)
                    manager.ApplyViewByName(viewName);

                manager.SaveAsNewViewFromSelector();
                RefreshViews();
            };
            currentMenu.Items.Add(saveAs);

            if (!isDefault)
            {
                ToolStripMenuItem duplicate = new ToolStripMenuItem("Duplicar vista guardada...");
                duplicate.ToolTipText = "Copia la vista seleccionada tal como está guardada.";
                duplicate.Click += delegate
                {
                    manager.DuplicateViewFromSelector(viewName);
                    RefreshViews();
                };
                currentMenu.Items.Add(duplicate);

                ToolStripMenuItem rename = new ToolStripMenuItem("Cambiar nombre...");
                rename.ToolTipText = "Renombra la vista seleccionada conservando su configuración.";
                rename.Click += delegate
                {
                    manager.RenameViewFromSelector(viewName);
                    RefreshViews();
                };
                currentMenu.Items.Add(rename);

                ToolStripMenuItem delete = new ToolStripMenuItem("Eliminar vista...");
                delete.Click += delegate
                {
                    manager.DeleteViewFromSelector(viewName);
                    RefreshViews();
                };
                currentMenu.Items.Add(delete);
            }

            bool canMoveLeft = !isDefault && manager.CanMoveViewLeft(viewName);
            bool canMoveRight = !isDefault && manager.CanMoveViewRight(viewName);

            if (canMoveLeft || canMoveRight)
            {
                currentMenu.Items.Add(new ToolStripSeparator());

                if (canMoveLeft)
                {
                    ToolStripMenuItem moveLeft = new ToolStripMenuItem("Mover a la izquierda");
                    moveLeft.Click += delegate
                    {
                        manager.MoveViewLeftFromSelector(viewName);
                        RefreshViews();
                    };
                    currentMenu.Items.Add(moveLeft);
                }

                if (canMoveRight)
                {
                    ToolStripMenuItem moveRight = new ToolStripMenuItem("Mover a la derecha");
                    moveRight.Click += delegate
                    {
                        manager.MoveViewRightFromSelector(viewName);
                        RefreshViews();
                    };
                    currentMenu.Items.Add(moveRight);
                }
            }

            currentMenu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem designer = new ToolStripMenuItem("Diseñar vista...");
            designer.Click += delegate
            {
                manager.ShowDesignerFromSelector(viewName);
                RefreshViews();
            };
            currentMenu.Items.Add(designer);

            currentMenu.Show(ownerControl, location);
        }

        private void AddHeaderItem(string viewName, bool isCurrent, bool hasChanges)
        {
            string titleText = "Vista: " + viewName + (hasChanges ? " *" : string.Empty);

            ToolStripLabel title = new ToolStripLabel(titleText)
            {
                AutoSize = false,
                Width = 230,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(Font, FontStyle.Bold),
                ForeColor = Color.MidnightBlue,
                BackColor = Color.FromArgb(245, 247, 250),
                Enabled = false,
                AccessibleName = titleText,
                AccessibleDescription = isCurrent
                    ? hasChanges ? "Vista activa con cambios sin guardar" : "Vista activa"
                    : "Vista no activa"
            };

            currentMenu.Items.Add(title);
            currentMenu.Items.Add(new ToolStripSeparator());
        }

        private void DetachViewButtonEvents()
        {
            foreach (ToolStripItem item in viewsToolStrip.Items)
            {
                item.Click -= ViewButton_Click;
                item.MouseUp -= ViewButton_MouseUp;
                item.Paint -= ViewButton_Paint;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                toolTip.Dispose();

                DetachViewButtonEvents();

                if (currentMenu != null)
                {
                    currentMenu.Dispose();
                    currentMenu = null;
                }
            }

            base.Dispose(disposing);
        }
    }
}
