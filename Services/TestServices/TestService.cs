namespace WebApiAlmacen.Services.TestServices
{
    public class TransientService
    {
        public Guid Guid = Guid.NewGuid();
        public Guid GetGuid()
        {
            return Guid;
        }
    }

    public class ScopedService
    {
        public Guid Guid = Guid.NewGuid();
        public Guid GetGuid()
        {
            return Guid;
        }
    }

    public class SingletonService
    {
        public Guid Guid = Guid.NewGuid();
        public Guid GetGuid()
        {
            return Guid;
        }
    }

    public class TestService
    {
        private readonly TransientService transientService;
        private readonly ScopedService scopedService;
        private readonly SingletonService singletonService;

        public TestService(TransientService transientService,
            ScopedService scopedService, SingletonService singletonService)
        {
            this.transientService = transientService;
            this.scopedService = scopedService;
            this.singletonService = singletonService;
        }

        public Guid GetTransient() { return transientService.Guid; }
        public Guid GetScoped() { return scopedService.Guid; }
        public Guid GetSingleton() { return singletonService.Guid; }
    }

}
