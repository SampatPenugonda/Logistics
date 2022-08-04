using ExceptionHandling.Middlewares;
using Logistics.API;
using Logistics.API.ChangeStream;
using Logistics.API.ConfigureIndexes;
using Logistics.API.DAL;
using MongoDB.Driver;

IConfiguration configuration = new ConfigurationBuilder()
                            .AddJsonFile("appsettings.Development.json")
                            .Build();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Injection MongoClient
var mongoClient = new MongoClient(configuration["MongoDb-Connection-String"]);
builder.Services.AddSingleton<IMongoClient>(x => { return mongoClient; });
// Services Injection
builder.Services.AddSingleton<ICitiesDal, CitiesDal>();
builder.Services.AddSingleton<IPlanesDal, PlanesDal>();
builder.Services.AddSingleton<ICargoDal,CargoDal>();
builder.Services.AddSingleton<ChangeStream, ChangeStream>();
// Configuration Injection
builder.Services.AddSingleton<IConfiguration>(x => configuration);
var changeStreamServices = builder.Services.BuildServiceProvider().GetService<ChangeStream>();

var app = builder.Build();
// Create Indexes Once after the application host got initialized. 
app.Lifetime.ApplicationStarted.Register(() =>
LogisticsIndex.createIndexes(mongoClient)
); 

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();
app.UseMiddleware<ExceptionHandlingMiddleware>();

new Thread(() => changeStreamServices.Init()).Start();
app.Run();
