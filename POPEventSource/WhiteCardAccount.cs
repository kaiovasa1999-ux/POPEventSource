namespace POPEventSource
{
    public class WhiteCardAccount
    {
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        public Guid Id { get; private set; }
        public string AccountHolder { get; private set; } = default!;
        public decimal Balance { get; private set; }
        public string Currency { get; private set; } = "BGN"; //BGN default value
        public bool IsActive { get; private set; }
        public List<Event> Events { get; set; } = [];

        public static async Task<WhiteCardAccount> OpenAsync(
            string accountHolder,
            decimal initialDeposit,
            string currency = "BGN") // може и евро от 2026г :)
        {
            if (string.IsNullOrWhiteSpace(accountHolder))
                throw new ArgumentException("Account holder name is required", nameof(accountHolder));
            if (initialDeposit < 0)
                throw new ArgumentException("Initial deposit must be non-negative", nameof(initialDeposit));

            var acct = new WhiteCardAccount();
            var @event = new AccountOpened(Guid.NewGuid(), accountHolder, initialDeposit, currency);
            await acct.ApplyAsync(@event).ConfigureAwait(false);
            return acct;
        }

        public async Task DepositAsync(decimal amount, string description)
        {
            EnsureAccountIsActive();
            if (amount <= 0) throw new ArgumentException("Deposit amount must be positive", nameof(amount));

            var @event = new MoneyDeposited(Id, amount, description);
            await ApplyAsync(@event).ConfigureAwait(false);
        }

        public async Task WithdrawAsync(decimal amount, string description)
        {
            EnsureAccountIsActive();
            if (amount <= 0) throw new ArgumentException("Withdrawal amount must be positive", nameof(amount));
            if (Balance < amount) throw new InvalidOperationException("Insufficient funds");

            var @event = new MoneyWithdrawn(Id, amount, description);
            await ApplyAsync(@event).ConfigureAwait(false);
        }

        public async Task TransferToAsync(Guid toAccountId, decimal amount, string description)
        {
            EnsureAccountIsActive();
            if (amount <= 0) throw new ArgumentException("Transfer amount must be positive", nameof(amount));
            if (Balance < amount) throw new InvalidOperationException("Insufficient funds");

            var @event = new MoneyTransferred(Id, amount, toAccountId, description);
            await ApplyAsync(@event).ConfigureAwait(false);
        }

        public async Task CloseAsync(string reason)
        {
            EnsureAccountIsActive();
            if (Balance != 0) throw new InvalidOperationException("Cannot close account with non-zero balance");

            var @event = new AccountClosed(Id, reason);
            await ApplyAsync(@event).ConfigureAwait(false);
        }

        public static async Task<WhiteCardAccount> ReplayEventsAsync(IEnumerable<Event> history)
        {
            var acct = new WhiteCardAccount();
            foreach (var @event in history)
            {
                await acct.ApplyAsync(@event).ConfigureAwait(false);
            }
            return acct;
        }
        private async Task ApplyAsync(Event @event)
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                switch (@event)
                {
                    case AccountOpened e:
                        Id = e.AccountId;
                        AccountHolder = e.AccountHolder;
                        Balance = e.InitialDeposit;
                        Currency = e.Currency;
                        IsActive = true;
                        break;
                    case MoneyDeposited e:
                        Balance += e.Amount;
                        break;
                    case MoneyWithdrawn e:
                        Balance -= e.Amount;
                        break;
                    case MoneyTransferred e:
                        Balance -= e.Amount;
                        break;
                    case AccountClosed _:
                        IsActive = false;
                        break;
                }

                Events.Add(@event);
            }
            finally
            {
                _lock.Release();
            }
        }

        private void EnsureAccountIsActive()
        {
            if (!IsActive)
                throw new InvalidOperationException("Account is closed");
        }
    }
}
