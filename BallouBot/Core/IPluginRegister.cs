using System.ComponentModel.Composition.Registration;

namespace BallouBot.Core
{
	public interface IPluginRegister
	{
		void Register(RegistrationBuilder builder);
	}
}