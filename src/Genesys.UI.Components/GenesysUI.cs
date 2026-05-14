using System.Drawing;

namespace Genesys.UI.Components
{
    public static class GenesysUI
    {
        // Manejo del Logo que aparece en la barra de titulos del formulario.
        // Se establece en program.cs 
        internal static Icon AppIcon { get; private set; }

        public static void Initialize(Icon appIcon)
        {
            AppIcon = appIcon;
        }
    }
}
