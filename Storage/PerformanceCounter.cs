using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Diagnostics;

namespace ContentRepository
{
    internal class PerformanceCounter
    {
        //============================================================================== Members

        private System.Diagnostics.PerformanceCounter _counter;

        //============================================================================== Constructors

        internal PerformanceCounter(System.Diagnostics.PerformanceCounter counter)
        {
            if (counter == null)
                throw new ArgumentNullException("counter");

            _counter = counter;
            _counter.ReadOnly = false;
            Accessible = true;
        }

        //============================================================================== Properties

        internal bool Accessible { get; private set; }

        internal string CounterName
        {
            get { return _counter.CounterName; }
        }

        //============================================================================== Performance counter API

        internal bool Increment()
        {
            if (!this.Accessible)
                return false;

            try
            {
                _counter.Increment();
            }
            catch (Exception ex)
            {
                LogException(ex);
                return false;
            }

            return true;
        }

        internal bool IncrementBy(long value)
        {
            if (!this.Accessible)
                return false;

            try
            {
                _counter.IncrementBy(value);
            }
            catch (Exception ex)
            {
                LogException(ex);
                return false;
            }

            return true;
        }

        internal bool Decrement()
        {
            if (!this.Accessible)
                return false;

            try
            {
                _counter.Decrement();
            }
            catch (Exception ex)
            {
                LogException(ex);
                return false;
            }

            return true;
        }

        internal bool SetRawValue(long value)
        {
            if (!this.Accessible)
                return false;

            try
            {
                _counter.RawValue = value;
            }
            catch (Exception ex)
            {
                LogException(ex);
                return false;
            }

            return true;
        }

        internal bool Reset()
        {
            return this.SetRawValue(0);
        }

        //============================================================================== Helper methods

        private void LogException(Exception ex)
        {
            if (!this.Accessible) 
                return;

            lock (_counter)
            {
                if (!this.Accessible) 
                    return;

                this.Accessible = false;
                Logger.WriteException(ex);
            }
        }
    }
}
