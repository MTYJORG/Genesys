using System;
using System.Windows.Forms;

namespace Genesys.UI.Components.Forms.Services
{
    public static class GenesysFormLauncher
    {
        private const int SPLASH_DELAY_MS = 500; // Umbral en milisegundos

        /// <summary>
        /// Abre un GenesysGridForm desde una aplicación que ya tiene un message loop activo
        /// (por ejemplo, desde un menú principal). El formulario se muestra inicialmente
        /// invisible para conservar el ciclo normal de WinForms y se revela al terminar
        /// la primera carga del grid. El splash se muestra solo si la carga toma más de SPLASH_DELAY_MS.
        /// </summary>
        public static void ShowGridForm<T>() where T : GenesysGridForm, new()
        {
            LaunchForm<T>(showAction: form => form.Show());
        }

        /// <summary>
        /// Ejecuta un GenesysGridForm como formulario principal de una aplicación
        /// sin menú previo. Al cerrar este formulario, termina la aplicación.
        /// El splash se muestra solo si la carga toma más de SPLASH_DELAY_MS.
        /// </summary>
        public static void RunGridForm<T>() where T : GenesysGridForm, new()
        {
            LaunchForm<T>(showAction: form => Application.Run(form));
        }

        private static void LaunchForm<T>(Action<Form> showAction) where T : GenesysGridForm, new()
        {
            bool loadCompleted = false;
            GenesysLoadingForm splash = null;
            Timer delayTimer = null;

            // Crear el formulario destino (invisible al principio)
            T form = new T
            {
                StartPosition = FormStartPosition.CenterScreen,
                Opacity = 0
            };

            // Manejador de carga completada
            EventHandler loadCompletedHandler = null;
            loadCompletedHandler = (sender, e) =>
            {
                if (loadCompleted) return;
                loadCompleted = true;
                form.InitialLoadCompleted -= loadCompletedHandler;

                // Si el timer aún está activo (no ha mostrado splash), cancelarlo
                if (delayTimer != null && delayTimer.Enabled)
                {
                    delayTimer.Stop();
                    delayTimer.Dispose();
                    delayTimer = null;
                    // Revelar formulario sin splash (carga rápida)
                    RevealFormWithoutSplash(form);
                }
                else
                {
                    // El splash ya se mostró (o el timer ya pasó) -> revelar y cerrar splash
                    RevealForm(form, splash);
                }
            };
            form.InitialLoadCompleted += loadCompletedHandler;

            // Timer que muestra el splash después del retraso
            delayTimer = new Timer { Interval = SPLASH_DELAY_MS };
            delayTimer.Tick += (timerSender, timerArgs) =>
            {
                delayTimer.Stop(); // Ejecutar solo una vez
                if (loadCompleted) return; // La carga terminó antes de mostrar splash

                // Crear y mostrar el splash
                splash = new GenesysLoadingForm();
                splash.Show();
                Application.DoEvents(); // Forzar actualización visual
            };
            delayTimer.Start();

            // Limpiar recursos si el formulario se cierra antes de completar la carga
            form.FormClosed += (sender, e) =>
            {
                if (delayTimer != null && delayTimer.Enabled)
                {
                    delayTimer.Stop();
                    delayTimer.Dispose();
                    delayTimer = null;
                }
                CloseSplash(splash);
            };

            // Mostrar o ejecutar el formulario (inicialmente invisible)
            showAction(form);
        }

        private static void RevealFormWithoutSplash(GenesysGridForm form)
        {
            if (form == null || form.IsDisposed)
                return;

            Action reveal = () =>
            {
                if (form.IsDisposed) return;
                form.Opacity = 1;
                form.Activate();
            };

            if (form.IsHandleCreated)
                form.BeginInvoke(reveal);
            else
                reveal();
        }

        private static void RevealForm(GenesysGridForm form, GenesysLoadingForm splash)
        {
            CloseSplash(splash);

            if (form == null || form.IsDisposed)
                return;

            Action reveal = () =>
            {
                if (form.IsDisposed) return;
                form.Opacity = 1;
                form.Activate();
            };

            if (form.IsHandleCreated)
                form.BeginInvoke(reveal);
            else
                reveal();
        }

        private static void CloseSplash(GenesysLoadingForm splash)
        {
            if (splash == null || splash.IsDisposed)
                return;
            splash.Close();
        }
    }
}