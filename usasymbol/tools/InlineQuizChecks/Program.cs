using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using usasymbol.Controllers;
using usasymbol.Models;
using usasymbol.Services;
using USASymbol.Data;
using USASymbol.Options;
using USASymbol.Services;
using USASymbol.Services.Images;
using USASymbol.Services.Interface;
using USASymbol.Views.Shared.Components;

var directory = new DirectoryInfo(AppContext.BaseDirectory);
while (directory != null && !File.Exists(Path.Combine(directory.FullName, "usasymbol.csproj"))) directory = directory.Parent;
var root = directory?.FullName ?? throw new InvalidOperationException("Checkout not found.");
var output = Path.Combine(root, "artifacts", "inline-quiz-checks");
Directory.CreateDirectory(output);
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    ApplicationName = typeof(QuizService).Assembly.GetName().Name, ContentRootPath = root,
    WebRootPath = Path.Combine(root, "wwwroot"), EnvironmentName = "Development"
});
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Services.AddControllersWithViews().AddApplicationPart(typeof(QuizService).Assembly);
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<StateSymbolQuizBuilder>();
builder.Services.AddSingleton<QuizService>();
builder.Services.AddScoped<IBorderService, BorderService>();
builder.Services.AddSingleton<IImagePathNormalizer, ImagePathNormalizer>();
builder.Services.AddSingleton(Options.Create(new BunnyOptions()));
builder.Services.AddSingleton<IImageUrlService, ImageUrlService>();
await using var app = builder.Build();
var quizService = app.Services.GetRequiredService<QuizService>();
var quiz = quizService.GetBySlug("arizona-state-symbols-quiz")!;
var assertions = 0;
void Check(bool condition, string reason) { if (!condition) throw new InvalidOperationException(reason); assertions++; }
Check(quiz != null && quiz.Questions.Count >= 10, "Arizona pool exists.");
var dinosaur = quiz!.Questions.First(q => q.Question == "What is the official state dinosaur of Arizona?");
Check(dinosaur.CorrectAnswer == "Sonorasaurus" && dinosaur.Explanation.Contains("2018"), "Answer and adoption fact come from the listing.");
Check(dinosaur.AnswerImageUrl.EndsWith("sonorasaurus.webp") && dinosaur.ImageUrl.Length == 0, "The answer thumbnail is not revealed before a text answer.");
Check(dinosaur.RelatedAnswers.Count == 3 && dinosaur.RelatedAnswers.All(link => link.Url.StartsWith("/states/")), "Distractors link to real state symbol pages.");
Check(quiz.Questions.Count(q => q.ImageUrl.Length > 0) >= 3, "The pool supplies image-question variants.");
Check(quiz.Questions.All(q => q.LinkUrl.Length > 0 && q.FamilyKey.Length > 0), "Every generated question has a topic and content link.");
foreach (var state in StateSymbolQuizBuilder.AllStateNames)
{
    var stateQuiz = quizService.GetBySlug(StateSymbolQuizBuilder.SlugFor(state));
    Check(stateQuiz != null && stateQuiz.Questions.Select(q => q.FamilyKey).Distinct().Count() >= 5, state + " has five independent question topics.");
}

// Exercise the actual controller against an isolated in-memory database.
await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();
await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
await db.Database.EnsureCreatedAsync();
var controller = new QuizController(quizService, db, app.Services.GetRequiredService<IMemoryCache>(), new QuizShareImageService())
{
    ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
};
var questions = quiz.Questions.GroupBy(q => q.FamilyKey).Select(group => group.First()).Take(5).ToList();
InlineQuizResultDto AnswerSet(bool correct) => new()
{
    Answers = questions.Select(q => new InlineQuizAnswerDto { Question = q.Question, Choice = correct ? q.CorrectAnswer : q.Choices.First(choice => choice != q.CorrectAnswer) }).ToList()
};
JsonElement JsonResult(IActionResult result) => JsonSerializer.SerializeToElement(((OkObjectResult)result).Value);
var initial = JsonResult(await controller.SaveInlineResult(quiz.Slug, AnswerSet(true)));
Check(initial.GetProperty("percentile").ValueKind == JsonValueKind.Null && initial.GetProperty("hardestQuestion").ValueKind == JsonValueKind.Null,
    "No community figures are invented for an empty database.");
Check((await db.QuizAttempts.SingleAsync()).Score == 100, "Server scores a completed round from the answer key.");
Check(await controller.SaveResult(quiz.Slug, new QuizResultDto { Mode = "inline", Score = 100, QuestionCount = 5 }) is BadRequestResult,
    "The legacy score endpoint cannot bypass inline answer validation.");
var invalid = AnswerSet(true); invalid.Answers[0].Choice = "not a real option";
Check(await controller.SaveInlineResult(quiz.Slug, invalid) is BadRequestResult, "Unknown answer choices are rejected.");
var duplicate = AnswerSet(true); duplicate.Answers[1] = duplicate.Answers[0];
Check(await controller.SaveInlineResult(quiz.Slug, duplicate) is BadRequestResult, "Repeated questions cannot manufacture a five-question result.");
var shortRound = AnswerSet(true); shortRound.Answers.RemoveAt(0);
Check(await controller.SaveInlineResult(quiz.Slug, shortRound) is BadRequestResult, "Incomplete results are rejected.");
await controller.SaveEvents(quiz.Slug, new QuizEventsDto { Mode = "inline", Events = new()
{
    new() { T = "answer", Q = dinosaur.Question, P = dinosaur.CorrectAnswer, C = false },
    new() { T = "answer", Q = dinosaur.Question, P = "invented", C = true },
    new() { T = "result_click", Q = "/quizzes/state-birds-quiz" },
    new() { T = "result_click", Q = "https://example.com" }
} });
Check(await db.QuizEvents.CountAsync() == 2 && (await db.QuizEvents.SingleAsync(e => e.EventType == "answer")).IsCorrect == true,
    "Answer correctness is verified server-side; invalid clicks and choices are ignored.");
for (var i = 0; i < 20; i++)
{
    db.QuizAttempts.Add(new QuizAttempt { QuizSlug = quiz.Slug, Mode = "inline", Score = 40, QuestionCount = 5, CreatedAt = DateTime.UtcNow });
    db.QuizEvents.Add(new QuizEvent { QuizSlug = quiz.Slug, Mode = "inline", EventType = "answer", QuestionKey = questions[0].Question,
        IsCorrect = i < 5, CreatedAt = DateTime.UtcNow });
}
await db.SaveChangesAsync();
var measured = JsonResult(await controller.SaveInlineResult(quiz.Slug, AnswerSet(true)));
Check(measured.GetProperty("rounds").GetInt32() == 21 && measured.GetProperty("percentile").GetInt32() == 95, "Percentiles use prior comparable five-question rounds.");
Check(measured.GetProperty("correctPercent").GetInt32() == 25, "Question difficulty uses real answer counts after the minimum sample.");
Check(controller.ShareImage(quiz.Slug, 4, 5, "inline", "01111") is FileContentResult png && png.FileContents.Length > 1000, "An inline score has an OG image.");

// Render the real component, with the neighboring states selected by the real border service.
using var scope = app.Services.CreateScope();
var component = new InlineQuizViewComponent(quizService, scope.ServiceProvider.GetRequiredService<IBorderService>());
var componentView = (Microsoft.AspNetCore.Mvc.ViewComponents.ViewViewComponentResult)await component.InvokeAsync("state", "Arizona");
var model = componentView.ViewData?.Model as InlineQuizViewModel ?? throw new InvalidOperationException("Component model missing.");
Check(model.Neighbors.Count == 3 && model.Neighbors.All(neighbor => neighbor.Slug != quiz.Slug), "The result navigation includes real neighboring-state quizzes.");
Check(model.FullQuizLabel == "Full quiz · 10 questions", "The full-quiz label describes the actual round length.");
var http = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
http.Request.Scheme = "http"; http.Request.Host = new HostString("localhost");
var action = new ActionContext(http, new Microsoft.AspNetCore.Routing.RouteData(), new ActionDescriptor());
var view = scope.ServiceProvider.GetRequiredService<ICompositeViewEngine>().GetView(null, "/Views/Shared/Components/InlineQuiz/Default.cshtml", false).View
    ?? throw new InvalidOperationException("Quiz view not found.");
using var writer = new StringWriter();
await view.RenderAsync(new ViewContext(action, view,
    new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) { Model = model },
    new TempDataDictionary(http, scope.ServiceProvider.GetRequiredService<ITempDataProvider>()), writer, new HtmlHelperOptions()));
var markup = writer.ToString();
Check(!markup.Contains("Score <") && !markup.Contains("shuffled every time") && !markup.Contains("from-amber"), "The widget has no score-zero, implementation copy, or yellow header.");
var html = "<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">"
    + "<link rel=\"stylesheet\" href=\"/css/main.css\"><link rel=\"stylesheet\" href=\"/css/inline-quiz.css\">"
    + "<style>body{display:block;padding:16px;background:#f8fafb}main{width:100%;max-width:720px;margin:auto;min-width:0}</style><main>" + markup + "</main></html>";
await File.WriteAllTextAsync(Path.Combine(output, "quiz.html"), html);
var browserEvents = new ConcurrentBag<QuizEventItemDto>();
app.UseStaticFiles();
app.MapGet("/fixture", () => Results.Content(html, "text/html"));
app.MapPost("/quizzes/{slug}/events", (QuizEventsDto events) => { foreach (var item in events.Events ?? new()) browserEvents.Add(item); return Results.NoContent(); });
app.MapPost("/quizzes/{slug}/inline-result", () => Results.NoContent());
await app.StartAsync();

if (args.Contains("--browser"))
{
    var browserProfile = Path.Combine(root, "tmp", "inline-quiz-browser-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(browserProfile);
    var start = new ProcessStartInfo("C:/Program Files/Google/Chrome/Application/chrome.exe")
    { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, RedirectStandardError = true, RedirectStandardOutput = true };
    foreach (var argument in new[] { "--headless=new", "--no-sandbox", "--in-process-gpu", "--disable-gpu", "--disable-software-rasterizer",
        "--disable-background-networking", "--no-first-run", "--remote-debugging-port=0", "--user-data-dir=" + browserProfile, "about:blank" }) start.ArgumentList.Add(argument);
    using var browser = Process.Start(start)!;
    _ = browser.StandardError.ReadToEndAsync(); _ = browser.StandardOutput.ReadToEndAsync();
    try
    {
        var portFile = Path.Combine(browserProfile, "DevToolsActivePort");
        for (var attempt = 0; !File.Exists(portFile) && attempt < 100; attempt++) await Task.Delay(100);
        var port = (await File.ReadAllLinesAsync(portFile))[0];
        using var client = new HttpClient();
        var version = JsonDocument.Parse(await client.GetStringAsync("http://127.0.0.1:" + port + "/json/version"));
        using var socket = new ClientWebSocket();
        await socket.ConnectAsync(new Uri(version.RootElement.GetProperty("webSocketDebuggerUrl").GetString()!), CancellationToken.None);
        var commandId = 0;
        async Task<JsonElement> Command(string method, object? parameters = null, string? session = null)
        {
            var id = ++commandId;
            var payload = new Dictionary<string, object?> { ["id"] = id, ["method"] = method, ["params"] = parameters ?? new { } };
            if (session != null) payload["sessionId"] = session;
            var message = JsonSerializer.SerializeToUtf8Bytes(payload);
            await socket.SendAsync(message, WebSocketMessageType.Text, true, CancellationToken.None);
            while (true)
            {
                using var stream = new MemoryStream(); var buffer = new byte[65536]; WebSocketReceiveResult received;
                do { received = await socket.ReceiveAsync(buffer, CancellationToken.None); stream.Write(buffer, 0, received.Count); } while (!received.EndOfMessage);
                using var document = JsonDocument.Parse(stream.ToArray()); var response = document.RootElement;
                if (!response.TryGetProperty("id", out var responseId) || responseId.GetInt32() != id) continue;
                if (response.TryGetProperty("error", out var error)) throw new InvalidOperationException(error.ToString());
                return response.GetProperty("result").Clone();
            }
        }
        var target = (await Command("Target.createTarget", new { url = "about:blank" })).GetProperty("targetId").GetString();
        var session = (await Command("Target.attachToTarget", new { targetId = target, flatten = true })).GetProperty("sessionId").GetString();
        async Task<JsonElement> Evaluate(string expression)
        {
            var evaluated = await Command("Runtime.evaluate", new { expression, returnByValue = true, awaitPromise = true, userGesture = true }, session);
            if (evaluated.TryGetProperty("exceptionDetails", out var error)) throw new InvalidOperationException(error.ToString());
            return evaluated.GetProperty("result").TryGetProperty("value", out var value) ? value.Clone() : JsonSerializer.SerializeToElement<object?>(null);
        }
        await Command("Emulation.setDeviceMetricsOverride", new { width = 375, height = 1100, deviceScaleFactor = 1, mobile = true }, session);
        await Command("Page.navigate", new { url = app.Urls.Single() + "/fixture" }, session);
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if ((await Evaluate("!!document.querySelector('.iq-choice')")).GetBoolean()) break;
            await Task.Delay(100);
        }
        Check((await Evaluate("document.querySelectorAll('.iq-choice').length===4")).GetBoolean(), "The first question initializes in the real browser.");
        Check((await Evaluate("document.querySelector('[data-iq=more]').hidden && !document.querySelector('.iq-card').contains(document.querySelector('[data-iq=more]'))")).GetBoolean(), "Navigation is hidden during play and lives outside the card.");
        Check((await Evaluate("[...document.querySelectorAll('.iq-choice')].every(button=>button.getBoundingClientRect().height>=48)")).GetBoolean(), "Mobile answer targets are at least 48px high.");
        Check((await Evaluate("getComputedStyle(document.querySelector('.iq-answers')).gridTemplateColumns.split(' ').length===1")).GetBoolean(), "Mobile answers use one column.");
        Check((await Evaluate("document.documentElement.scrollWidth<=innerWidth")).GetBoolean(), "The quiz fits a real 375px layout viewport.");
        Check((await client.GetAsync(app.Urls.Single() + "/icons/quiz.svg")).IsSuccessStatusCode, "Quiz icons load from the application without a third-party font request.");
        await Evaluate("window.checkData=JSON.parse(document.querySelector('[data-iq=data]').textContent);window.roundQuestions=[];");
        await File.WriteAllBytesAsync(Path.Combine(output, "question-phone.png"), Convert.FromBase64String((await Command("Page.captureScreenshot", new { format = "png", captureBeyondViewport = true }, session)).GetProperty("data").GetString()!));
        for (var i = 0; i < 5; i++)
        {
            var current = (await Evaluate("document.querySelector('[data-iq=question]').textContent")).GetString()!;
            var q = quiz.Questions.Single(q => q.Question == current);
            var picked = i == 0 ? q.Choices.First(choice => choice != q.CorrectAnswer) : q.CorrectAnswer;
            await Evaluate("window.roundQuestions.push(" + JsonSerializer.Serialize(current) + ");var b=[...document.querySelectorAll('.iq-choice')].find(b=>b.dataset.choice===" + JsonSerializer.Serialize(picked) + ");b.focus();b.dispatchEvent(new KeyboardEvent('keydown',{key:String([...b.parentElement.children].indexOf(b)+1),bubbles:true}));");
            Check((await Evaluate("document.querySelectorAll('.iq-choice:disabled').length===4 && !document.querySelector('[data-iq=next]').hidden")).GetBoolean(), "Keyboard answer locks the choices and exposes an explicit Next button.");
            Check((await Evaluate("document.querySelector('[data-iq=question]').textContent===" + JsonSerializer.Serialize(current))).GetBoolean(), "Answers never advance automatically.");
            Check((await Evaluate("!!document.querySelector('[data-iq=feedback] a')")).GetBoolean(), "Each answer exposes a content link.");
            if (i == 0) await File.WriteAllBytesAsync(Path.Combine(output, "feedback-phone.png"), Convert.FromBase64String((await Command("Page.captureScreenshot", new { format = "png", captureBeyondViewport = true }, session)).GetProperty("data").GetString()!));
            await Evaluate("document.querySelector('[data-iq=next]').click()");
        }
        Check((await Evaluate("document.querySelector('[data-iq=final]').textContent==='4' && !document.querySelector('[data-iq=more]').hidden")).GetBoolean(), "The completed round shows the score and navigation.");
        Check((await Evaluate("document.querySelectorAll('[data-iq=review-list]>li').length===5 && document.querySelectorAll('[data-iq=review-list] a').length===5")).GetBoolean(), "The review contains all five answers and five content links.");
        Check((await Evaluate("document.querySelector('[data-iq=community]').hidden")).GetBoolean(), "Missing statistics do not produce a fabricated comparison.");
        Check((await Evaluate("JSON.parse(localStorage.getItem('usas.quiz.inline.best'))[checkData.slug]===4 && document.querySelectorAll('[data-iq=collection-grid] a').length===50")).GetBoolean(), "Best score and the fifty-state collection persist.");
        Check((await Evaluate("new Set(roundQuestions.map(text=>checkData.pool.find(q=>q.q===text).family)).size===5")).GetBoolean(), "A round does not repeat the same symbol topic.");
        Check((await Evaluate("roundQuestions.filter(text=>checkData.pool.find(q=>q.q===text).image).length===2")).GetBoolean(), "The round mixes two photo and three text questions.");
        await Evaluate("Object.defineProperty(navigator,'share',{value:undefined,configurable:true});Object.defineProperty(navigator,'clipboard',{value:{writeText:async text=>{window.copiedShare=text}},configurable:true});document.querySelector('[data-iq=share]').click()");
        await Task.Delay(100);
        Check((await Evaluate("copiedShare.includes('4/5') && copiedShare.includes('s=4&n=5&m=inline')")).GetBoolean(), "Share includes the real score and an OG-enabled quiz URL.");
        await Evaluate("document.querySelector('[data-iq=review]').open=true");
        await File.WriteAllBytesAsync(Path.Combine(output, "result-phone.png"), Convert.FromBase64String((await Command("Page.captureScreenshot", new { format = "png", captureBeyondViewport = true }, session)).GetProperty("data").GetString()!));
        await Command("Emulation.setDeviceMetricsOverride", new { width = 1280, height = 1100, deviceScaleFactor = 1, mobile = false }, session);
        await File.WriteAllBytesAsync(Path.Combine(output, "result-desktop.png"), Convert.FromBase64String((await Command("Page.captureScreenshot", new { format = "png", captureBeyondViewport = true }, session)).GetProperty("data").GetString()!));
        await Evaluate("document.querySelector('[data-iq=again]').click()");
        Check((await Evaluate("document.querySelector('[data-iq=result]').hidden && document.querySelector('[data-iq=more]').hidden")).GetBoolean(), "Replay clears the result and hides result navigation.");
        await Task.Delay(250);
        Check(browserEvents.Count(item => item.T == "answer") == 5 && browserEvents.Any(item => item.T == "complete") && browserEvents.Any(item => item.T == "result_click"), "Start, answer, complete and replay-click events reach the server.");
        Check((await Evaluate("!roundQuestions.includes(document.querySelector('[data-iq=question]').textContent)")).GetBoolean(), "Replay starts with a new question.");
        for (var i = 0; i < 5; i++)
        {
            await Evaluate("var current=checkData.pool.find(q=>q.q===document.querySelector('[data-iq=question]').textContent);[...document.querySelectorAll('.iq-choice')].find(b=>b.dataset.choice!==current.a).click();document.querySelector('[data-iq=next]').click()");
        }
        Check((await Evaluate("document.querySelector('[data-iq=final]').textContent==='0' && document.querySelector('[data-iq=record]').textContent.includes('4 / 5')")).GetBoolean(), "A weaker replay does not overwrite the personal best.");
        await Command("Browser.close");
    }
    finally { if (!browser.HasExited) browser.Kill(true); }
}
await app.StopAsync();
Console.WriteLine($"Passed {assertions} inline quiz checks. Previews: {output}");
