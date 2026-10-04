namespace DocMind.Api.Warmup
{
    public class WarmupState
    {
        private volatile bool _isReady;

        public bool IsReady => _isReady;

        public void MarkReady() => _isReady = true;
    }
}
