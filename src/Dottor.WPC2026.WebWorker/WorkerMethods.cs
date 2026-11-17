using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;

namespace Dottor.WPC2026.WebWorker;

// Metodi [JSExport] eseguiti dentro il Web Worker, su un thread separato da quello della UI.
// Si chiamano dal componente con WebWorkerClient.InvokeAsync, usando il nome completo:
// await worker.InvokeAsync<string>("Dottor.WPC2026.WebWorker.WorkerMethods.AnalyzeOrders", [csv]);
// [JSExport] supporta solo tipi "interop-friendly": i dati complessi viaggiano come JSON.

[SupportedOSPlatform("browser")]
public static partial class WorkerMethods
{
    [JSExport]
    public static string Greet(string name) => $"Hello, {name}!";

    [JSExport]
    public static string AnalyzeOrders(string csv) =>
        JsonSerializer.Serialize(OrderCsvAnalyzer.Analyze(csv), WorkerJsonContext.Default.CsvSummary);
}