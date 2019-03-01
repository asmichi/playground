using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Lifetime;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LifeTimeService
{
    internal class Program
    {
        static void Main(string[] args)
        {
            AppDomain ad = AppDomain.CreateDomain("OtherDomain");

            while (true)
            {
                using (var sponsor = new MySponsor())
                {
                    for (int i = 0; i < 30; i++)
                    {
#if true
                        var ro = ProxyHolder.Create((RemoteObject)ad.CreateInstanceAndUnwrap(
                            Assembly.GetExecutingAssembly().FullName,
                            typeof(RemoteObject).FullName));
#else
                        var ro = (LeaseManagedRemoteObject)ad.CreateInstanceAndUnwrap(
                            Assembly.GetExecutingAssembly().FullName,
                            typeof(LeaseManagedRemoteObject).FullName);

                        sponsor.Register((ILease)ro.GetLifetimeService());
#endif
                        // Oops, forgot to dispose it!
                        // ro.Dispose();

                        Thread.Sleep(100);
                    }
                }
            }
        }
    }

    internal static class ProxyHolder
    {
        public static ProxyHolder<T> Create<T>(T proxy)
            where T : IDisposable
        {
            return new ProxyHolder<T>(proxy);
        }
    }

    // Disposes the holding proxy object in its finalizer.
    internal class ProxyHolder<T> : IDisposable
        where T : IDisposable
    {
        public T Proxy { get; }

        public ProxyHolder(T proxy)
        {
            this.Proxy = proxy;
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            Proxy.Dispose();
        }

        ~ProxyHolder()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }

    internal class RemoteObject : MarshalByRefObject, IDisposable
    {
        private static int InstanceCount;
        // Add some memory pressure.
        private readonly List<int>[] _bigArray = Enumerable.Range(0, 1024).Select(_ => new List<int>(1024)).ToArray();
        private bool _disposed = false;

        public RemoteObject()
        {
            Interlocked.Increment(ref InstanceCount);
            Debug.WriteLine($"RemoteObject.ctor: {InstanceCount} instances remain.");
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                RemotingServices.Disconnect(this);
                _disposed = true;

                Interlocked.Decrement(ref InstanceCount);
                Debug.WriteLine($"RemoteObject.Dispose: {InstanceCount} instances remain.");
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.Infrastructure)]
        public override object InitializeLifetimeService()
        {
            // Infinite lifetime.
            // This instance will never be GC'ed unless RemotingServices.Disconnect(this) explicitly called.
            return null;
        }
    }

    internal class LeaseManagedRemoteObject : MarshalByRefObject, IDisposable
    {
        private static int InstanceCount;
        // Add some memory pressure.
        private readonly List<int>[] _bigArray = Enumerable.Range(0, 1024).Select(_ => new List<int>(1024)).ToArray();
        private bool _disposed = false;

        public LeaseManagedRemoteObject()
        {
            Interlocked.Increment(ref InstanceCount);
            Debug.WriteLine($"RemoteObject.ctor: {InstanceCount} instances remain.");
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                RemotingServices.Disconnect(this);
                _disposed = true;

                Interlocked.Decrement(ref InstanceCount);
                Debug.WriteLine($"RemoteObject.Dispose: {InstanceCount} instances remain.");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        // Just for logging the instance count.
        ~LeaseManagedRemoteObject()
        {
            Interlocked.Decrement(ref InstanceCount);
            Debug.WriteLine($"RemoteObject.Dispose: {InstanceCount} instances remain.");
        }

        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.Infrastructure)]
        public override object InitializeLifetimeService()
        {
            // This MBRO instance will not be GC'ed until this lease expires because this lease holds a reference to this MBRO instance.
            var lease = (ILease)base.InitializeLifetimeService();

            if (lease.CurrentState == LeaseState.Initial)
            {
                lease.InitialLeaseTime = TimeSpan.FromSeconds(1);
                lease.SponsorshipTimeout = TimeSpan.FromSeconds(30);
                lease.RenewOnCallTime = TimeSpan.FromSeconds(1);
            }

            return lease;
        }
    }

    internal class MySponsor : MarshalByRefObject, IDisposable, ISponsor
    {
        private static int CurrentId;

        private bool _disposed = false;
        private int _id;
        private List<ILease> _registeredLeases = new List<ILease>();

        public MySponsor()
        {
            this._id = Interlocked.Increment(ref CurrentId);
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                RemotingServices.Disconnect(this);
                UnregisterAllLeases();

                _disposed = true;
            }
        }

        ~MySponsor()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.Infrastructure)]
        public override object InitializeLifetimeService()
        {
            return null;
        }

        public TimeSpan Renewal(ILease lease)
        {
            Debug.WriteLine($"MySponsor[{_id}].Renewal");
            return lease.InitialLeaseTime;
        }

        public void Register(ILease lease)
        {
            lease.Register(this);
            lock (_registeredLeases)
            {
                _registeredLeases.Add(lease);
            }
        }

        private void UnregisterAllLeases()
        {
            lock (_registeredLeases)
            {
                foreach (var lease in _registeredLeases)
                {
                    try
                    {
                        lease.Unregister(this);
                    }
                    catch (RemotingException)
                    {
                        // Okay. Already disconnected.
                    }
                }
                _registeredLeases.Clear();
            }
        }
    }
}
