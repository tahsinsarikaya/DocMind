using DocMind.Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace DocMind.Infrastructure.Providers.Fake
{
    public class FakeEmbeddingService : IEmbeddingService
    {
        public int Dimensions => 128;

        public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GenerateDeterministicVector(text));
        }

        public Task<IReadOnlyList<float[]>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<float[]> result = texts.Select(GenerateDeterministicVector).ToList();
            return Task.FromResult(result);
        }

        private float[] GenerateDeterministicVector(string text)
        {
            // Gerçek bir embedding değil — ama AYNI metin HER ZAMAN aynı vektörü üretir,
            // ve metnin hash'inden türediği için farklı metinler farklı vektörler üretir.
            // Bu, testlerde "benzer metinler benzer sonuç vermeli" gibi gerçek anlamsal
            // iddialar kuramayacağımız, ama "sistem uçtan uca çalışıyor mu" diye
            // deterministik şekilde doğrulayabileceğimiz bir sahte araç.
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
            var vector = new float[Dimensions];

            for (var i = 0; i < Dimensions; i++)
            {
                vector[i] = hash[i % hash.Length] / 255f;
            }

            return vector;
        }
    }
}
