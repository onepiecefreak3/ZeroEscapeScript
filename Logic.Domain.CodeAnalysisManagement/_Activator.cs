using CrossCutting.Core.Contract.Bootstrapping;
using CrossCutting.Core.Contract.Configuration;
using CrossCutting.Core.Contract.DependencyInjection;
using CrossCutting.Core.Contract.DependencyInjection.DataClasses;
using CrossCutting.Core.Contract.EventBrokerage;
using Logic.Domain.CodeAnalysisManagement.Contract;
using Logic.Domain.CodeAnalysisManagement.Contract.SpikeChunsoft;
using Logic.Domain.CodeAnalysisManagement.DataClasses.SpikeChunsoft;
using Logic.Domain.CodeAnalysisManagement.SpikeChunsoft;

namespace Logic.Domain.CodeAnalysisManagement;

public class CodeAnalysisActivator : IComponentActivator
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
        kernel.Register<ITokenFactory<SpikeChunsoftSyntaxToken>, SpikeChunsoftScriptFactory>(ActivationScope.Unique);
        kernel.Register<ILexer<SpikeChunsoftSyntaxToken>, SpikeChunsoftScriptLexer>();
        kernel.Register<IBuffer<SpikeChunsoftSyntaxToken>, TokenBuffer<SpikeChunsoftSyntaxToken>>();
        kernel.Register<IBuffer<int>, StringBuffer>();

        kernel.Register<ISpikeChunsoftScriptParser, SpikeChunsoftScriptParser>(ActivationScope.Unique);
        kernel.Register<ISpikeChunsoftScriptComposer, SpikeChunsoftScriptComposer>(ActivationScope.Unique);
        kernel.Register<ISpikeChunsoftScriptWhitespaceNormalizer, SpikeChunsoftScriptWhitespaceNormalizer>(ActivationScope.Unique);

        kernel.Register<ISpikeChunsoftSyntaxFactory, SpikeChunsoftSyntaxFactory>();

        kernel.Register<ITreeWalker, SpikeChunsoftTreeWalker>(ActivationScope.Unique);

        kernel.RegisterConfiguration<CodeAnalysisConfiguration>();
    }

    public void AddMessageSubscriptions(IEventBroker broker)
    {
    }

    public void Configure(IConfigurator configurator)
    {
    }
}