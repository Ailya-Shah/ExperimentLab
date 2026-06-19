using ExperimentLab.Data;
using Microsoft.EntityFrameworkCore;
using ExperimentLab.Services;          // top of the file, with the other usings

var builder = WebApplication.CreateBuilder(args);

// --- Register services ---

// EF Core using SQLite; the DB file lives next to the app as experimentlab.db
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")
                      ?? "Data Source=experimentlab.db"));

builder.Services.AddControllers();

// Swagger / OpenAPI — gives you an interactive API page in the browser
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<AssignmentService>();
builder.Services.AddSingleton<StatsService>();
builder.Services.AddSingleton<DecisionService>();

var app = builder.Build();

// --- Create the database & tables automatically on startup ---
// For Phase 1 this is simplest. (Later you'd switch to EF migrations.)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// --- HTTP pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();   // browse to /swagger
}

app.MapControllers();
app.UseDefaultFiles();   // serves wwwroot/index.html at "/"
app.UseStaticFiles();    // serves the rest of wwwroot

app.Run();
