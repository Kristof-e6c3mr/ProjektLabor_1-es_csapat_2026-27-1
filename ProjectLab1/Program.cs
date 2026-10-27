using ProjectLab1.Components;
using ProjectLab1.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var apiUrl = builder.Configuration["ApiBaseUrl"] ?? "http://webapi:8080/";

// 1. A session kezelőnek SCOPED-nak kell lennie Blazor Serverben, 
// különben minden csatlakozó felhasználó megosztaná ugyanazt a bejelentkezést!
builder.Services.AddScoped<UserSessionService>();

// 2. Közvetlen, biztonságos HttpClient regisztráció (az IHttpClientFactory időtúllépési bugjainak elkerülésére)
builder.Services.AddScoped(sp =>
{
    var session = sp.GetRequiredService<UserSessionService>();

    // Létrehozzuk a saját elkapónkat, és megadjuk neki a gyökér HTTP kezelőt
    var handler = new JwtAuthorizationHandler(session)
    {
        InnerHandler = new HttpClientHandler()
    };

    // Létrehozzuk a klienst, ami pontosan addig él, amíg a felhasználó böngészőablaka nyitva van
    var client = new HttpClient(handler)
    {
        BaseAddress = new Uri(apiUrl)
    };

    return client;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();