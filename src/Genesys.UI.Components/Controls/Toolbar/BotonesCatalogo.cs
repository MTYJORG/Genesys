using System.Collections.Generic;
using Genesys.UI.Components.Properties;

namespace Genesys.UI.Components.Controls.Toolbar
{
    public static class BotonesCatalogo
    {
        private static readonly Dictionary<BotonTipo, BotonInfo> botones = new Dictionary<BotonTipo, BotonInfo>()
        {
            { BotonTipo.Abrir, new BotonInfo {Texto = "Abrir", Tooltip = "Abrir registro", Icono = Resources.abrir } },
            { BotonTipo.Ayuda, new BotonInfo { Texto = "Ayuda", Tooltip = "Mostrar ayuda", Icono = Resources.ayuda } },
            { BotonTipo.Buscar, new BotonInfo { Texto = "Buscar", Tooltip = "Buscar información", Icono = Resources.buscar } },
            { BotonTipo.Cerrar, new BotonInfo { Texto = "Cerrar", Tooltip = "Cerrar <Alt+F4>", Icono = Resources.cerrar } },
            { BotonTipo.Eliminar, new BotonInfo { Texto = "Eliminar", Tooltip = "Eliminar registro", Icono = Resources.eliminar } },
            { BotonTipo.ExportarExcel, new BotonInfo { Texto = "Exportar Excel", Tooltip = "Exportar a Excel", Icono = Resources.ExportarExcel } },
            { BotonTipo.ExportarPdf, new BotonInfo { Texto = "Exportar PDF", Tooltip = "Exportar a PDF", Icono = Resources.ExportarPdf } },
            { BotonTipo.Guardar, new BotonInfo { Texto = "Guardar", Tooltip = "Guardar registro", Icono = Resources.guardar} },
            { BotonTipo.Imprimir, new BotonInfo { Texto = "Imprimir", Tooltip = "Imprimir documento", Icono = Resources.ayuda } },
            { BotonTipo.Modificar, new BotonInfo { Texto = "Modificar", Tooltip = "Modificar documento", Icono = Resources.modificar } },
            { BotonTipo.Nuevo, new BotonInfo {Texto = "Nuevo", Tooltip = "Nuevo registro", Icono = Resources.nuevo } },
            { BotonTipo.Refrescar, new BotonInfo {Texto = "Refrescar", Tooltip = "Refrescar", Icono = Resources.refrescar } },
            { BotonTipo.Respaldar, new BotonInfo {Texto = "Respaldar", Tooltip = "respaldar", Icono = Resources.respaldar } },
            { BotonTipo.Restaurar, new BotonInfo {Texto = "Restaurar", Tooltip = "restaurar", Icono = Resources.restaurar } },
        };

        public static BotonInfo Obtener(BotonTipo tipo)
        {
            return botones[tipo];
        }
    }
}