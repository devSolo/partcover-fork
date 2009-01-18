
namespace PartCover.Browser.Api
{
    public interface IFeature : IService
    {
        void attach(IServiceContainer container);

        void detach(IServiceContainer container);

        void build(IServiceContainer container);

        void destroy(IServiceContainer container);
    }
}
