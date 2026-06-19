using ExperimentLab.Data;
using ExperimentLab.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5080");

// --- Register services ---

// EF Core using SQLite; the DB file lives next to the app as experimentlab.db
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")
                      ?? "Data Source=experimentlab.db"));

builder.Services.AddSingleton<AssignmentService>();
builder.Services.AddSingleton<StatsService>();
builder.Services.AddSingleton<DecisionService>();

builder.Services.AddControllers();

// Swagger / OpenAPI — gives you an interactive API page in the browser
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- Apply EF Core migrations on startup ---
// Migrate() builds the schema if the database doesn't exist, and safely
// applies any new migrations on top of an existing one — unlike
// EnsureCreated(), it never wipes data when the model changes.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// --- HTTP pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();   // browse to /swagger
}

app.UseDefaultFiles();   // serves wwwroot/index.html at "/"
app.UseStaticFiles();    // serves the rest of wwwroot

app.MapControllers();

app.Run();