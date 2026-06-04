using Genesys.UI.Components.Controls.Messages;
using Genesys.UI.Components.Controls.Toolbar;
using Genesys.UI.Components.Visual;
using Syncfusion.WinForms.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Action = System.Action;

namespace Genesys.UI.Components.Forms
{
    /// <summary>
    /// Formulario base simple para pantallas que no requieren grid, filtros ni vistas.
    /// Proporciona únicamente barra de botones, panel de mensajes y un panel central
    /// donde los formularios derivados pueden colocar su contenido.
    /// </summary>
    public class GenesysSimpleForm : SfForm
    {
        private const int ButtonsPanelHeight = 46;
        private const int MessagesPanelHeight = 49;

        private bool disposed;

        public Panel ButtonsPanel { get; private set; }
        public Panel MessagesPanel { get; private set; }
        public Panel ContentPanel { get; private set; }
        public Panel ToolbarHostPanel { get; private set; }

        public GenesysToolbar Toolbar { get; private set; }
        public GenesysMessages Messages { get; private set; }

        public event EventHandler CerrarRequested;

        public GenesysSimpleForm()
        {
            Initialize();
        }

        /// <summary>
        /// Construye la estructura visual base del formulario simple.
        /// </summary>
        protected virtual void Initialize()
        {
            KeyPreview = true;

            GenesysFormVisual.Apply(this);

            SuspendLayout();

            BuildPanels();
            BuildToolbar();
            ConfigureDefaultToolbar();
            BuildMessages();
            AddMainPanelsToForm();

            ResumeLayout(true);
            PerformLayout();
        }

        private void BuildPanels()
        {
            ButtonsPanel = GenesysPanelFactory.Create(
                "ButtonsPanel",
                DockStyle.Top,
                height: ButtonsPanelHeight,
                backColor: Color.White);

            MessagesPanel = GenesysPanelFactory.Create(
                "MessagesPanel",
                DockStyle.Top,
                height: MessagesPanelHeight,
                backColor: Color.White);

            ContentPanel = GenesysPanelFactory.Create(
                "ContentPanel",
                DockStyle.Fill,
                backColor: Color.White);

            ButtonsPanel.TabStop = false;
            MessagesPanel.TabStop = false;
            ContentPanel.TabStop = false;

            GenesysControlVisual.EnableDoubleBuffer(ButtonsPanel);
            GenesysControlVisual.EnableDoubleBuffer(MessagesPanel);
            GenesysControlVisual.EnableDoubleBuffer(ContentPanel);
        }

        private void BuildToolbar()
        {
            ToolbarHostPanel = new Panel
            {
                Name = "ToolbarHostPanel",
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                TabStop = false
            };

            Toolbar = new GenesysToolbar
            {
                Dock = DockStyle.Fill,
                TabStop = false
            };

            ToolbarHostPanel.Controls.Add(Toolbar);
            ButtonsPanel.Controls.Add(ToolbarHostPanel);
        }

        private void BuildMessages()
        {
            Messages = new GenesysMessages
            {
                Dock = DockStyle.Fill,
                TabStop = false
            };

            MessagesPanel.Controls.Add(Messages);
        }

        private void AddMainPanelsToForm()
        {
            Controls.Add(ContentPanel);
            Controls.Add(MessagesPanel);
            Controls.Add(ButtonsPanel);
        }

        /// <summary>
        /// Configura los botones base del formulario. Por default solo agrega Cerrar.
        /// Las clases derivadas pueden sobreescribirlo para cambiar el set inicial.
        /// </summary>
        protected virtual void ConfigureDefaultToolbar()
        {
            Toolbar.Add(
                BotonTipo.Cerrar,
                "Cerrar",
                "Cerrar formulario",
                new Padding(55, 0, 0, 0),
                CerrarFormulario);
        }

        /// <summary>
        /// Agrega un botón antes del botón Cerrar. Si Cerrar no existe, se agrega al final.
        /// </summary>
        protected void AddToolbarButton(
            BotonTipo tipo,
            string texto,
            string tooltip,
            Action onClick)
        {
            Toolbar.AddBefore(
                BotonTipo.Cerrar.ToString(),
                tipo,
                texto,
                tooltip,
                onClick);
        }

        /// <summary>
        /// Agrega un botón antes del botón Cerrar usando padding personalizado.
        /// </summary>
        protected void AddToolbarButton(
            BotonTipo tipo,
            string texto,
            string tooltip,
            Padding textPadding,
            Action onClick)
        {
            Toolbar.AddBefore(
                BotonTipo.Cerrar.ToString(),
                tipo,
                texto,
                tooltip,
                textPadding,
                onClick);
        }

        protected virtual void CerrarFormulario()
        {
            CerrarRequested?.Invoke(this, EventArgs.Empty);
            Close();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                Close();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
                disposed = true;

            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Opciones mínimas para configurar GenesysSimpleForm desde código externo.
    /// Se mantiene separado del registry de GenesysGridForm para no mezclar conceptos de grid.
    /// </summary>
    public class GenesysSimpleFormOptions
    {
        public string Title { get; set; }
        public GenesysSimpleToolbarButtonOptions[] ToolbarButtons { get; set; }
    }

    public class GenesysSimpleToolbarButtonOptions
    {
        public BotonTipo Tipo { get; set; }
        public string Texto { get; set; }
        public string Tooltip { get; set; }
        public System.Action<GenesysSimpleForm> OnClick { get; set; }
    }

    public static class GenesysSimpleRegistry
    {
        private static readonly Dictionary<Type, GenesysSimpleFormOptions> optionsByForm =
            new Dictionary<Type, GenesysSimpleFormOptions>();

        public static void Register<TForm>(GenesysSimpleFormOptions options)
            where TForm : GenesysSimpleForm
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            optionsByForm[typeof(TForm)] = options;
        }

        public static GenesysSimpleFormOptions Get(Type formType)
        {
            if (formType == null)
                return null;

            GenesysSimpleFormOptions options;

            if (optionsByForm.TryGetValue(formType, out options))
                return options;

            foreach (var item in optionsByForm)
            {
                if (item.Key.IsAssignableFrom(formType))
                    return item.Value;
            }

            return null;
        }
    }
}
