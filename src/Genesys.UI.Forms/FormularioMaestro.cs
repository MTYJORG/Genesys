using ComponentesComunes.Dialogos;
using Newtonsoft.Json;
using Syncfusion.WinForms.DataGrid;
using Syncfusion.WinForms.DataGridConverter;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ComponentesComunes
{
    public partial class FormularioMaestro : Form
    {
        #region ========== PROPIEDADES VIRTUALES (SOBRESCRIBIR EN CLASE HIJA) ==========

        // SQL
        // La librería referencia System.Configuration y lee el App.config de la aplicación que la consume, no el de la librería.
        // Agregar la siguiente linea en "program.cs" de la aplicacion antes de abrir cualquier formulario.
        // AppConfiguracion.CadenaConexion = ConfigurationManager.ConnectionStrings["conServerAdm"].ConnectionString;
        protected string CadenaConexion => AppConfiguracion.CadenaConexion;
        protected virtual string StoredProcedurePrincipal => "";        // ejemplo: "uspCABCLotes";
        protected virtual string ColumnaClaveEdicion => "ID";           // ejemplo: "CIDLOTE"
        protected virtual List<ParametroDatos> ObtenerParametros() => new List<ParametroDatos>();

        protected virtual string LookupParametroValor => "";  // "Lotes", "Productos", "Clientes", etc.

        protected virtual List<ConfiguracionColumna> ObtenerColumnas() => new List<ConfiguracionColumna>();

        // Vistas
        protected virtual string CarpetaAppData => "MiApp";
        protected virtual string PrefijoArchivoVista => "FormularioMaestro";
        #endregion

        #region ========== CAMPOS PRIVADOS ==========

        private DataTable _datosActuales;
        private bool _suprimirCarga = true;
        private string _nombreVistaActual;
        private Dictionary<string, double> _anchosOriginales;
        
        // protected para que el hijo lo acceda
        protected System.Data.DataRow _rowCodigoSeleccionado;  

        #endregion

        #region ========== CONSTRUCTOR ==========

        public FormularioMaestro()
        {
            InitializeComponent();
            ConfigurarEventos();
            ConfigurarLookup();

            _suprimirCarga = false;
        }

        // Método virtual que el hijo puede sobrescribir
        protected virtual void OnLookupCompleted(System.Data.DataRow rowSeleccionada)
        {
            // El formulario hijo decide qué hacer con el registro seleccionado
        }

        #endregion

        #region ========== CONFIGURACIÓNES ==========

        private void ConfigurarLookup()
        {
            // Configurar Lookup SOLO si el formulario hijo lo habilita
            //if (HabilitarLookup && !string.IsNullOrEmpty(LookupParametroValor))
            //{

            aTxtCampoFiltro.EsLookup = true;
            aTxtCampoFiltro.LookupControl = txtDescripcion;
            aTxtCampoFiltro.LookupProvider = new StoredProcedureLookupProvider { ParametroValor = LookupParametroValor };

            aTxtCampoFiltro.LookupCompleted += (s, e) =>
            {
                if (e.Success)
                {
                    if (e.Data != null)
                    {
                        _rowCodigoSeleccionado = e.Data;
                        // Notificar al formulario hijo que se seleccionó un valor
                        OnLookupCompleted(e.Data);
                    }
                }
                else
                {
                    MostrarError(e.ErrorMessage == "Valor no válido" ? "Valor no válido" : e.ErrorMessage);
                    aTxtCampoFiltro.Clear();
                    txtDescripcion.Clear();
                }
            };

            //}
            //else
            //{
            //    Ocultar los controles si no se usan
            //    aTxtCodigo.Visible = false;
            //    txtDescripcion.Visible = false;
            //}
        }

        private void ConfigurarEventos()
        {
            // Botones
            btnNuevo.Click += (s, e) => AccionNuevo();
            btnEditar.Click += (s, e) => AccionEditar();
            btnGuardarVista.Click += (s, e) => GuardarVista();
            btnCargarVista.Click += (s, e) => CargarVista();
            btnExcel.Click += (s, e) => ExportarExcel();
            btnPdf.Click += (s, e) => ExportarPdf();
            btnSalir.Click += (s, e) => this.Close();

            // Filtros
            datFechaInicio.ValueChanged += (s, e) => CargarDatos();
            datFechaFinal.ValueChanged += (s, e) => CargarDatos();
            cbxFiltroStatus.SelectedIndexChanged += (s, e) => CargarDatos();
            aTxtCampoFiltro.TextChanged += (s, e) => CargarDatos();
            //btnLimpiar.Click += (s, e) => LimpiarFiltros();

            // Grid y navegación
            syncGrid.SelectionChanged += (s, e) =>
            {
                if (syncGrid.SelectedIndex >= 0 && bindingSource.Position != syncGrid.SelectedIndex)
                    bindingSource.Position = syncGrid.SelectedIndex;
            };

            bindingSource.CurrentChanged += (s, e) =>
            {
                if (bindingSource.Position >= 0 && syncGrid.View?.Records?.Count > bindingSource.Position)
                    syncGrid.SelectedIndex = bindingSource.Position;
            };

            this.FormClosing += (s, e) => GuardarVistaActual();
        }

        #endregion

        #region ========== CARGA DE DATOS ==========

        protected virtual void CargarDatos()
        {
            if (_suprimirCarga) return;
            if (string.IsNullOrEmpty(StoredProcedurePrincipal) || string.IsNullOrEmpty(CadenaConexion)) return;

            try
            {
                Cursor = Cursors.WaitCursor;

                using (var conn = new SqlConnection(CadenaConexion))
                using (var cmd = new SqlCommand(StoredProcedurePrincipal, conn) { CommandType = CommandType.StoredProcedure, CommandTimeout = 120 })
                using (var da = new SqlDataAdapter(cmd))
                {
                    foreach (var param in ObtenerParametros())
                        cmd.Parameters.AddWithValue(param.Nombre, param.Valor ?? DBNull.Value);

                    var dt = new DataTable();
                    conn.Open();
                    da.Fill(dt);
                    _datosActuales = dt;

                    if (_datosActuales.Rows.Count == 0)
                    {
                        syncGrid.DataSource = null;
                        bindingSource.DataSource = null;
                        lblInfoRegistros.Text = "Sin datos";
                        return;
                    }

                    syncGrid.DataSource = _datosActuales;
                    bindingSource.DataSource = _datosActuales;
                    AplicarConfiguracionColumnas();

                    lblInfoRegistros.Text = $"Total: {_datosActuales.Rows.Count} registros";
                    lblInfoRegistros.ForeColor = Color.FromArgb(33, 115, 70);

                    if (!string.IsNullOrEmpty(_nombreVistaActual))
                        AplicarVista(_nombreVistaActual);
                }
            }
            catch (Exception ex)
            {
                MostrarError($"Error al cargar datos: {ex.Message}");
                lblInfoRegistros.Text = "Error de carga";
                lblInfoRegistros.ForeColor = Color.FromArgb(183, 28, 28);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void AplicarConfiguracionColumnas()
        {
            var columnas = ObtenerColumnas();
            if (columnas == null || columnas.Count == 0) return;

            foreach (var col in columnas)
            {
                var gridCol = syncGrid.Columns.FirstOrDefault(c => c.MappingName == col.NombreCampo);
                if (gridCol == null) continue;

                gridCol.HeaderText = col.TextoEncabezado ?? col.NombreCampo;
                gridCol.Visible = col.Visible;
                if (col.Ancho.HasValue && col.Ancho.Value > 0)
                    gridCol.Width = col.Ancho.Value;
            }
        }

        #endregion

        #region ========== VISTAS ==========

        private string RutaVistas => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), CarpetaAppData, "Vistas");

        private void GuardarVista()
        {
            string nombre = DialogoEntrada.Mostrar("Nombre de la vista:", "Guardar Vista");
            if (string.IsNullOrEmpty(nombre)) return;

            var vista = new ConfiguracionVista
            {
                Nombre = nombre,
                FechaCreacion = DateTime.Now,
                AnchosColumnas = syncGrid.Columns.ToDictionary(c => c.MappingName, c => c.Width),
                OrdenColumnas = syncGrid.Columns.Select(c => c.MappingName).ToList(),
                ColumnasOcultas = syncGrid.Columns.Where(c => !c.Visible).Select(c => c.MappingName).ToList()
            };

            Directory.CreateDirectory(RutaVistas);
            string archivo = Path.Combine(RutaVistas, $"{PrefijoArchivoVista}_{nombre}.json");
            File.WriteAllText(archivo, JsonConvert.SerializeObject(vista, Formatting.Indented));

            _nombreVistaActual = nombre;
            MostrarExito($"Vista '{nombre}' guardada");
        }

        private void CargarVista()
        {
            if (!Directory.Exists(RutaVistas))
            {
                MostrarAdvertencia("No hay vistas guardadas");
                return;
            }

            var archivos = Directory.GetFiles(RutaVistas, $"{PrefijoArchivoVista}_*.json");
            if (archivos.Length == 0)
            {
                MostrarAdvertencia("No hay vistas guardadas");
                return;
            }

            var nombres = archivos.Select(f => Path.GetFileNameWithoutExtension(f).Replace($"{PrefijoArchivoVista}_", "")).ToList();

            string seleccion = DialogoSeleccion.Mostrar(nombres, "Seleccionar Vista");
            if (seleccion != null)
            {
                AplicarVista(seleccion);
                _nombreVistaActual = seleccion;
                MostrarExito($"Vista '{seleccion}' cargada");
            }
        }

        private void AplicarVista(string nombreVista)
        {
            string archivo = Path.Combine(RutaVistas, $"{PrefijoArchivoVista}_{nombreVista}.json");
            if (!File.Exists(archivo)) return;

            try
            {
                var vista = JsonConvert.DeserializeObject<ConfiguracionVista>(File.ReadAllText(archivo));
                if (vista == null) return;

                foreach (var col in syncGrid.Columns)
                    col.Visible = !vista.ColumnasOcultas.Contains(col.MappingName);

                foreach (var kvp in vista.AnchosColumnas)
                {
                    var col = syncGrid.Columns.FirstOrDefault(c => c.MappingName == kvp.Key);
                    if (col != null && col.Visible)
                        col.Width = kvp.Value;
                }
            }
            catch (Exception ex)
            {
                MostrarError($"Error al cargar vista: {ex.Message}");
            }
        }

        private void GuardarVistaActual()
        {
            try
            {
                Directory.CreateDirectory(RutaVistas);
                var config = new { UltimaVista = _nombreVistaActual ?? "" };
                string archivo = Path.Combine(RutaVistas, $"{PrefijoArchivoVista}_ultima.json");
                File.WriteAllText(archivo, JsonConvert.SerializeObject(config));
            }
            catch { }
        }

        #endregion

        #region ========== EXPORTACIÓN ==========

        private void ExportarExcel()
        {
            if (_datosActuales == null || _datosActuales.Rows.Count == 0)
            {
                MostrarAdvertencia("No hay datos para exportar");
                return;
            }

            var dialog = new SaveFileDialog { Filter = "Excel Files|*.xlsx", FileName = $"{this.Text}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx" };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                var options = new Syncfusion.WinForms.DataGridConverter.ExcelExportingOptions();
                var engine = syncGrid.ExportToExcel(syncGrid.View, options);
                engine.Excel.Workbooks[0].SaveAs(dialog.FileName);
                MostrarExito("Exportado a Excel");

                if (MessageBox.Show("¿Abrir archivo?", "Exportación", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    System.Diagnostics.Process.Start(dialog.FileName);
            }
            catch (Exception ex)
            {
                MostrarError($"Error al exportar: {ex.Message}");
            }
        }

        private void ExportarPdf()
        {
            if (_datosActuales == null || _datosActuales.Rows.Count == 0)
            {
                MostrarAdvertencia("No hay datos para exportar");
                return;
            }

            var dialog = new SaveFileDialog { Filter = "PDF Files|*.pdf", FileName = $"{this.Text}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf" };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                var options = new Syncfusion.WinForms.DataGridConverter.PdfExportingOptions();
                var doc = syncGrid.ExportToPdf(syncGrid.View, options);
                doc.Save(dialog.FileName);
                doc.Close(true);
                MostrarExito("Exportado a PDF");

                if (MessageBox.Show("¿Abrir archivo?", "Exportación", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    System.Diagnostics.Process.Start(dialog.FileName);
            }
            catch (Exception ex)
            {
                MostrarError($"Error al exportar: {ex.Message}");
            }
        }

        #endregion

        #region ========== ACCIONES CRUD ==========

        protected virtual void AccionNuevo()
        {
            MostrarInfo("Función Nuevo no implementada");
        }

        protected virtual void AccionEditar()
        {
            var id = ObtenerValorSeleccionado(ColumnaClaveEdicion);
            if (id == null || id == DBNull.Value)
                MostrarAdvertencia("Debe seleccionar un registro");
            else
                MostrarInfo($"Editar registro ID: {id}");
        }

        protected object ObtenerValorSeleccionado(string columna)
        {
            var fila = syncGrid.SelectedItem as DataRowView;
            return fila?[columna];
        }

        #endregion

        #region ========== MENSAJES ==========

        private void MostrarMensaje(string mensaje, Color fondo, Color texto)
        {
            panelMensajes.BackColor = fondo;
            panelMensajes.Controls.Clear();
            var lbl = new Label { Text = mensaje, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = texto, BackColor = fondo };
            panelMensajes.Controls.Add(lbl);
            panelMensajes.Visible = true;

            if (fondo == Color.FromArgb(212, 237, 218) || fondo == Color.FromArgb(207, 226, 255))
            {
                var timer = new Timer { Interval = 5000 };
                timer.Tick += (s, e) => { panelMensajes.Visible = false; timer.Stop(); };
                timer.Start();
            }
        }

        protected void MostrarError(string msg) => MostrarMensaje(msg, Color.FromArgb(248, 215, 218), Color.FromArgb(114, 28, 36));
        protected void MostrarExito(string msg) => MostrarMensaje(msg, Color.FromArgb(212, 237, 218), Color.FromArgb(21, 87, 36));
        protected void MostrarAdvertencia(string msg) => MostrarMensaje(msg, Color.FromArgb(255, 243, 205), Color.FromArgb(133, 100, 4));
        protected void MostrarInfo(string msg) => MostrarMensaje(msg, Color.FromArgb(207, 226, 255), Color.FromArgb(13, 60, 97));

        #endregion
    }
}