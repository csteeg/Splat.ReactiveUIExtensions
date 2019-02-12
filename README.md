# Splat.ReactiveUIExtensions
Add constructor injection to splat and add methods to register all views and viewmodels in an assembly
You can install the [nuget package "Splat.ReactiveUIExtensions"](https://www.nuget.org/packages/Splat.ReactiveUIExtensions/) to get started


## Register all your views and viewmodels on startup:
Instead of 
            Locator.CurrentMutable.Register(() => new UpcomingMoviesListView(), typeof(IViewFor<UpcomingMoviesListViewModel>));
            Locator.CurrentMutable.Register(() => new UpcomingMoviesCellView(), typeof(IViewFor<UpcomingMoviesCellViewModel>));
            Locator.CurrentMutable.Register(() => new MovieDetailView(), typeof(IViewFor<MovieDetailViewModel>));
            
You can now do
Locator.CurrentMutable.RegisterViewsAndViewModels<ViewModelBase>(typeof(AppBootstrapper).Assembly);

The first parameter is the assembly where your views and viewmodels reside, the generic parameter is the base class for your viewmodels.
The given assembly will be scanned for viewmodels by looking if there are types defined that are are derived from the generic parameter TBaseViewModelType. 
The views are registered by checking if there are types implementing IViewFor by default. There is also an overload for RegisterViewsAndViewModels where you can pass in the 
base class for views.

There are also methods to register views (RegisterViews, duh) and register viewmodels (hmm, RegisterViewModels) if you wish to have some more control from your code in how things are scanned and registered.

## Constructor injection
If you use the extension methods in this library to register your classes in splat, it will use dependency injection by default for
the class constructors. 
These methods are:
* RegisterLazySingleton
* RegisterType

The singletons are always lazy when registering them with dependency injection enabled, since it needs all dependencies to be registered to be able to construct an instance of it.
