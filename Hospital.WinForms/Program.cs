using Hospital.Data;
using Hospital.Domain;
using Hospital.Services;

namespace Hospital.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        try { 
            Application.Run(new MainForm(new HospitalService(new JsonHospitalRepository()))); 
        }
        catch (Exception ex) { 
            MessageBox.Show(ex.Message, "Ошибка запуска", MessageBoxButtons.OK, MessageBoxIcon.Error); 
        }
    }
}
