using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Mongo2Go;
using MongoDB.Driver;
using Microsoft.Extensions.Logging.Abstractions;
using TeamConnect.Api.Shared.Services;

namespace TeamConnect.Api.Tests.Fixtures;

public class MongoFixture : IDisposable
{
    private readonly MongoDbRunner _runner;
    public MongoDbContext Context { get; }

    public MongoFixture()
    {
        _runner = MongoDbRunner.Start();

        var inMemorySettings = new Dictionary<string, string>
        {
            { "MongoDb:ConnectionString", _runner.ConnectionString },
            { "MongoDb:DatabaseName", "TeamConnect_TestDb" }
        };

        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        // Use a no-op logger in tests to satisfy the constructor without extra dependencies
        var logger = NullLogger<MongoDbContext>.Instance;
        Context = new MongoDbContext(config, logger);
    }

    public void Dispose()
    {
        // Drop DB and dispose runner
        try
        {
            var client = new MongoClient(_runner.ConnectionString);
            client.DropDatabase("TeamConnect_TestDb");
        }
        catch { }

        _runner.Dispose();
    }
}
