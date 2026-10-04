using System;
using System.Collections.Generic;
using System.Text;

namespace DocMind.Domain.Services
{
    public class TextChunker
    {
        public IReadOnlyList<string> Chunk(string text, int maxChunkSize = 800, int overlap = 100)
        {
            if (maxChunkSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxChunkSize), "Max chunk size must be positive.");

            if (overlap < 0 || overlap >= maxChunkSize)
                throw new ArgumentOutOfRangeException(nameof(overlap), "Overlap must be non-negative and smaller than the max chunk size.");

            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<string>();

            // Windows satır sonlarını (\r\n) normalize ediyoruz, yoksa paragraf tespiti kaçar.
            var normalized = text.Replace("\r\n", "\n").Trim();

            var chunks = new List<string>();
            var start = 0;

            while (start < normalized.Length)
            {
                var end = Math.Min(start + maxChunkSize, normalized.Length);

                // Metnin sonuna gelmediysek, kelimeyi/cümleyi ortadan kesmemek için daha iyi bir kesme noktası ara.
                if (end < normalized.Length)
                    end = FindBreakPosition(normalized, start, end);

                var chunk = normalized[start..end].Trim();
                if (chunk.Length > 0)
                    chunks.Add(chunk);

                if (end >= normalized.Length)
                    break;

                // Bir sonraki parça, öncekinin son `overlap` karakterinden başlasın.
                var nextStart = end - overlap;
                start = nextStart > start ? nextStart : end; // her zaman ilerlemeyi garanti et
            }

            return chunks;
        }

        private static int FindBreakPosition(string text, int start, int end)
        {
            // Parçanın en az yarısını doldurmayan bir noktada kesmeyelim (minik parçalar oluşmasın).
            var minBreak = start + (end - start) / 2;

            // 1. Tercih: paragraf sonu
            for (var i = end - 1; i > minBreak; i--)
                if (text[i] == '\n' && text[i - 1] == '\n')
                    return i + 1;

            // 2. Tercih: cümle sonu
            for (var i = end - 1; i > minBreak; i--)
                if ((text[i] == ' ' || text[i] == '\n') && (text[i - 1] is '.' or '!' or '?'))
                    return i + 1;

            // 3. Tercih: herhangi bir boşluk (en azından kelimeyi bölmeyelim)
            for (var i = end - 1; i > minBreak; i--)
                if (char.IsWhiteSpace(text[i]))
                    return i + 1;

            // Hiçbiri yoksa (örn. boşluksuz uzun bir metin) sert kes.
            return end;
        }
    }
}
