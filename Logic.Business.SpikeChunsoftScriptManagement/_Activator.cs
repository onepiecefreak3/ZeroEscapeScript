using CrossCutting.Core.Contract.Bootstrapping;
using CrossCutting.Core.Contract.Configuration;
using CrossCutting.Core.Contract.DependencyInjection;
using CrossCutting.Core.Contract.DependencyInjection.DataClasses;
using CrossCutting.Core.Contract.EventBrokerage;
using Logic.Business.SpikeChunsoftScriptManagement.Contract;
using Logic.Business.SpikeChunsoftScriptManagement.Conversion;
using Logic.Business.SpikeChunsoftScriptManagement.Creation;
using Logic.Business.SpikeChunsoftScriptManagement.Extraction;
using Logic.Business.SpikeChunsoftScriptManagement.InternalContract;
using Logic.Business.SpikeChunsoftScriptManagement.InternalContract.Conversion;
using Logic.Business.SpikeChunsoftScriptManagement.InternalContract.Creation;
using Logic.Business.SpikeChunsoftScriptManagement.InternalContract.Extraction;

namespace Logic.Business.SpikeChunsoftScriptManagement;

public class SpikeChunsoftScriptManagementActivator : IComponentActivator
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
        kernel.Register<IScriptManagementWorkflow, ScriptManagementWorkflow>(ActivationScope.Unique);

        kernel.Register<IExtractWorkflow, ExtractWorkflow>(ActivationScope.Unique);
        kernel.Register<IExtractFsbWorkflow, ExtractFsbWorkflow>(ActivationScope.Unique);

        kernel.Register<ICreateWorkflow, CreateWorkflow>(ActivationScope.Unique);
        kernel.Register<ICreateFsbWorkflow, CreateFsbWorkflow>(ActivationScope.Unique);

        kernel.Register<IFsbScriptFileConverter, FsbScriptFileConverter>(ActivationScope.Unique);
        kernel.Register<IFsbCodeUnitConverter, FsbCodeUnitConverter>(ActivationScope.Unique);
        kernel.Register<IBlockBuilder, BlockBuilder>(ActivationScope.Unique);

        kernel.Register<ISpikeChunsoftScriptManagementConfigurationValidator, SpikeChunsoftScriptManagementConfigurationValidator>(ActivationScope.Unique);

        kernel.RegisterConfiguration<SpikeChunsoftScriptManagementConfiguration>();
    }

    public void AddMessageSubscriptions(IEventBroker broker)
    {
    }

    public void Configure(IConfigurator configurator)
    {
    }
}