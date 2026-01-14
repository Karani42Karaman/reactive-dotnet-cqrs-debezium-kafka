

namespace Payment.ReadConsumer.Infrastructure.Retry
{
    public static class RetryPolicy
    {
        public static async Task ExecuteAsync(
            Func<Task> action,
            int maxRetry = 3,
            int baseDelayMs = 500)
        {
            var attempt = 0;

            while (true)
            {
                try
                {
                    await action();
                    return;
                }
                catch
                {
                    attempt++;
                    if (attempt >= maxRetry)
                        throw;

                    var delay = baseDelayMs * (int)Math.Pow(2, attempt);
                    await Task.Delay(delay);
                }
            }
        }
    }
}
