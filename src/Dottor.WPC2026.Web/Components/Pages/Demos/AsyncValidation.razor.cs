using Microsoft.AspNetCore.Components.Forms;
using Dottor.WPC2026.Web.Models;

namespace Dottor.WPC2026.Web.Components.Pages.Demos;

public partial class AsyncValidation
{
    private RegistrationModel _model = new();
    private EditContext _editContext = default!;
    private readonly List<(DateTime Time, string Text)> _log = [];
    private bool _isSubmitting;
    private string? _registeredMessage;
    private bool _wasEmailPending;

    private bool EmailPending => _editContext.IsValidationPending(() => _model.Email);
    private bool EmailFaulted => _editContext.IsValidationFaulted(() => _model.Email);
    private bool EmailAvailable =>
        !string.IsNullOrWhiteSpace(_model.Email)
        && _editContext.IsModified(() => _model.Email)
        && !_editContext.GetValidationMessages(() => _model.Email).Any();

    protected override void OnInitialized() => CreateEditContext();

    private void CreateEditContext()
    {
        if (_editContext is not null)
        {
            _editContext.OnFieldChanged -= OnFieldChanged;
            _editContext.OnValidationStateChanged -= OnValidationStateChanged;
        }

        _model = new RegistrationModel();
        _editContext = new EditContext(_model);
        _editContext.OnFieldChanged += OnFieldChanged;
        _editContext.OnValidationStateChanged += OnValidationStateChanged;
        _wasEmailPending = false;
    }

    private void OnFieldChanged(object? sender, FieldChangedEventArgs e)
    {
        _registeredMessage = null;
        if (e.FieldIdentifier.FieldName == nameof(RegistrationModel.Email))
        {
            if (_wasEmailPending)
            {
                AddLog("Email modificata durante la verifica: il controllo precedente viene annullato");
            }
            AddLog($"Email = \"{_model.Email}\": regole sincrone valutate, controllo asincrono avviato");
        }
        else
        {
            // Il DataAnnotationsValidator gestisce lo stesso evento dopo di noi: leggiamo i messaggi subito dopo.
            var field = e.FieldIdentifier;
            _ = InvokeAsync(async () =>
            {
                await Task.Yield();
                var messages = _editContext.GetValidationMessages(field).ToList();
                AddLog($"{field.FieldName}: regole sincrone valutate subito → {(messages.Count == 0 ? "valido" : messages[0])}");
                StateHasChanged();
            });
        }
    }

    // Lo stato asincrono cambia fuori dal ciclo dell'evento UI: serve un nuovo render.
    private void OnValidationStateChanged(object? sender, ValidationStateChangedEventArgs e)
    {
        var pending = EmailPending;
        if (_wasEmailPending && !pending)
        {
            var message = _editContext.GetValidationMessages(_editContext.Field(nameof(RegistrationModel.Email))).FirstOrDefault();
            AddLog(EmailFaulted
                ? "Verifica email terminata con errore (faulted)"
                : $"Verifica email completata → {message ?? "disponibile"}");
        }
        _wasEmailPending = pending;
        _ = InvokeAsync(StateHasChanged);
    }

    private async Task RegisterAsync()
    {
        _isSubmitting = true;
        await Users.RegisterAsync(_model.Email!, _model.FullName!);
        _registeredMessage = $"Benvenuto nel Pizza Club, {_model.FullName}! 🍕 La tessera è legata a {_model.Email}: ora l'indirizzo risulta già iscritto.";
        AddLog($"Submit valido: {_model.Email} iscritta al Pizza Club");
        _isSubmitting = false;
        var message = _registeredMessage;
        CreateEditContext();
        _registeredMessage = message;
    }

    private void AddLog(string text)
    {
        _log.Insert(0, (DateTime.Now, text));
        if (_log.Count > 30)
        {
            _log.RemoveAt(_log.Count - 1);
        }
    }

    public void Dispose()
    {
        _editContext.OnFieldChanged -= OnFieldChanged;
        _editContext.OnValidationStateChanged -= OnValidationStateChanged;
    }
}
