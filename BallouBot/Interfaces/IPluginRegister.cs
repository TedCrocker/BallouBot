using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Registration;

namespace BallouBot.Interfaces
{
	public interface IPluginRegister
	{
		IList<AssemblyCatalog> Register(RegistrationBuilder builder);
	}
}