using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTK_library.OpenGL
{
    interface IObject
        : IDisposable
    {}

    public abstract class Object
        : IObject
    {
        private bool _disposed = false;

        protected abstract void DisposeObjects();

        ~Object()
        {
            DisposeObjects();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                DisposeObjects();
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
