using System;
using System.Collections.Generic;
using System.Text;
using Qdrant.Client;
using Testcontainers.Qdrant;


namespace DocMind.Tests.Infrastructure
{
    public class QdrantFixture : IAsyncLifetime
    {
        private readonly QdrantContainer _container = new QdrantBuilder()
            .WithImage("qdrant/qdrant:latest")
            .Build();

        public QdrantClient CreateClient()
        {
            return new QdrantClient(new Uri(_container.GetGrpcConnectionString()));
        }

        public async Task InitializeAsync() => await _container.StartAsync();

        public async Task DisposeAsync() => await _container.DisposeAsync();
    }
}
