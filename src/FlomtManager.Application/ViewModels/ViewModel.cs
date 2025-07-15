using FlomtManager.Domain.Abstractions.ViewModels;
using ReactiveUI;

namespace FlomtManager.Application.ViewModels;

// todo: implement disposable pattern for all view models
internal abstract class ViewModel : ReactiveObject, IViewModel;
