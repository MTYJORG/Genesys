using Syncfusion.WinForms.DataGrid;
using System;
using System.Runtime.CompilerServices;

namespace Genesys.UI.Components.Controls.GridViews
{
    /// <summary>
    /// Fuente única para interpretar y aplicar la alineación visual de columnas.
    ///
    /// Regla central:
    /// - Automatic significa "no es alineación manual".
    /// - Left/Center/Right significan decisión explícita del usuario.
    /// - La alineación efectiva para exportar se resuelve aquí con vista + tipo + formato.
    ///
    /// Esta clase evita que GridConfigurator, VistasAdministrador, Designer, Excel y PDF
    /// interpreten "Automatic" de formas diferentes.
    /// </summary>
    internal static class GenesysGridColumnVisualHelper
    {
        private sealed class AlignmentState
        {
            public string Alignment;
        }

        private static readonly ConditionalWeakTable<GridColumn, AlignmentState> ExplicitAlignmentByColumn =
            new ConditionalWeakTable<GridColumn, AlignmentState>();

        public const string AlignmentAutomatic = "Automatic";
        public const string AlignmentLeft = "Left";
        public const string AlignmentCenter = "Center";
        public const string AlignmentRight = "Right";

        public static string NormalizeAlignment(string alignment)
        {
            if (string.IsNullOrWhiteSpace(alignment))
                return AlignmentAutomatic;

            string value = alignment.Trim();

            if (value.Equals("Automático", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Automatico", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Auto", StringComparison.OrdinalIgnoreCase) ||
                value.Equals(AlignmentAutomatic, StringComparison.OrdinalIgnoreCase))
                return AlignmentAutomatic;

            if (value.Equals("Izquierda", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Near", StringComparison.OrdinalIgnoreCase) ||
                value.Equals(AlignmentLeft, StringComparison.OrdinalIgnoreCase))
                return AlignmentLeft;

            if (value.Equals("Centro", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Middle", StringComparison.OrdinalIgnoreCase) ||
                value.Equals(AlignmentCenter, StringComparison.OrdinalIgnoreCase))
                return AlignmentCenter;

            if (value.Equals("Derecha", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Far", StringComparison.OrdinalIgnoreCase) ||
                value.Equals(AlignmentRight, StringComparison.OrdinalIgnoreCase))
                return AlignmentRight;

            return AlignmentAutomatic;
        }

        public static bool IsAutomaticAlignment(string alignment)
        {
            return NormalizeAlignment(alignment) == AlignmentAutomatic;
        }

        public static bool IsExplicitAlignment(string alignment)
        {
            string normalized = NormalizeAlignment(alignment);
            return normalized == AlignmentLeft ||
                   normalized == AlignmentCenter ||
                   normalized == AlignmentRight;
        }

        public static string ToDisplayAlignment(string alignment)
        {
            string normalized = NormalizeAlignment(alignment);

            if (normalized == AlignmentLeft)
                return "Izquierda";

            if (normalized == AlignmentCenter)
                return "Centro";

            if (normalized == AlignmentRight)
                return "Derecha";

            return "Automático";
        }

        public static string FromDisplayAlignment(string display)
        {
            return NormalizeAlignment(display);
        }

        public static string[] GetAlignmentDisplayOptions()
        {
            return new[] { "Automático", "Izquierda", "Centro", "Derecha" };
        }

        public static string[] GetAlignmentDisplayOptionsForKind(string kindName)
        {
            if (string.Equals(kindName, "Numeric", StringComparison.OrdinalIgnoreCase))
                return new[] { "Automático", "Derecha", "Izquierda", "Centro" };

            if (string.Equals(kindName, "Date", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(kindName, "Boolean", StringComparison.OrdinalIgnoreCase))
                return new[] { "Automático", "Derecha", "Izquierda", "Centro" };

            return GetAlignmentDisplayOptions();
        }

        public static void ApplyAlignment(GridColumn column, string alignment)
        {
            if (column == null)
                return;

            string normalized = NormalizeAlignment(alignment);

            if (normalized == AlignmentAutomatic)
            {
                ClearExplicitAlignment(column);
                ApplyAutomaticCellAlignment(column);
                return;
            }

            SetExplicitAlignment(column, normalized);

            if (normalized == AlignmentCenter)
            {
                ApplyWinFormsAlignment(column, System.Windows.Forms.HorizontalAlignment.Center);
                return;
            }

            if (normalized == AlignmentRight)
            {
                ApplyWinFormsAlignment(column, System.Windows.Forms.HorizontalAlignment.Right);
                return;
            }

            ApplyWinFormsAlignment(column, System.Windows.Forms.HorizontalAlignment.Left);
        }

        public static string DetectAlignment(GridColumn column)
        {
            if (column == null)
                return AlignmentAutomatic;

            string explicitAlignment = GetExplicitAlignment(column);
            if (IsExplicitAlignment(explicitAlignment))
                return explicitAlignment;

            try
            {
                if (column.CellStyle != null &&
                    column.CellStyle.HorizontalAlignment == System.Windows.Forms.HorizontalAlignment.Right)
                    return AlignmentRight;

                if (column.CellStyle != null &&
                    column.CellStyle.HorizontalAlignment == System.Windows.Forms.HorizontalAlignment.Center)
                    return AlignmentCenter;
            }
            catch
            {
            }

            return AlignmentAutomatic;
        }

        public static string GetExplicitAlignment(GridColumn column)
        {
            if (column == null)
                return null;

            AlignmentState state;
            if (ExplicitAlignmentByColumn.TryGetValue(column, out state))
                return NormalizeAlignment(state == null ? null : state.Alignment);

            return null;
        }

        public static string ResolveEffectiveAlignment(string viewAlignment, GridColumn gridColumn, Type dataType, string format)
        {
            string normalizedViewAlignment = NormalizeAlignment(viewAlignment);

            if (IsExplicitAlignment(normalizedViewAlignment))
                return normalizedViewAlignment;

            if (IsRightAlignedByColumn(gridColumn))
                return AlignmentRight;

            if (IsRightAlignedByFormat(format))
                return AlignmentRight;

            dataType = Nullable.GetUnderlyingType(dataType) ?? dataType;

            if (IsNumericType(dataType) || IsDateType(dataType))
                return AlignmentRight;

            return AlignmentLeft;
        }

        public static string ResolveEffectiveAlignment(string viewAlignment, object gridColumn, Type dataType, string format)
        {
            return ResolveEffectiveAlignment(viewAlignment, gridColumn as GridColumn, dataType, format);
        }

        public static bool IsRightAlignedByColumn(GridColumn column)
        {
            if (column == null)
                return false;

            string columnTypeName = column.GetType().Name.ToUpperInvariant();

            return columnTypeName.Contains("NUMERIC") ||
                   columnTypeName.Contains("CURRENCY") ||
                   columnTypeName.Contains("DECIMAL") ||
                   columnTypeName.Contains("DOUBLE") ||
                   columnTypeName.Contains("INT") ||
                   columnTypeName.Contains("DATE");
        }

        public static bool IsRightAlignedByFormat(string format)
        {
            if (string.IsNullOrWhiteSpace(format))
                return false;

            string normalizedFormat = format.Trim().ToUpperInvariant();

            return normalizedFormat.StartsWith("N") ||
                   normalizedFormat.StartsWith("C") ||
                   normalizedFormat.StartsWith("P") ||
                   normalizedFormat.StartsWith("F") ||
                   normalizedFormat.Contains("#,##") ||
                   normalizedFormat.Contains("0.00");
        }

        public static bool IsNumericType(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            return type == typeof(byte) ||
                   type == typeof(short) ||
                   type == typeof(int) ||
                   type == typeof(long) ||
                   type == typeof(float) ||
                   type == typeof(double) ||
                   type == typeof(decimal);
        }

        public static bool IsDateType(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return type == typeof(DateTime);
        }

        private static void SetExplicitAlignment(GridColumn column, string alignment)
        {
            if (column == null)
                return;

            AlignmentState state = ExplicitAlignmentByColumn.GetOrCreateValue(column);
            state.Alignment = NormalizeAlignment(alignment);
        }

        private static void ClearExplicitAlignment(GridColumn column)
        {
            if (column == null)
                return;

            try
            {
                ExplicitAlignmentByColumn.Remove(column);
            }
            catch
            {
            }
        }

        private static void ApplyAutomaticCellAlignment(GridColumn column)
        {
            if (column == null)
                return;

            System.Windows.Forms.HorizontalAlignment alignment =
                IsRightAlignedByColumn(column)
                    ? System.Windows.Forms.HorizontalAlignment.Right
                    : System.Windows.Forms.HorizontalAlignment.Left;

            try
            {
                if (column.CellStyle != null)
                    column.CellStyle.HorizontalAlignment = alignment;
            }
            catch
            {
            }
        }

        private static void ApplyWinFormsAlignment(GridColumn column, System.Windows.Forms.HorizontalAlignment alignment)
        {
            try
            {
                if (column.CellStyle != null)
                    column.CellStyle.HorizontalAlignment = alignment;
            }
            catch
            {
            }

            try
            {
                if (column.HeaderStyle != null)
                    column.HeaderStyle.HorizontalAlignment = alignment;
            }
            catch
            {
            }
        }
    }
}
