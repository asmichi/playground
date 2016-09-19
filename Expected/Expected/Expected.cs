using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Expected
{
    public static partial class Expected
    {
        public static Unexpected<E> Unexpected<E>(E error)
            => new Unexpected<E>(error);
    }

    public struct Unexpected<E>
    {
        public E Error { get; }

        public Unexpected(E error)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            this.Error = error;
        }
    }

    [DebuggerDisplay("{EffectiveValue}")]
    public struct Expected<T, E>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _hasValue { get; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private T _value { get; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private E _error { get; }

        public Expected(T value)
        {
            this._hasValue = true;
            this._value = value;
            this._error = default(E);
        }

        public Expected(Unexpected<E> unexpected)
        {
            if (unexpected.Error == null)
            {
                throw new ArgumentNullException(nameof(unexpected) + "." + nameof(unexpected.Error));
            }

            this._hasValue = false;
            this._value = default(T);
            this._error = unexpected.Error;
        }

        private Expected(bool hasValue, T value, E error)
        {
            this._hasValue = hasValue;
            this._value = value;
            this._error = error;
        }

        [DebuggerStepThrough()]
        public static implicit operator Expected<T, E>(T value)
            => new Expected<T, E>(value);
        [DebuggerStepThrough()]
        public static implicit operator Expected<T, E>(Unexpected<E> unexpected)
            => new Expected<T, E>(unexpected);
        private object EffectiveValue
            => _hasValue ? (object)_value : (object)_error;

        public bool HasValue => _hasValue;
        public T Value
        {
            get
            {
                Debug.Assert(_hasValue);
                return _value;
            }
        }
        public E Error
        {
            get
            {
                Debug.Assert(!_hasValue);
                return _error;
            }
        }

        public Expected<U, E> Map<U>(Func<T, U> f)
        {
            if (_hasValue)
            {
                return new Expected<U, E>(f(_value));
            }
            else
            {
                return Expected.Unexpected(_error);
            }
        }

        public Expected<U, E> Bind<U>(Func<T, Expected<U, E>> f)
        {
            if (_hasValue)
            {
                return f(_value);
            }
            else
            {
                return Expected.Unexpected(_error);
            }
        }

        public Expected<U, E> Bind<U>(Func<T, U> f)
        {
            if (_hasValue)
            {
                return f(_value);
            }
            else
            {
                return Expected.Unexpected(_error);
            }
        }

        public T ValueOrDefault(Func<T> defaultValue)
        {
            if (_hasValue)
            {
                return _value;
            }
            else
            {
                return defaultValue();
            }
        }

        public Expected<T, E> HandleError(Func<E, Expected<T, E>> f)
        {
            if (_hasValue)
            {
                return this;
            }
            else
            {
                return f(_error);
            }
        }

        public T HandleError(Func<E, T> f)
        {
            if (_hasValue)
            {
                return _value;
            }
            else
            {
                return f(_error);
            }
        }

        public void Match(Action<T> value, Action<E> error)
        {
            if (_hasValue)
            {
                value(_value);
            }
            else
            {
                error(_error);
            }
        }

        public U Match<U>(Func<T, U> value, Func<E, U> error)
        {
            if (_hasValue)
            {
                return value(_value);
            }
            else
            {
                return error(_error);
            }
        }
    }
}
