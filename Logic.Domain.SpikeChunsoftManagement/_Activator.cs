using CrossCutting.Core.Contract.Bootstrapping;
using CrossCutting.Core.Contract.Configuration;
using CrossCutting.Core.Contract.DependencyInjection;
using CrossCutting.Core.Contract.DependencyInjection.DataClasses;
using CrossCutting.Core.Contract.EventBrokerage;
using Logic.Domain.SpikeChunsoftManagement.Contract.Script;
using Logic.Domain.SpikeChunsoftManagement.Script;

namespace Logic.Domain.SpikeChunsoftManagement;

public class SpikeChunsoftManagementActivator : IComponentActivator
{
    public void Activating()
    {
    }

    public void Activated()
    {
    }

    public void Deactivating()
    {
    }

    public void Deactivated()
    {
    }

    public void Register(ICoCoKernel kernel)
    {
        kernel.Register<IFsbReader, FsbReader>(ActivationScope.Unique);
        kernel.Register<IFsbParser, FsbParser>(ActivationScope.Unique);
        kernel.Register<IFsbComposer, FsbComposer>(ActivationScope.Unique);
        kernel.Register<IFsbWriter, FsbWriter>(ActivationScope.Unique);

        kernel.RegisterConfiguration<SpikeChunsoftManagementConfiguration>();
    }

    public void AddMessageSubscriptions(IEventBroker broker)
    {
    }

    public void Configure(IConfigurator configurator)
    {
    }
}