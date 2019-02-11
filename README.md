# Splat.ReactiveUIExtensions
Add constructor injection to splat and add methods to register all views and viewmodels in an assembly

##Register all your views and viewmodels on startup:
Instead of 
            Locator.CurrentMutable.Register(() => new UpcomingMoviesListView(), typeof(IViewFor<UpcomingMoviesListViewModel>));
            Locator.CurrentMutable.Register(() => new UpcomingMoviesCellView(), typeof(IViewFor<UpcomingMoviesCellViewModel>));
            Locator.CurrentMutable.Register(() => new MovieDetailView(), typeof(IViewFor<MovieDetailViewModel>));
            
You can now do
Locator.CurrentMutable.RegisterViewsAndViewModels(typeof(AppBootstrapper).Assembly, typeof(ViewModelBase));

The first parameter is the assembly where your views and viewmodels reside, the second one is the base class for your viewmodels.
The viewmodels are detected by looking if types are derived from the second parameter. The views are detected by 
checking if they implement IViewFor by default. There is also a generic version of RegisterViewsAndViewModels where you can pass in the 
base class for views.

##Constructor injection
If you use the extension methods in this library to register your classes in splat, it will use dependency injection by default for
the class constructors. 
These methods are:
* RegisterLazySingleton
* RegisterType

The singletons are always lazy when registering them with dependency injection enabled, since it needs all dependencies to be registered to be able to construct an instance of it.