using FGP.Server;

var builder = WebApplication.CreateBuilder(args);

// --- SERVICES (Hiring Staff) ---
builder.Services.AddControllers();
builder.Services.AddSingleton<GitService>(); // <--- This will have a RED line. That is normal!
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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