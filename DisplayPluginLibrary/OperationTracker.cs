using System;
using System.Timers;

namespace DisplayPluginLibrary
{
    /// <summary>
    ///     Provides support for flow control and throttling of operations such as data or redraw requests.
    /// </summary>
    /// <typeparam name="TOperation">Operation data.</typeparam>
    public sealed class OperationTracker<TOperation>
    {
        private readonly TimeSpan interval;
        private readonly Action<TOperation> action;
        private const int FirstOperation = 0;
        private const int NextOperation = 1;
        private readonly object padLock = new object();
        private readonly (TOperation Operation, bool Active, bool Pending)[] operations = new (TOperation, bool, bool)[2];
        private readonly Timer timer = new Timer(50);
        private DateTime lastTimeExecutedUtc = DateTime.UtcNow;

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="interval">Specifies the minimum time between operations being executed.</param>
        /// <param name="action">Action to execute on operation data.</param>
        public OperationTracker(TimeSpan interval, Action<TOperation> action)
        {
            this.interval = interval;
            this.action = action;

            this.timer.Elapsed += this.OnTimer;
        }

        /// <summary>
        ///     Clear any active/pending operations.
        /// </summary>
        public void Abort()
        {
            lock (this.padLock)
            {
                this.operations[FirstOperation] = (default, false, false);
                this.operations[NextOperation] = (default, false, false);
            }
        }

        /// <summary>
        ///     Add operation data (current/pending or replaces pending)
        /// </summary>
        /// <param name="operation">Operation data.</param>
        public void Add(TOperation operation)
        {
            lock (this.padLock)
            {
                if (!this.operations[FirstOperation].Active)
                {
                    this.operations[FirstOperation] = (operation, true, true);
                    this.lastTimeExecutedUtc = DateTime.MinValue;
                }
                else
                {
                    this.operations[NextOperation] = (operation, true, true);
                    this.timer.Start();
                }
            }

            this.TryExecute();
        }

        /// <summary>
        ///     Indicate that the current operation has finished executing.
        /// </summary>
        public void Complete()
        {
            lock (this.padLock)
            {
                this.operations[FirstOperation] = (default, false, false);
                if (this.operations[NextOperation].Active)
                {
                    Swap(ref this.operations[NextOperation], ref this.operations[FirstOperation]);
                }
                else
                {
                    this.timer.Stop();
                }
            }
        }

        /// <summary>
        ///     Get current operation data.
        /// </summary>
        /// <param name="currentOperation">Current operation.</param>
        /// <returns>True if there is a current operation.</returns>
        public bool GetCurrent(out TOperation currentOperation)
        {
            lock (this.padLock)
            {
                if (this.operations[FirstOperation].Active)
                {
                    currentOperation = this.operations[FirstOperation].Operation;
                    return true;
                }

                currentOperation = default;
                return false;
            }
        }

        private void OnTimer(object sender, ElapsedEventArgs e)
        {
            if (DateTime.UtcNow - this.lastTimeExecutedUtc < interval)
            {
                return;
            }

            this.TryExecute();
        }

        private void TryExecute()
        {
            TOperation operation;
            lock (this.padLock)
            {
                if (!this.operations[FirstOperation].Pending)
                {
                    return;
                }

                operation = this.operations[FirstOperation].Operation;
                this.operations[FirstOperation] = (operation, true, false);
            }

            this.action(operation);
            this.lastTimeExecutedUtc = DateTime.UtcNow;
        }

        private static void Swap<TAny>(ref TAny lhs, ref TAny rhs)
        {
            var temp = lhs;
            lhs = rhs;
            rhs = temp;
        }
    }
}