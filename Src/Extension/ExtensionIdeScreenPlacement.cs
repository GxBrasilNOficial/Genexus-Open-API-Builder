#nullable enable

using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace GenexusOpenApiBuilder.Extension;

/// <summary>
/// Posiciona diálogos da extensão no monitor da IDE GeneXus.
/// CenterParent não funciona com owner que não é Form (HWND da IDE);
/// CenterScreen cai no monitor primário.
/// </summary>
internal static class ExtensionIdeScreenPlacement
{
    public static IWin32Window? ResolveOwner()
    {
        var mainWindowHandle = Process.GetCurrentProcess().MainWindowHandle;
        if (mainWindowHandle != IntPtr.Zero)
        {
            return new NativeWindowHandle(mainWindowHandle);
        }

        if (Form.ActiveForm is { } activeForm
            && activeForm.Visible
            && !activeForm.IsDisposed)
        {
            return activeForm;
        }

        return null;
    }

    public static void CenterOnIdeScreen(Form form, IWin32Window? owner)
    {
        if (form is null)
        {
            throw new ArgumentNullException(nameof(form));
        }

        form.StartPosition = FormStartPosition.Manual;
        var working = GetWorkingArea(form, owner);
        form.Location = new Point(
            working.Left + Math.Max(0, (working.Width - form.Width) / 2),
            working.Top + Math.Max(0, (working.Height - form.Height) / 2));
    }

    public static Rectangle GetWorkingArea(Form form, IWin32Window? owner)
    {
        if (owner is not null && owner.Handle != IntPtr.Zero)
        {
            return Screen.FromHandle(owner.Handle).WorkingArea;
        }

        if (form.IsHandleCreated)
        {
            return Screen.FromHandle(form.Handle).WorkingArea;
        }

        var processMainWindowHandle = Process.GetCurrentProcess().MainWindowHandle;
        if (processMainWindowHandle != IntPtr.Zero)
        {
            return Screen.FromHandle(processMainWindowHandle).WorkingArea;
        }

        return Screen.PrimaryScreen?.WorkingArea ?? Screen.AllScreens[0].WorkingArea;
    }

    private sealed class NativeWindowHandle : IWin32Window
    {
        public NativeWindowHandle(IntPtr handle)
        {
            Handle = handle;
        }

        public IntPtr Handle { get; }
    }
}
