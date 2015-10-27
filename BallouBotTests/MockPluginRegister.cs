using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.Registration;
using BallouBot.Core;
using BallouBot.Data;
using BallouBotTests.Mocks;

namespace BallouBotTests
{
	public class MockPluginRegister : IPluginRegister
	{
		public IList<AssemblyCatalog> Register(RegistrationBuilder builder)
		{
			builder.ForType<MockDataSource>()
				.Export<IDataSource>()
				.SetCreationPolicy(CreationPolicy.Shared);
			return new List<AssemblyCatalog>() { new AssemblyCatalog(GetType().Assembly, builder) };
		}
	}
}