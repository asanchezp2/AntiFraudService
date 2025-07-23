using AntiFraudService.Appplication.Commands.UpdateTransaction;
using AntiFraudService.Domain.Interfaces;
using Application.Commands.CreateTransaction;
using Confluent.Kafka;
using Infrastructure.DependencyInjection;
using Infrastructure.Messaging;
using Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var kafkaSection = builder.Configuration.GetSection("Kafka");
var bootstrapServers = kafkaSection.GetValue<string>("BootstrapServers");
var topic = kafkaSection.GetValue<string>("Topic");


// Load environment variables (useful for Docker and local development)
builder.Configuration.AddEnvironmentVariables();

// Register services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register MediatR handlers
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreateTransactionCommand).Assembly));

// Register infrastructure services (EF Core, Kafka, Repositories, etc.)
builder.Services.AddInfrastructureServices(builder.Configuration);

// Kafka Producer
builder.Services.AddScoped<ITransactionProducer>(sp =>
{
    var config = new ProducerConfig { BootstrapServers = bootstrapServers };
    return new TransactionProducer(config, topic);
});

// Kafka Consumer
builder.Services.AddScoped<ITransactionConsumer>(sp =>
{
    var config = new ConsumerConfig
    {
        BootstrapServers = bootstrapServers,
        GroupId = "antifraud-consumer-group",
        AutoOffsetReset = AutoOffsetReset.Earliest
    };
    var logger = sp.GetRequiredService<ILogger<TransactionConsumer>>();
    return new TransactionConsumer(config, topic, logger);
});

// UpdateTransactionHandler
builder.Services.AddScoped<UpdateTransactionHandler>();

builder.Services.AddHostedService<TransactionStatusWorker>();

// Build the app
var app = builder.Build();

// Automatically apply EF Core migrations on startup
// This is useful for development and Docker environments
// You can disable it by setting the environment variable APPLY_MIGRATIONS=false
var applyMigrations = builder.Configuration.GetValue<bool>("APPLY_MIGRATIONS");
if (applyMigrations)
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
        db.Database.Migrate(); // Applies any pending migrations
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
