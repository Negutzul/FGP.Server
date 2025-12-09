using FGP.Server;
using FGP.Server.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- SERVICES (Hiring Staff) ---
builder.Services.AddControllers();
builder.Services.AddSingleton<GitService>(); // <--- This will have a RED line. That is normal!
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=app.db"));
var app = builder.Build();

// --- PIPELINE (Instructions) ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();