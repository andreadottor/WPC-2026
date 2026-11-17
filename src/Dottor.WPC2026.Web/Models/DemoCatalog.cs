namespace Dottor.WPC2026.Web.Models;

public sealed record DemoInfo(int? Number, string Chapter, string Url, string Title, string Summary, string Feature, string RenderMode);

/// <summary>Elenco delle demo nell'ordine dei capitoli della sessione, usato da menu e home page.</summary>
public static class DemoCatalog
{
    public static IReadOnlyList<DemoInfo> All { get; } =
    [
        new(1, "01 · Circuit", "demo/circuit-pause", "Il forno si riavvia, il carrello resta",
            "Nuova versione in produzione mentre i clienti ordinano: il circuito va in pausa e carrello e ordine tornano al loro posto. Pausa anche dal browser e con AutoPause.",
            "Pausa/ripresa dei circuiti + [PersistentState] + AutoPause", "Interactive Server"),
        new(2, "02 · Form e Static SSR", "demo/async-validation", "Validazione asincrona",
            "Iscrizione al Pizza Club: email univoca verificata sul server con stato \"in corso\", annullamento ed errori.",
            "AsyncValidationAttribute + EditContext.IsValidationPending", "Interactive Server"),
        new(3, "02 · Form e Static SSR", "demo/static-ssr-validation", "Validazione client, TempData e Session",
            "Il form d'ordine senza circuito né WebAssembly: valida nel browser prima del POST e porta la conferma oltre il redirect.",
            "Validazione client-side per Static SSR + [SupplyParameterFromTempData] + [SupplyParameterFromSession]", "Static SSR"),
        new(4, "02 · Form e Static SSR", "demo/cacheview", "CacheView",
            "Il menu con le vendite del giorno sfornato una volta e riusato: VaryByQuery, scadenza e CacheKey.",
            "<CacheView>: cache dell'output di un sotto-albero in Static SSR", "Static SSR"),
        new(5, "03 · WebAssembly", "demo/web-worker", "Web Worker",
            "Centinaia di migliaia di ordini analizzati fuori dal thread della UI: qual è la pizza più amata?",
            "Template blazorwebworker + [JSExport]", "Interactive WebAssembly"),
        new(6, "03 · WebAssembly", "demo/hosted-service", "IHostedService nel browser",
            "Il monitor della cucina: un BackgroundService nel browser che controlla il forno e sopravvive alla navigazione.",
            "Supporto a IHostedService in Blazor WebAssembly", "Interactive WebAssembly"),
        new(7, "04 · Componenti", "demo/virtualize", "Virtualize evoluto",
            "Il diario della pizzeria con 10.000 eventi di altezza variabile e il tracking dei rider ancorato in fondo.",
            "ScrollToItemAsync, AnchorMode, altezze variabili", "Interactive Server"),
        new(8, "04 · Componenti", "demo/quickgrid", "QuickGrid con stato nell'URL",
            "Gli ordini con pagina e ordinamento nell'indirizzo: link condivisibili e tasto Indietro funzionante.",
            "Stato nella query string + OnRowClick", "Interactive Server"),
        new(9, "05 · Agentic UI", "demo/chat", "Chat minimale",
            "Il pizzaiolo virtuale: una chat completa con streaming e cronologia in poche righe (IChatClient + ChatPage).",
            "Microsoft.AspNetCore.Components.AI (preview) + Microsoft.Extensions.AI", "Interactive Server"),
        new(10, "05 · Agentic UI", "demo/agentic-ui", "Agentic UI",
            "Un assistente progetta la pizza del mese: anteprima, conferma, approvazione prima di metterla nel menu e tool.",
            "Microsoft.AspNetCore.Components.AI (preview)", "Interactive Server"),
        new(null, "Extra", "demo/other-features", "Altre novità",
            "DisplayName, EnvironmentView, BrowserOptions, Antiforgery, navigazione relativa e altro.",
            "Panoramica rapida", "Static SSR"),
    ];

    public static IEnumerable<IGrouping<string, DemoInfo>> ByChapter => All.GroupBy(d => d.Chapter);
}