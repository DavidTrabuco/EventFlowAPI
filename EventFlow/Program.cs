using EventFlow.Application.Services;
using EventFlow.Domain.Interface;
using EventFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Persistência: EF Core com provider InMemory — sem servidor, sem connection string.
// Para ligar no SQL Server depois, troque APENAS esta linha por:
//   options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
// e rode as migrations. Nenhum service ou controller muda.
builder.Services.AddDbContext<EventFlowDbContext>(options =>
    options.UseInMemoryDatabase("EventFlowDb"));

builder.Services.AddScoped<IEventoService, EventoService>();
builder.Services.AddScoped<IParticipanteService, ParticipanteService>();
builder.Services.AddScoped<IIngressoService, IngressoService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
