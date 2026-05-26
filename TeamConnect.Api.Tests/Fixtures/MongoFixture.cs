using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Mongo2Go;
using MongoDB.Driver;
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

        Context = new MongoDbContext(config);
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
