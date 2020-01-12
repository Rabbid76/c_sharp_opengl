using System;
using OpenTK_library.Controls;

namespace OpenTK_libray_viewmodel.Model
{
    public interface IModel
        : IDisposable
    {
        IControls GetControls();
        float GetScale();
        void Setup(int cx, int cy);
        void Draw(int cx, int cy, double app_t);
    }
}
