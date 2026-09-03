#nullable enable

using System;
using System.Diagnostics;
using System.Windows.Forms;
using GenexusOpenApiBuilder.Extension.Diagnostics;

namespace GenexusOpenApiBuilder.Extension;

/// <summary>
/// B082 — abre o diálogo modeless, permite Abortar via DoEvents entre itens, e fecha no Dispose.
/// </summary>
internal sealed class ExtensionBusyProgressScope : IDisposable
{
    private readonly ExtensionBusyProgressDialog _dialog;
    private readonly Control? _ownerControl;
    private readonly bool _previousOwnerWaitCursor;
    private bool _disposed;

    private ExtensionBusyProgressScope(
        ExtensionBusyProgressDialog dialog,
        Control? ownerControl,
        bool previousOwnerWaitCursor)
    {
        _dialog = dialog;
        _ownerControl = ownerControl;
        _previousOwnerWaitCursor = previousOwnerWaitCursor;
    }

    public ApiPlanBusyProgressSession Session => _dialog.Session;

    public static ExtensionBusyProgressScope Show(IWin32Window? owner, string title, ExtensionTexts texts)
    {
        ExtensionBusyProgressDialog? dialog = null;
        var session = new ApiPlanBusyProgressSession(
            update =>
            {
                dialog?.ApplyUpdate(update);
                Application.DoEvents();
            },
            () => Application.DoEvents());

        dialog = new ExtensionBusyProgressDialog(title, texts, session);
        Control? ownerControl = owner as Control;
        var previousWait = false;
        if (ownerControl is not null)
        {
            previousWait = ownerControl.UseWaitCursor;
            ownerControl.UseWaitCursor = true;
        }

        dialog.UseWaitCursor = true;
        Cursor.Current = Cursors.WaitCursor;
        var placementOwner = owner ?? ExtensionIdeScreenPlacement.ResolveOwner();
        ExtensionIdeScreenPlacement.CenterOnIdeScreen(dialog, placementOwner);
        if (placementOwner is null)
        {
            dialog.Show();
        }
        else
        {
            dialog.Show(placementOwner);
        }

        session.Report(texts.BusyProgressStarting, 0, 0, string.Empty);
        Application.DoEvents();
        return new ExtensionBusyProgressScope(dialog, ownerControl, previousWait);
    }

    public void Report(string stage, int current, int total, string itemName, long elapsedMs = -1)
    {
        Session.Report(stage, current, total, itemName, elapsedMs);
    }

    public void ThrowIfAbortRequested()
    {
        Session.ThrowIfAbortRequested();
    }

    public long Measure(Action action)
    {
        var sw = Stopwatch.StartNew();
        action();
        sw.Stop();
        return sw.ElapsedMilliseconds;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_ownerControl is not null && !_ownerControl.IsDisposed)
        {
            _ownerControl.UseWaitCursor = _previousOwnerWaitCursor;
        }

        Cursor.Current = Cursors.Default;
        if (!_dialog.IsDisposed)
        {
            _dialog.Close();
            _dialog.Dispose();
        }
    }
}
