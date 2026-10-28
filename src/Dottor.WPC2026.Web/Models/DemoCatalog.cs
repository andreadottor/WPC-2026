namespace Dottor.WPC2026.Web.Models;

public sealed record DemoInfo(int? Number, string Url, string Title, string Summary, string Feature, string RenderMode);

/// <summary>Elenco delle demo, usato da menu e home page.</summary>
public static class DemoCatalog
{
    public static IReadOnlyList<DemoInfo> All { get; } =
    [
        new(1, "demo/circuit-pause", "Rilascio senza perdere lo stato",
            "Il server si riavvia con una nuova versione, il circuito viene messo in pausa e lo stato torna al suo posto.",
            "Pausa/ripresa dei circuiti + [PersistentState]", "Interactive Server"),
        new(2, "demo/async-validation", "Validazione asincrona",
            "Email univoca verificata sul server con stato \"in corso\", annullamento ed errori.",
            "AsyncValidationAttribute + EditContext.IsValidationPending", "Interactive Server"),
        new(3, "demo/static-ssr-validation", "Validazione client in SSR statico",
            "Un form senza circuito né WebAssembly che valida nel browser prima del POST.",
            "Validazione client-side per Static SSR + TempData", "Static SSR"),
        new(4, "demo/virtualize", "Virtualize evoluto",
            "Timeline di 10.000 eventi con altezze variabili, scroll a un elemento e ancoraggio in fondo.",
            "ScrollToItemAsync, AnchorMode, altezze variabili", "Interactive Server"),
        new(5, "demo/quickgrid", "QuickGrid con stato nell'URL",
            "Ordinamento e pagina finiscono nell'indirizzo: link condivisibili e tasto Indietro funzionante.",
            "Stato nella query string + OnRowClick", "Interactive Server"),
        new(6, "demo/agentic-ui", "Agentic UI",
            "Un assistente propone modifiche a un documento: anteprima, conferma, approvazione e tool.",
            "Microsoft.AspNetCore.Components.AI (preview)", "Interactive Server"),
        new(null, "demo/other-features", "Altre novità",
            "DisplayName, EnvironmentView, BrowserOptions, TempData, navigazione relativa e altro.",
            "Panoramica rapida", "Static SSR"),
    ];
}
