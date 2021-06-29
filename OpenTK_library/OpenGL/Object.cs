using System;

namespace OpenTK_library.OpenGL
{
    public interface IObject
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
