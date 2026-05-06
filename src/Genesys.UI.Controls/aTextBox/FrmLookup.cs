using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Genesys.UI.Controls
{
    internal partial class FrmLookup : Form
    {
        private DataTable _data;
        private string _col1;
        private string _col2;

        //public string Valor { get; private set; }
        //public string Descripcion { get; private set; }
        public LookupResult Resultado { get; private set; }

        public FrmLookup()
        {
            InitializeComponent();
        }

        // 🔥 MÉTODO DE ENTRADA
        public void SetData(DataTable dt, Control origen)
        {
            _data = dt;

            Posicionar(origen);

            bindingSource1.DataSource = _data;
            bindingSource1.Filter = null;

            dgv.DataSource = bindingSource1;

            DetectarColumnas();

            txtBuscar.Text = "";
            txtBuscar.Focus();
        }

        private void Posicionar(Control origen)
        {
            var p = origen.Parent.PointToScreen(origen.Location);

            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(p.X, p.Y + origen.Height + 5);
        }

        private void DetectarColumnas()
        {
            if (_data.Columns.Count > 0)
                _col1 = _data.Columns[0].ColumnName;

            if (_data.Columns.Count > 1)
                _col2 = _data.Columns[1].ColumnName;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            AjustarForma(); // 🔥 TU MÉTODO ORIGINAL
            ActualizarContador();
        }

        // 🔍 FILTRO
        private void txtBuscar_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtBuscar.Text))
                {
                    bindingSource1.RemoveFilter();
                }
                else
                {
                    var palabras = txtBuscar.Text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    var filtro = string.Join(" AND ",
                        palabras.Select(p =>
                            $"(({_col1} LIKE '%{p}%') OR ({_col2} LIKE '%{p}%'))"
                        ));

                    bindingSource1.Filter = filtro;
                }

                ActualizarContador();
            }
            catch
            {
                // ignorar errores de filtro como en tu versión original
            }
        }

        private void ActualizarContador()
        {
            lblRegistros.Text = dgv.Rows.Count + " Registros";
        }

        // 🔥 SELECCIÓN CENTRALIZADA
        private void Seleccionar(int rowIndex)
        {
            if (rowIndex < 0) return;

            var row = dgv.Rows[rowIndex];

            if (!(row.DataBoundItem is DataRowView drv))
                return;

            var dataRow = drv.Row;

            var value = dataRow[0]?.ToString();

            Resultado = new LookupResult
            {
                Value = value,
                Description = dataRow.Table.Columns.Count > 1
                    ? dataRow[1]?.ToString()
                    : value,
                Data = dataRow
            };

            DialogResult = DialogResult.OK;
            Close();
        }

        private void dgv_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            Seleccionar(e.RowIndex);
        }

        private void dgv_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;

                if (dgv.CurrentRow != null)
                    Seleccionar(dgv.CurrentRow.Index);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        // 🔥 MÉTODO ORIGINAL (SOLO RENOMBRADO)
        private void AjustarForma()
        {
            try
            {
                float fPixelesAncho;
                int i = 0, j = 0, k = 0;
                float fAnchoColumna = 0;
                int NuevoAnchoDGV = 0;
                Font font = dgv.DefaultCellStyle.Font;
                int nTotalRows = dgv.Rows.Count;
                int nTotalCols = dgv.Columns.Count;

                bool bVisible = true;

                for (i = 0; i < nTotalCols; i++)
                {
                    if (dgv.Columns[i].Name.StartsWith("xOcultarSiguientesCampos"))
                    {
                        bVisible = false;
                        dgv.Columns[i].Visible = false;
                    }
                    else
                    {
                        dgv.Columns[i].Visible = bVisible;
                    }

                    if (dgv.Columns[i].Visible)
                        k = i;
                }

                for (i = 0; i < nTotalCols; i++)
                {
                    if (!dgv.Columns[i].Visible) continue;

                    string sTipoCelda = "";

                    using (Graphics g = CreateGraphics())
                    {
                        fAnchoColumna = g.MeasureString(dgv.Columns[i].HeaderText, font).Width + 22;
                    }

                    for (j = 0; j < nTotalRows; j++)
                    {
                        string sCelda = dgv.Rows[j].Cells[i].Value?.ToString() ?? "";

                        using (Graphics g = CreateGraphics())
                        {
                            fPixelesAncho = g.MeasureString(sCelda, font).Width + 4;
                        }

                        if (fPixelesAncho > fAnchoColumna)
                            fAnchoColumna = fPixelesAncho;

                        if (sTipoCelda == "" || sTipoCelda == "N")
                        {
                            sTipoCelda = double.TryParse(sCelda, out _) ? "N" : "A";
                        }
                    }

                    if (i == 0)
                    {
                        dgv.Columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    }
                    else if (sTipoCelda == "N")
                    {
                        dgv.Columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                        dgv.Columns[i].DefaultCellStyle.Format = "N0";
                    }

                    if (i == k)
                    {
                        float restante = dgv.Width - NuevoAnchoDGV;

                        if (restante > fAnchoColumna)
                            fAnchoColumna = restante;
                    }

                    dgv.Columns[i].Width = (int)fAnchoColumna;
                    NuevoAnchoDGV += (int)fAnchoColumna;
                }

                if (NuevoAnchoDGV > 1000)
                    NuevoAnchoDGV = 1000;

                dgv.Width = NuevoAnchoDGV + 18;
                this.Width = dgv.Width + 20;

                float alto = (int)(nTotalRows * 25.3);

                if (alto >= 240)
                {
                    alto = 240;
                    this.Width += 20;
                }

                dgv.Height = (int)alto + 20;
                this.Height = dgv.Height + 110;
            }
            catch (Exception ex)
            {
                MessageBox.Show("FrmLookup AjustarForma: " + ex.Message);
            }
        }

        // 🔥 MÉTODO ESTÁTICO PARA aTextBox
        public static LookupResult Mostrar(DataTable dt, Control origen)
        {
            using (var frm = new FrmLookup())
            {
                frm.SetData(dt, origen);

                if (frm.ShowDialog() == DialogResult.OK)
                    return frm.Resultado;

                return null;
            }
        }
    }
}