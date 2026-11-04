using Chunkora.Agent.Configuration;
using Chunkora.Agent.Services;
using Chunkora.Agent.Utils;
using Chunkora.Agent.Workers;
using Grpc.Net.Client;

DotNetEnv.Env.NoClobber().TraversePath().Load();

var builder = Host.CreateApplicationBuilder(args);

using var channel = GrpcChannel.ForAddress(builder.Configuration["Agent:ServerAddress"] ?? "http://localhost:50051");
var statusClient = new Chunkora.Contracts.Agent.V1.StatusService.StatusServiceClient(channel);
var instanceClient = new Chunkora.Contracts.Agent.V1.InstanceService.InstanceServiceClient(channel);
var instanceOperationClient = new Chunkora.Contracts.Agent.V1.InstanceOperationService.InstanceOperationServiceClient(channel);
var instanceIoClient = new Chunkora.Contracts.Agent.V1.InstanceIoService.InstanceIoServiceClient(channel);

builder.Services.AddSingleton(statusClient);
builder.Services.AddSingleton(instanceClient);
builder.Services.AddSingleton(instanceOperationClient);
builder.Services.AddSingleton(instanceIoClient);

builder.Services.Configure<AgentOptions>(builder.Configuration.GetSection(AgentOptions.SectionName));

builder.Services.AddSingleton<StatusService>();
builder.Services.AddSingleton<InstanceService>();
builder.Services.AddSingleton<InstanceOperationService>();
builder.Services.AddSingleton<InstanceIoService>();

builder.Services.AddHostedService<StatusWorker>();
builder.Services.AddHostedService<InstanceWorker>();
builder.Services.AddHostedService<InstanceOperationWorker>();
builder.Services.AddHostedService<InstanceIoWorker>();

var host = builder.Build();

Log.Initialize(host.Services.GetRequiredService<ILoggerFactory>());

host.Run();