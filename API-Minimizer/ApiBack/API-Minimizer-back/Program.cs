using MinimizerCommon.Commons;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(c => { c.EnableAnnotations(); });

// Register custom services
builder.Services.AddSingleton<IDbContext, SimpleDbContext>();
builder.Services.AddSingleton<IBudgetService, SimpleBudgetService>();
builder.Services.AddSingleton<INotificationService, SimpleNotificationService>();
builder.Services.AddSingleton<IAccountService, SimpleAccountService>();

// Register refactored services
builder.Services.AddSingleton<IHealthService, HealthService>();
builder.Services.AddSingleton<IApiKeyValidationService, ApiKeyValidationService>();
builder.Services.AddSingleton<IResponseFormattingService, ResponseFormattingService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
