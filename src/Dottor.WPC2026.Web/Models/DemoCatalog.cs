namespace Dottor.WPC2026.Web.Models;

public sealed record DemoInfo(int? Number, string Chapter, string Url, string Title, string Summary, string Feature, string RenderMode);

/// <summary>Elenco delle demo nell'ordine dei capitoli della sessione, usato da menu e home page.</summary>
public static class DemoCatalog
{
    public static IReadOnlyList<DemoInfo> All { get; } =
    [
        new(1, "01 · Circuit", "demo/circuit-pause", "Rilascio senza perdere lo stato",
            "Il server si riavvia con una nuova versione, il circuito viene messo in pausa e lo stato torna al suo posto. Pausa anche dal browser e con AutoPause.",
            "Pausa/ripresa dei circuiti + [PersistentState] + AutoPause", "Interactive Server"),
        new(2, "02 · Form e Static SSR", "demo/async-validation", "Validazione asincrona",
            "Email univoca verificata sul server con stato \"in corso\", annullamento ed errori.",
            "AsyncValidationAttribute + EditContext.IsValidationPending", "Interactive Server"),
        new(3, "02 · Form e Static SSR", "demo/static-ssr-validation", "Validazione client, TempData e Session",
            "Un form senza circuito né WebAssembly che valida nel browser prima del POST e porta i dati oltre il redirect.",
            "Validazione client-side per Static SSR + [SupplyParameterFromTempData] + [SupplyParameterFromSession]", "Static SSR"),
        new(4, "02 · Form e Static SSR", "demo/cacheview", "CacheView",
            "Un report costoso renderizzato una volta e riusato: VaryByQuery, scadenza e CacheKey.",
            "<CacheView>: cache dell'output di un sotto-albero in Static SSR", "Static SSR"),
        new(5, "03 · WebAssembly", "demo/web-worker", "Web Worker",
            "L'analisi di un CSV da centinaia di migliaia di righe spostata fuori dal thread della UI.",
            "Template blazorwebworker + [JSExport]", "Interactive WebAssembly"),
        new(6, "03 · WebAssembly", "demo/hosted-service", "IHostedService nel browser",
            "Un BackgroundService che aggiorna le statistiche dei ticket e sopravvive alla navigazione.",
            "Supporto a IHostedService in Blazor WebAssembly", "Interactive WebAssembly"),
        new(7, "04 · Componenti", "demo/virtualize", "Virtualize evoluto",
            "Timeline di 10.000 eventi con altezze variabili, scroll a un elemento e ancoraggio in fondo.",
            "ScrollToItemAsync, AnchorMode, altezze variabili", "Interactive Server"),
        new(8, "04 · Componenti", "demo/quickgrid", "QuickGrid con stato nell'URL",
            "Ordinamento e pagina finiscono nell'indirizzo: link condivisibili e tasto Indietro funzionante.",
            "Stato nella query string + OnRowClick", "Interactive Server"),
        new(9, "05 · Agentic UI", "demo/agentic-ui", "Agentic UI",
            "Un assistente propone modifiche a un documento: anteprima, conferma, approvazione e tool.",
            "Microsoft.AspNetCore.Components.AI (preview)", "Interactive Server"),
        new(null, "Extra", "demo/other-features", "Altre novità",
            "DisplayName, EnvironmentView, BrowserOptions, Antiforgery, navigazione relativa e altro.",
            "Panoramica rapida", "Static SSR"),
    ];

    public static IEnumerable<IGrouping<string, DemoInfo>> ByChapter => All.GroupBy(d => d.Chapter);
}