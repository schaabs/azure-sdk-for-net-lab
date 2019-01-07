using System.ComponentModel;
using System.Diagnostics;

namespace Azure.Core.Diagnostics
{

    // TODO (pri 1): do we really want a new abstraction? Maybe we can use TraceSource?
    public abstract class ServiceLogger
    {
        public abstract bool IsEnabledFor(TraceLevel level);
        public abstract void Log(string message, TraceLevel level = TraceLevel.Info);

        public virtual void Log(object value, TraceLevel level = TraceLevel.Info)
            => Log(value.ToString(), level);

        public static readonly ServiceLogger NullLogger = new NullLogger();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => base.Equals(obj);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => base.GetHashCode();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString() => base.ToString();
    }
    class NullLogger : ServiceLogger
    {
        public override bool IsEnabledFor(TraceLevel level)
            => false;

        public override void Log(string message, TraceLevel level = TraceLevel.Info) { }
    }
}
