using Elasticsearch.Net;
using FleetPrideApi.Modals;
using FleetPrideApi.Services;
using FleetPrideApi.Settings;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Nest;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<ElasticSearchSettings>(builder.Configuration.GetSection("ElasticSearch"))
    .AddSingleton<IElasticClient>(provider => {
        var settings = provider.GetRequiredService<IOptions<ElasticSearchSettings>>().Value;
        var connectionSettings = new ConnectionSettings(GetConnectionPool(settings))
                        .ServerCertificateValidationCallback(CertificateValidations.AllowAll);
        if (!String.IsNullOrEmpty(settings.UserName) && !String.IsNullOrEmpty(settings.Password))
        {
            connectionSettings.BasicAuthentication(settings.UserName, settings.Password);
        }

        return new Nest.ElasticClient(connectionSettings);
    });
builder.Services.AddTransient<IElasticSearchService<Part>, ElasticSearchService<Part>>();
builder.Services.AddMediatR(typeof(Program));

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

static IConnectionPool GetConnectionPool(ElasticSearchSettings settings)
{
    if (settings.IndexingNodes.Count == 1)
    {
        return new SingleNodeConnectionPool(new Uri(settings.IndexingNodes.First()));
    }

    return new SniffingConnectionPool(settings.IndexingNodes.Select(n => new Uri(n)));
}

