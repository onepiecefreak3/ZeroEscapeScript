using System.Text;
using CrossCutting.Core.Contract.EventBrokerage;
using CrossCutting.Core.Contract.Messages;
using CrossCutting.Core.Contract.DependencyInjection;
using Logic.Business.SpikeChunsoftScriptManagement.Contract;
using ZeroEscapeScript;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

KernelLoader loader = new();
ICoCoKernel kernel = loader.Initialize();

var eventBroker = kernel.Get<IEventBroker>();
eventBroker.Raise(new InitializeApplicationMessage());

var mainLogic = kernel.Get <IScriptManagementWorkflow>();
return mainLogic.Execute();
