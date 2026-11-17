using Microsoft.AspNetCore.Components.Forms;
using Dottor.WPC2026.Web.Models;

namespace Dottor.WPC2026.Web.Components.Pages.Demos;

public partial class AsyncValidation
{
    private RegistrationModel model = new();
    private EditContext editContext = default!;
    private readonly List<(DateTime Time, string Text)> log = [];
    private bool isSubmitting;
    private string? registeredMessage;
    private bool wasEmailPending;

    private bool EmailPending => editContext.IsValidationPending(() => model.Email);
    private bool EmailFaulted => editContext.IsValidationFaulted(() => model.Email);
    private bool EmailAvailable =>
        !string.IsNullOrWhiteSpace(model.Email)
        && editContext.IsModified(() => model.Email)
        && !editContext.GetValidationMessages(() => model.Email).Any();

    protected override void OnInitialized() => CreateEditContext();

    private void CreateEditContext()
    {
        if (editContext is not null)
        {
            editContext.OnFieldChanged -= OnFieldChanged;
            editContext.OnValidationStateChanged -= OnValidationStateChanged;
        }

        model = new RegistrationModel();
        editContext = new EditContext(model);
        editContext.OnFieldChanged += OnFieldChanged;
        editContext.OnValidationStateChanged += OnValidationStateChanged;
        wasEmailPending = false;
    }

    private void OnFieldChanged(object? sender, FieldChangedEventArgs e)
    {
        registeredMessage = null;
        if (e.FieldIdentifier.FieldName == nameof(RegistrationModel.Email))
        {
            if (wasEmailPending)
            {
                AddLog("Email modificata durante la verifica: il controllo precedente viene annullato");
            }
            AddLog($"Email = \"{model.Email}\": regole sincrone valutate, controllo asincrono avviato");
        }
        else
        {
            // Il DataAnnotationsValidator gestisce lo stesso evento dopo di noi: leggiamo i messaggi subito dopo.
            var field = e.FieldIdentifier;
            _ = InvokeAsync(async () =>
            {
                await Task.Yield();
                var messages = editContext.GetValidationMessages(field).ToList();
                AddLog($"{field.FieldName}: regole sincrone valutate subito → {(messages.Count == 0 ? "valido" : messages[0])}");
                StateHasChanged();
            });
        }
    }

    // Lo stato asincrono cambia fuori dal ciclo dell'evento UI: serve un nuovo render.
    private void OnValidationStateChanged(object? sender, ValidationStateChangedEventArgs e)
    {
        var pending = EmailPending;
        if (wasEmailPending && !pending)
        {
            var message = editContext.GetValidationMessages(editContext.Field(nameof(RegistrationModel.Email))).FirstOrDefault();
            AddLog(EmailFaulted
                ? "Verifica email terminata con errore (faulted)"
                : $"Verifica email completata → {message ?? "disponibile"}");
        }
        wasEmailPending = pending;
        _ = InvokeAsync(StateHasChanged);
    }

    private async Task RegisterAsync()
    {
        isSubmitting = true;
        await Users.RegisterAsync(model.Email!, model.FullName!);
        registeredMessage = $"Benvenuto nel Pizza Club, {model.FullName}! 🍕 La tessera è legata a {model.Email}: ora l'indirizzo risulta già iscritto.";
        AddLog($"Submit valido: {model.Email} iscritta al Pizza Club");
        isSubmitting = false;
        var message = registeredMessage;
        CreateEditContext();
        registeredMessage = message;
    }

    private void AddLog(string text)
    {
        log.Insert(0, (DateTime.Now, text));
        if (log.Count > 30)
        {
            log.RemoveAt(log.Count - 1);
        }
    }

    public void Dispose()
    {
        editContext.OnFieldChanged -= OnFieldChanged;
        editContext.OnValidationStateChanged -= OnValidationStateChanged;
    }
}
