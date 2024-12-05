using System;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using WzVisualizer.GUI;

namespace WzVisualizer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplyCulture();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        static void ApplyCulture()
        {
            CultureInfo culture = CultureInfo.CurrentCulture;
            Thread.CurrentThread.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
        }
    }
}
