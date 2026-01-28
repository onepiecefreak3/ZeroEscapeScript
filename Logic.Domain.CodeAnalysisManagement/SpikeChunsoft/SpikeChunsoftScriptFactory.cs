using CrossCutting.Core.Contract.DependencyInjection;
using CrossCutting.Core.Contract.DependencyInjection.DataClasses;
using Logic.Domain.CodeAnalysisManagement.Contract;
using Logic.Domain.CodeAnalysisManagement.DataClasses.SpikeChunsoft;

namespace Logic.Domain.CodeAnalysisManagement.SpikeChunsoft;

internal class SpikeChunsoftScriptFactory : ITokenFactory<SpikeChunsoftSyntaxToken>
{
    private readonly ICoCoKernel _kernel;

    public SpikeChunsoftScriptFactory(ICoCoKernel kernel)
    {
        _kernel = kernel;
    }

    public ILexer<SpikeChunsoftSyntaxToken> CreateLexer(string text)
    {
        var buffer = _kernel.Get<IBuffer<int>>(
            new ConstructorParameter("text", text));
        return _kernel.Get<ILexer<SpikeChunsoftSyntaxToken>>(
            new ConstructorParameter("buffer", buffer));
    }

    public IBuffer<SpikeChunsoftSyntaxToken> CreateTokenBuffer(ILexer<SpikeChunsoftSyntaxToken> lexer)
    {
        return _kernel.Get<IBuffer<SpikeChunsoftSyntaxToken>>(new ConstructorParameter("lexer", lexer));
    }
}