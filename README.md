# LogicBuilder.DataComponents

LogicBuilder.DataComponents is archived and is no longer being maintained. Please find the composite libraries at their new locations below:
- [LogicBuilder.Data](https://github.com/BpsLogicBuilder/LogicBuilder.Data)
- [LogicBuilder.Domain](https://github.com/BpsLogicBuilder/LogicBuilder.Domain)
- [LogicBuilder.EntityFrameworkCore.SqlServer](https://github.com/BpsLogicBuilder/LogicBuilder.EntityFrameworkCore.SqlServer)
- [LogicBuilder.Expressions.Utils](https://github.com/BpsLogicBuilder/LogicBuilder.Expressions.Utils)
- [LogicBuilder.Kendo.ExpressionExtensions](https://github.com/BpsLogicBuilder/LogicBuilder.Kendo.ExpressionExtensions)
- [LogicBuilder.Structures](https://github.com/BpsLogicBuilder/LogicBuilder.Structures)

LogicBuilder.Expressions.EntityFrameworkCore is no longer being maintained.

Packages in this repository include abstractions for mapping LINQ expresions and objects between business and data objects types.

## LogicBuilder.EntityFrameworkCore.SqlServer
* Maps business model objects to and from data objects.
* Maps query expressions from business model to data model.

To use:
```c#
    //Create a context
    public class SchoolContext : DbContext
    {
        public SchoolContext(DbContextOptions<SchoolContext> options) : base(options)
        {
        }
    }

    //Create Store
    public interface ISchoolStore : IStore
    {
    }
    public class SchoolStore : StoreBase, ISchoolStore
    {
        public SchoolStore(SchoolContext context)
            : base(context)
        {
        }
    }

    //Create Repository
    public interface ISchoolRepository : IContextRepository
    {
    }
    public class SchoolRepository : ContextRepositoryBase, ISchoolRepository
    {
        public SchoolRepository(ISchoolStore store, IMapper mapper) : base(store, mapper)
        {
        }
    }

    //Register Services including AutoMapper profiles.
    IServiceProvider serviceProvider = new ServiceCollection()
                .AddDbContext<SchoolContext>
                (
                    options =>
                    {
                        options.UseInMemoryDatabase("ContosoUniVersity");
                        options.UseInternalServiceProvider(new ServiceCollection().AddEntityFrameworkInMemoryDatabase().BuildServiceProvider());
                    }
                )
                .AddTransient<ISchoolStore, SchoolStore>()
                .AddTransient<ISchoolRepository, SchoolRepository>()
                .AddSingleton<AutoMapper.IConfigurationProvider>(new MapperConfiguration(cfg => cfg.AddProfiles(typeof(SchoolProfile).GetTypeInfo().Assembly)))
                .AddTransient<IMapper>(sp => new Mapper(sp.GetRequiredService<AutoMapper.IConfigurationProvider>(), sp.GetService))
                .BuildServiceProvider();

    //Call the repository
    ISchoolRepository repository = serviceProvider.GetRequiredService<ISchoolRepository>();
    ICollection<StudentModel> list = Task.Run(() => repository.GetItemsAsync<StudentModel, Student>()).Result;
```

## Build LINQ Expressions Dynamically
Create complex expressions from single descriptor objects.

To use:
```c#
        public void BuildFilter()
        {
            IConfigurationProvider config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<ExpressionOperatorsMappingProfile>();
            });
            IMapper mapper = config.CreateMapper();

            Expression<Func<Product, bool>> filter = GetFilterExpression<Product>
            (
                //$it => $it.AlternateAddresses.Any(address => (address.City == "Redmond"))
                new FilterLambdaDescriptor
                (
                    new AnyDescriptor
                    (
                        new MemberSelectorDescriptor("AlternateAddresses", new ParameterDescriptor("$it")),
                        new EqualsBinaryDescriptor
                        (
                            new MemberSelectorDescriptor("City", new ParameterDescriptor("address")),
                            new ConstantDescriptor("Redmond")
                        ),
                        "address"
                    ),
                    typeof(Product),
                    "$it"
                )
            );

            Expression<Func<T, bool>> GetFilterExpression<T>(FilterLambdaDescriptor descriptor)
                => (Expression<Func<T, bool>>)mapper.Map<FilterLambdaOperator>
                (
                    descriptor,
                    opts => opts.Items["parameters"] = new Dictionary<string, ParameterExpression>()
                ).Build();
        }

        public void BuildSelector()
        {
            IConfigurationProvider config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<ExpressionOperatorsMappingProfile>();
            });
            IMapper mapper = config.CreateMapper();

            Expression<Func<IQueryable<Category>, Category>> selector = GetSelectorExpression<IQueryable<Category>, Category>
            (
                //$it => $it.FirstOrDefault(a => (a.CategoryID == -1))
                new SelectorLambdaDescriptor
                (
                    new FirstOrDefaultDescriptor
                    (
                        new ParameterDescriptor("$it"),
                        new EqualsBinaryDescriptor
                        (
                            new MemberSelectorDescriptor("CategoryID", new ParameterDescriptor("a")),
                            new ConstantDescriptor(-1)
                        ),
                        "a"
                    ),
                    typeof(IQueryable<Category>),
                    typeof(Category),
                    "$it"
                )
            );
            
            Expression<Func<T, TResult>> GetSelectorExpression<T, TResult>(SelectorLambdaDescriptor descriptor)
                => (Expression<Func<T, TResult>>)mapper.Map<SelectorLambdaOperator>
                (
                    descriptor,
                    opts => opts.Items["parameters"] = new Dictionary<string, ParameterExpression>()
                ).Build();
        }
```

`ExpressionOperatorsMappingProfile` is a mapping profile from `LogicBuilder.EntityFrameworkCore.SqlServer`.

For serializable descriptors, replace `cfg.AddProfile<ExpressionOperatorsMappingProfile>();` with a [custom mapping profile](https://github.com/BlaiseD/LogicBuilder.Samples/blob/master/Xamarin/Contoso/Contoso.AutoMapperProfiles/DescriptorToOperatorMappingProfile.cs) with serializable descriptors.