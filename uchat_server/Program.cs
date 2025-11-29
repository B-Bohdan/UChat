using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Net;
using System.Text.Json.Serialization;
using uchat.Models;
using uchat_server.Hubs;

// Перевірка аргументів, і usage
if (args.Length == 0)
{
    Console.WriteLine("usage: ./uchat_server [port]");
    return; // Завершаємо, якщо не надано порт
}

if (!int.TryParse(args[0], out int port))
{
    Console.WriteLine("Error: Port must be an integer.");
    return;
}

// Вивід PID
int pid = Process.GetCurrentProcess().Id;
Console.WriteLine($"Server started with PID: {pid}");

var builder = WebApplication.CreateBuilder(args);

// Отримаємо IP-адресу хосту з конфігурації, абло ставимо за замовчуванням
string ipString = builder.Configuration["ServerSettings:HostAddress"] ?? "127.0.0.1";

if (!IPAddress.TryParse(ipString, out IPAddress? ipAddress))
{
    Console.WriteLine($"Error: Invalid IP address in configuration: {ipString}");
    return;
}

Console.WriteLine($"Configured to listen on: {ipAddress}:{port}");

builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(ipAddress, port);
});

// Налаштування сервісів
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationContext>(options => options.UseSqlite(connectionString));
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        // Ігноруємо циклічні посилання під час серіалізації JSON
        // Користувач зберігає чати -> а чат користувачів -> користувач зберігає чати і так далі...
        options.PayloadSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

var app = builder.Build();

//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();
app.UseAuthorization();

// Мапінг хаба SignalR
app.MapHub<ChatHub>("/chatHub");

// app.Run() запускає сервер як daemon, тобто який працює нескінченно та не закриється сам
app.Run();