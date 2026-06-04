using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TiNRS.Tiler.ViewModels;

public abstract class ViewModelBase : ObservableObject, IDisposable
{
    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
