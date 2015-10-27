using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.Registration;

namespace BallouBot.Core
{
	public interface IPluginRegister
	{
		IList<AssemblyCatalog> Register(RegistrationBuilder builder);
	}
}