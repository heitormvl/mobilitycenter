namespace MobilityCenter.Frontend.Services;

public class ConfirmOptions
{
    public string Title        { get; init; } = "Confirmar";
    public string Message      { get; init; } = "Tem certeza?";
    public string ConfirmLabel { get; init; } = "Confirmar";
    public string CancelLabel  { get; init; } = "Cancelar";

    /// <summary>danger | warning | success</summary>
    public string Variant { get; init; } = "danger";
}

public class ConfirmService
{
    private TaskCompletionSource<bool>? _tcs;

    /// <summary>Fired when ShowAsync is called. The modal subscribes to this.</summary>
    public event Action<ConfirmOptions>? OnShow;

    /// <summary>Shows the confirm modal and awaits the user's choice.</summary>
    public Task<bool> ShowAsync(ConfirmOptions options)
    {
        _tcs = new TaskCompletionSource<bool>();
        OnShow?.Invoke(options);
        return _tcs.Task;
    }

    /// <summary>Called by the modal when the user clicks Confirm or Cancel.</summary>
    public void Complete(bool result) => _tcs?.TrySetResult(result);
}
