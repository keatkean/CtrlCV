using System.Runtime.InteropServices;

namespace CtrlCV
{
    internal static class Program
    {
        private static Form1? _mainForm;
        private static Mutex? _singleInstanceMutex;

        private const string MutexName = "Global\\CtrlCV_SingleInstance_B7A3F2E1";

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        private const int SW_RESTORE = 9;

        [STAThread]
        static void Main()
        {
            _singleInstanceMutex = new Mutex(true, MutexName, out bool createdNew);

            if (!createdNew)
            {
                ActivateExistingInstance();
                return;
            }

            try
            {
                NativeMethods.SetProcessDPIAware();

                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += OnThreadException;
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

                ApplicationConfiguration.Initialize();
                _mainForm = new Form1();
                Application.Run(_mainForm);
            }
            finally
            {
                _singleInstanceMutex.ReleaseMutex();
                _singleInstanceMutex.Dispose();
            }
        }

        private static void ActivateExistingInstance()
        {
            try
            {
                var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                var processes = System.Diagnostics.Process.GetProcessesByName(currentProcess.ProcessName);

                foreach (var proc in processes)
                {
                    if (proc.Id != currentProcess.Id && proc.MainWindowHandle != IntPtr.Zero)
                    {
                        if (IsIconic(proc.MainWindowHandle))
                            ShowWindow(proc.MainWindowHandle, SW_RESTORE);
                        SetForegroundWindow(proc.MainWindowHandle);
                        break;
                    }
                }
            }
            catch { }
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            HandleFatalError(e.Exception);
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                HandleFatalError(ex);
            else
                HandleFatalError(new Exception($"Non-exception object thrown: {e.ExceptionObject}"));
        }

        private static void HandleFatalError(Exception ex)
        {
            try
            {
                Form1.LogError("UNHANDLED EXCEPTION", ex);
            }
            catch { }

            try
            {
                MessageBox.Show(
                    $"An unexpected error occurred:\n\n{ex.Message}\n\n" +
                    "Details have been written to ctrlcv_error.log.\n" +
                    "The application will now close.",
                    "CtrlCV - Unexpected Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch { }

            try
            {
                if (_mainForm != null && !_mainForm.IsDisposed)
                {
                    _mainForm.Close();
                }
            }
            catch { }

            Environment.Exit(1);
        }
    }
}
