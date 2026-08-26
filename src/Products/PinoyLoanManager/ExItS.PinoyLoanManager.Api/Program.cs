using ExItS.PinoyLoanManager.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();
app.MapPlmHealth();
app.Run();
