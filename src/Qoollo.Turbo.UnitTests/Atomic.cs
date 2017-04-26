using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests
{
    public struct AtomicBool
    {
        private volatile int _value;
        public AtomicBool(bool val)
        {
            _value = val ? 1 : 0;
        }

        public bool Value
        {
            get { return _value != 0; }
            set { Interlocked.Exchange(ref _value, value ? 1 : 0); }
        }
        

        public static explicit operator AtomicBool(bool val)
        {
            return new AtomicBool(val);
        }
        public static implicit operator bool(AtomicBool val)
        {
            return val.Value;
        }
    }

    public struct AtomicNullableBool
    {
        private volatile int _value;
        public AtomicNullableBool(bool? val)
        {
            if (val.HasValue)
                _value = val.Value ? 1 : -1;
            else
                _value = 0;
        }

        public bool? Value
        {
            get
            {
                var val = _value;
                if (val == 0)
                    return null;
                if (val > 0)
                    return true;
                return false;
            }
            set
            {
                if (value.HasValue)
                    Interlocked.Exchange(ref _value, value.Value ? 1 : -1);
                else
                    Interlocked.Exchange(ref _value, 0);
            }
        }

        public bool HasValue { get { return _value != 0; } }

        public bool ValueOrDefault
        {
            get
            {
                var val = _value;
                if (val > 0)
                    return true;
                return false;
            }
        }

        public static explicit operator AtomicNullableBool(bool? val)
        {
            return new AtomicNullableBool(val);
        }
        public static implicit operator bool?(AtomicNullableBool val)
        {
            return val.Value;
        }
    }

    public struct AtomicInt
    {
        private volatile int _value;
        public AtomicInt(int val)
        {
            _value = val;
        }

        public int Value
        {
            get { return _value; }
            set { Interlocked.Exchange(ref _value, value); }
        }

        public int Increment() { return Interlocked.Increment(ref _value); }
        public int Decrement() { return Interlocked.Decrement(ref _value); }


        public static explicit operator AtomicInt(int val)
        {
            return new AtomicInt(val);
        }
        public static implicit operator int(AtomicInt val)
        {
            return val.Value;
        }
    }
}
