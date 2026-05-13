using System;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Genesys.UI.Controls
{
    [DebuggerStepThrough]
    public class aTextBox : TextBox
    {
        // ───────────────────────────────────────────────────────────────────────
        // CAMPOS
        // ───────────────────────────────────────────────────────────────────────

        private readonly Color _defaultBackColor;
        private Color _backFocusColor = Color.FromArgb(217, 235, 249);
        private bool _mayusculas = false;

        private string _mask;
        private string _watermarkText;
        private bool _mostrarWatermark = true;

        private bool CanEdit => this.Enabled && !this.ReadOnly;

        // ───────────────────────────────────────────────────────────────────────
        // 🔍 LOOKUP (F3)
        // ───────────────────────────────────────────────────────────────────────

        private Button _btnLookup;
        private bool _esLookup;
        private int _labelOriginalLeft;
        private bool _labelPosicionGuardada = false;

        public static Image DefaultLookupImage { get; set; }

        // Constructor estático: se ejecuta una sola vez al cargar la clase
        static aTextBox()
        {
            // La imagen tiene en propiedades : Build Action = Embedded Resource y se incrusta dentro del ensamblado
            // assembly.GetManifestResourceStream(...) ... busca recursos INTERNOS del DLL.
            var assembly = typeof(aTextBox).Assembly;
            using (Stream stream = assembly.GetManifestResourceStream("Genesys.UI.Controls.aTextBox.lupa_16x16.png"))
            {
                if (stream != null)
                {
                    DefaultLookupImage = new Bitmap(stream);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No se encontró lupa_16x16.png");
                }
            }
        }

        // Para indicar el usará el boton F3
        [Category("Lookup ERP")]
        public bool EsLookup
        {
            get => _esLookup;
            set
            {
                _esLookup = value;

                if (_esLookup && this.IsHandleCreated)
                    InicializarLookup();
            }
        }

        // Para guardar la imagen del boton F3
        [Category("Lookup ERP")]
        public Image LookupImage { get; set; }

        [Category("Lookup ERP")]
        public ILookupProvider LookupProvider { get; set; }

        [Category("Lookup ERP")] 
        public Control LookupControl { get; set; }

        [Category("Lookup ERP")]
        public bool LookupAutoValidar { get; set; } = true;

        public event EventHandler<LookupCompletedEventArgs> LookupCompleted;

        // ───────────────────────────────────────────────────────────────────────
        // CONSTRUCTOR
        // ───────────────────────────────────────────────────────────────────────

        public aTextBox()
        {
            this.CausesValidation = true;

            _defaultBackColor = this.BackColor;

            if (this.Font == Control.DefaultFont)
            {
                this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            }
        }

        // ───────────────────────────────────────────────────────────────────────
        // PROPIEDADES 
        // ───────────────────────────────────────────────────────────────────────

        [Category("Codigo Avanzado")]
        public Color BackFocusColor
        {
            get => _backFocusColor;
            set => _backFocusColor = value;
        }

        [Category("Codigo Avanzado")]
        public bool Mayusculas
        {
            get => _mayusculas;
            set
            {
                _mayusculas = value;
                this.CharacterCasing = value ? CharacterCasing.Upper : CharacterCasing.Normal;
            }
        }

        [Category("Codigo Avanzado")]
        public string Mask
        {
            get => _mask;
            set
            {
                _mask = value;
                ConfigureMask();
            }
        }

        [Category("Codigo Avanzado")]
        public bool MostrarWatermark
        {
            get => _mostrarWatermark;
            set
            {
                _mostrarWatermark = value;
                ConfigureMask();
            }
        }

        [Category("Codigo Avanzado")]
        public bool PermitirNegativos { get; set; } = false;

        public event EventHandler _TextChanged
        {
            add => this.TextChanged += value;
            remove => this.TextChanged -= value;
        }

        [Category("Codigo Avanzado")]
        public event EventHandler AfterValidated;

        // ───────────────────────────────────────────────────────────────────────
        // EVENTOS 
        // ───────────────────────────────────────────────────────────────────────

        protected override void OnCreateControl()
        {
            // Debug.WriteLine("OnCreateControl");

            base.OnCreateControl();
            ConfigureMask();

            if (EsLookup)
                InicializarLookup();
        }

        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);

            this.BackColor = _backFocusColor;

            if (CanEdit)
            {
                this.SelectAll();
                InternalEnter();
            }

            // 🔍 HOOK LOOKUP
            if (EsLookup && _btnLookup != null)
            {
                ReubicarBoton();
                _btnLookup.Visible = true;

                if (LookupControl != null)
                {
                    if (!_labelPosicionGuardada)
                    {
                        _labelOriginalLeft = LookupControl.Left;
                        _labelPosicionGuardada = true;
                    }

                    LookupControl.Left = _labelOriginalLeft + 20;
                }
            }
        }

        protected override void OnLeave(EventArgs e)
        {
            var form = this.FindForm();
            bool vaAlBoton = form?.ActiveControl == _btnLookup ||
                 string.IsNullOrEmpty(form?.ActiveControl?.Name) && _btnLookup.Visible;

            //Debug.WriteLine($"OnLeave → ActiveControl: {form?.ActiveControl?.Name ?? "null"}");

            base.OnLeave(e);
            ApplyBackColor();

            if (CanEdit)
            {
                // 🔹 FORMATO
                if (!string.IsNullOrEmpty(_mask) && _mask.Contains(":"))
                {
                    ValidateTimeInput();
                }
                else
                {
                    InternalLeave();
                }

                // 🔍 LOOKUP
                if (EsLookup)
                {
                    if (_btnLookup != null && !vaAlBoton)
                        _btnLookup.Visible = false;

                    if (LookupControl != null && !vaAlBoton)
                        LookupControl.Left = _labelOriginalLeft;

                    //ValidarLookupInterno(); // 🔥 AQUÍ está el cambio
                }
            }

            AfterValidated?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ReubicarBoton();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (EsLookup && keyData == Keys.F3)
            {
                AbrirLookup();
                return true;
            }

            if (keyData == Keys.Enter)
            {
                this.FindForm().SelectNextControl(this, true, true, true, true);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (!CanEdit)
            {
                base.OnKeyPress(e);
                return;
            }

            if (!string.IsNullOrEmpty(_mask))
            {
                if (_mask.Contains(":"))
                {
                    HandleTimeInput(e);
                    return;
                }

                HandleNumericInputByMask(e);
                return;
            }

            base.OnKeyPress(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            ApplyBackColor();
        }

        protected override void OnReadOnlyChanged(EventArgs e)
        {
            base.OnReadOnlyChanged(e);
            ApplyBackColor();
        }
        
        protected override void OnValidating(CancelEventArgs e)
        {
            base.OnValidating(e);

            if (!EsLookup || !CanEdit)
                return;

            if (string.IsNullOrWhiteSpace(this.Text))
            {
                LimpiarLookup();

                LookupCompleted?.Invoke(this, new LookupCompletedEventArgs
                {
                    Success = true
                });

                return;
            }

            try
            {
                var result = LookupProvider.GetByValue(this.Text);

                if (result != null)
                {
                    AsignarLookup(result);

                    LookupCompleted?.Invoke(this, new LookupCompletedEventArgs
                    {
                        Success = true,
                        Value = result.Value,
                        Description = result.Description,
                        Data = result.Data
                    });
                }
                else
                {
                    // limpia el valor de la etiqueta
                    if (LookupControl != null)
                        LookupControl.Text = "";

                    var args = new LookupCompletedEventArgs
                    {
                        Success = false,
                        ErrorMessage = "Valor no válido"
                    };

                    LookupCompleted?.Invoke(this, args);

                    e.Cancel = args.Cancel;
                }
            }
            catch (InvalidOperationException ex)
            {
                // 🔴 Aquí cae cuando hay más de un registro, limpio UI igual que en error
                if (LookupControl != null)
                    LookupControl.Text = "";

                var args = new LookupCompletedEventArgs
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };

                LookupCompleted?.Invoke(this, args);

                e.Cancel = true; 
            }
        }

        // ───────────────────────────────────────────────────────────────────────
        // 🔍 LOOKUP MÉTODOS
        // ───────────────────────────────────────────────────────────────────────

        private void InicializarLookup()
        {
            if (_btnLookup != null) return;

            _btnLookup = new Button
            {
                Size = new Size(24, this.Height - 2),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Standard,
                Visible = false,
                TabStop = false,        // 🔥 Botón NO participa en navegación
                //CausesValidation = false,  // 🔥
                Cursor = Cursors.Hand
            };
            _btnLookup.CausesValidation = true;
            _btnLookup.FlatAppearance.MouseOverBackColor = Color.FromArgb(230, 230, 230);
            _btnLookup.Image = new Bitmap(LookupImage ?? DefaultLookupImage, new Size(16, 16));
            _btnLookup.Text = "";
            _btnLookup.Location = new Point(this.Right + 4, this.Top + 1);
            _btnLookup.MouseDown += (s, e) =>
            {
                this.Focus();      // 🔥 evita salto a otro control
                AbrirLookup();
            };

            //🔥 Bloquear foco del botón
            _btnLookup.GotFocus += (s, e) =>
            {
                this.Focus();
            };

            this.Parent.Controls.Add(_btnLookup);

            ReubicarBoton();
        }

        private void ReubicarBoton()
        {
            if (_btnLookup == null || this.Parent == null) return;

            _btnLookup.Height = this.Height;
            _btnLookup.Location = new Point(this.Right, this.Top);
            _btnLookup.BringToFront();
        }

        private void AbrirLookup()
        {
            if (LookupProvider == null)
                return;

            var dt = LookupProvider.Search();

            if (dt == null || dt.Rows.Count == 0)
                return;

            var result = LookupHelper.Mostrar(dt, this);

            if (result != null)
            {
                AsignarLookup(result);

                LookupCompleted?.Invoke(this, new LookupCompletedEventArgs
                {
                    Success = true,
                    Value = result.Value,
                    Description = result.Description,
                    Data = result.Data
                });
            }
        }

        private void AsignarLookup(LookupResult result)
        {
            this.Text = result.Value;

            if (LookupControl != null)
                LookupControl.Text = result.Description;
        }

        private void LimpiarLookup()
        {
            this.Text = "";

            if (LookupControl != null)
                LookupControl.Text = "";
        }

        public class LookupCompletedEventArgs : EventArgs
        {
            public bool Success { get; set; }

            public string Value { get; set; }
            public string Description { get; set; }
            public DataRow Data { get; set; }

            public string ErrorMessage { get; set; }

            public bool Cancel { get; set; } = true;
        }

        // ───────────────────────────────────────────────────────────────────────
        // ACCIONES
        // ───────────────────────────────────────────────────────────────────────

        private void ApplyBackColor()
        {
            if (!this.Enabled || this.ReadOnly)
                this.BackColor = SystemColors.Control;
            else
                this.BackColor = _defaultBackColor;
        }

        private void ConfigureMask()
        {
            if (string.IsNullOrEmpty(_mask))
            {
                SetCueBanner("");
                return;
            }

            this.TextAlign = HorizontalAlignment.Right;
            _watermarkText = string.Join(" ", _mask.Replace('#', 'ˍ').ToCharArray());

            if (_mostrarWatermark)
                SetCueBanner(_watermarkText);
            else
                SetCueBanner("");
        }

        private void InternalEnter()
        {
            if (string.IsNullOrEmpty(_mask) || _mask.Contains(":"))
                return;

            if (double.TryParse(this.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out double valor))
                this.Text = valor.ToString(CultureInfo.CurrentCulture);
        }

        private void InternalLeave()
        {
            if (string.IsNullOrEmpty(_mask) || _mask.Contains(":"))
                return;

            int decimales = GetMaskDecimals();

            if (double.TryParse(this.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out double valor))
            {
                //_isFormatting = true;
                this.Text = string.Format("{0:N" + decimales + "}", valor);
                //_isFormatting = false;
            }
        }

        private int GetMaskDecimals()
        {
            int punto = _mask.IndexOf('.');
            return punto >= 0 ? _mask.Length - punto - 1 : 0;
        }

        public void SetValor(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                this.Text = "";
                return;
            }

            int decimales = GetMaskDecimals();

            if (double.TryParse(valor, NumberStyles.Any, CultureInfo.CurrentCulture, out double num))
            {
                //_isFormatting = true;
                this.Text = num.ToString("N" + decimales);
                //_isFormatting = false;
            }
        }

        private void HandleNumericInputByMask(KeyPressEventArgs e)
        {
            var parts = _mask.Split('.');
            int enteros = parts[0].Count(c => c == '#');
            int decimales = parts.Length > 1 ? parts[1].Count(c => c == '#') : 0;

            if (char.IsControl(e.KeyChar)) return;

            if (e.KeyChar == '-')
            {
                if (!PermitirNegativos || this.SelectionStart != 0 || this.Text.Contains("-"))
                {
                    e.Handled = true;
                }
                return;
            }

            if (char.IsDigit(e.KeyChar))
            {
                string texto = this.Text.Replace("-", "");
                string[] txt = texto.Split('.');

                int intLen = txt[0].Length;
                int decLen = txt.Length > 1 ? txt[1].Length : 0;

                if (this.SelectionLength == this.Text.Length)
                {
                    this.Text = "";
                    return;
                }

                int cursor = this.SelectionStart;
                if (this.Text.StartsWith("-")) cursor--;

                if (cursor <= intLen && intLen >= enteros)
                {
                    e.Handled = true;
                    return;
                }

                if (this.Text.Contains(".") &&
                    cursor > intLen &&
                    decLen >= decimales)
                {
                    e.Handled = true;
                    return;
                }
            }
            else if (e.KeyChar == '.')
            {
                if (decimales == 0)
                {
                    e.Handled = true;
                    return;
                }

                if (this.Text.Contains("."))
                {
                    this.SelectionStart = this.Text.IndexOf('.') + 1;
                    e.Handled = true;
                    return;
                }

                if (string.IsNullOrEmpty(this.Text) || this.Text == "-")
                {
                    this.Text = (this.Text.StartsWith("-") ? "-0." : "0.");
                    this.SelectionStart = this.Text.Length;
                }
                else
                {
                    int pos = this.SelectionStart;
                    this.Text = this.Text.Insert(pos, ".");
                    this.SelectionStart = pos + 1;
                }

                e.Handled = true;
            }
            else
            {
                e.Handled = true;
            }
        }

        private void HandleTimeInput(KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;

            if (char.IsDigit(e.KeyChar))
            {
                string[] parts = this.Text.Split(':');
                int h = parts[0].Length;
                int m = parts.Length > 1 ? parts[1].Length : 0;

                if (this.SelectionStart <= h && h >= 2) { e.Handled = true; return; }
                if (this.Text.Contains(":") && this.SelectionStart > h && m >= 2)
                { e.Handled = true; return; }
            }
            else if (e.KeyChar == ':')
            {
                if (this.Text.Contains(":"))
                {
                    this.SelectionStart = this.Text.IndexOf(':') + 1;
                    e.Handled = true;
                }
                else
                {
                    this.Text = "00:";
                    this.SelectionStart = this.Text.Length;
                    e.Handled = true;
                }
            }
            else
            {
                e.Handled = true;
            }
        }

        private void ValidateTimeInput()
        {
            var parts = this.Text.Split(':');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out int h) &&
                int.TryParse(parts[1], out int m))
            {
                if (h < 0 || h > 24 || m < 0 || m > 59)
                {
                    MessageBox.Show("Hora inválida HH:MM");
                    this.Text = "";
                }
            }
        }

        private const int EM_SETCUEBANNER = 0x1501;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern Int32 SendMessage(IntPtr hWnd, int msg, int wParam, string lParam);

        private void SetCueBanner(string text)
        {
            if (this.IsHandleCreated && !this.Focused)
                SendMessage(this.Handle, EM_SETCUEBANNER, 0, text);
        }

        public static class LookupHelper
        {
            public static LookupResult Mostrar(DataTable dt, Control origen)
            {
                using (var frm = new FrmLookup())
                {
                    frm.SetData(dt, origen);

                    if (frm.ShowDialog() == DialogResult.OK)
                    {
                        return frm.Resultado; // 🔥 DIRECTO
                    }

                    return null;
                }
            }
        }
    }
}
#region documenta errores
//El parpadeo al hacer click sobre el boton lookup era causado por una combinación de dos cosas:

//OnEnter lo volvía a mover a +20 inmediatamente después porque this.Focus() en el MouseDown del botón regresaba el foco al TextBox.

//Ese viaje rápido +20 → original → +20 era el parpadeo visible.
//La solución fue usar ese ActiveControl vacío como señal — si está vacío, el foco va al botón lookup, entonces OnLeave simplemente no mueve nada y el control se queda quieto.
#endregion