using System.Runtime.CompilerServices;
using uchat.ViewModels.Tools;

namespace uchat.ViewModels.VMEntities
{
    /// <summary>
    /// Provides a base view model that wraps a model object and exposes property change notification functionality for
    /// derived types.
    /// </summary>
    /// <remarks>This class is intended to be used as a base for view models that encapsulate a model object
    /// and require property change notifications, typically in MVVM scenarios. The wrapped model is exposed via the
    /// <see cref="Model"/> property and cannot be null.</remarks>
    /// <typeparam name="TModel">The type of the model object to be wrapped. Must be a reference type.</typeparam>
    public abstract class WrapperViewModel<TModel> : ObservableObject where TModel : class
    {
        public TModel Model { get; }

        public WrapperViewModel(TModel model)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
        }

        protected bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
