using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.Registration;
using System.Reflection;
using BallouBot.Interfaces;

namespace BallouBot.Core
{
	public static class PluginStore
	{
		public static CompositionContainer Container;

		public static void InitializePluginStore()
		{
			InitializePluginStoreNew();
		}

		public static CompositionContainer InitializePluginStoreNew(Func<RegistrationBuilder, ComposablePartCatalog> addAssemblies = null)
		{
			var setupBuilder = new RegistrationBuilder();
			AggregateCatalog setupCatalogs = new AggregateCatalog();
			if (addAssemblies == null)
			{
				setupCatalogs.Catalogs.Add(new DirectoryCatalog(".", setupBuilder));
				setupCatalogs.Catalogs.Add(new AssemblyCatalog(Assembly.GetExecutingAssembly(), setupBuilder));
			}
			else
			{
				setupCatalogs.Catalogs.Add(addAssemblies(setupBuilder));
			}
			
			setupBuilder.ForTypesDerivedFrom<IPluginRegister>()
				.Export<IPluginRegister>()
				.SelectConstructor(cinfo => cinfo[0]);


			var setupContainer = new CompositionContainer(setupCatalogs);

			var aggregateCatalog = new AggregateCatalog();
			var allCatalogs = new Dictionary<string, AssemblyCatalog>();
			var mainBuilder= new RegistrationBuilder();
			foreach (var register in setupContainer.GetExports<IPluginRegister>())
			{
				foreach (var catalog in register.Value.Register(mainBuilder))
				{
					if (!allCatalogs.ContainsKey(catalog.Assembly.FullName))
					{
						allCatalogs.Add(catalog.Assembly.FullName, catalog);
					}
				}
			}

			foreach (var cat in allCatalogs)
			{
				aggregateCatalog.Catalogs.Add(cat.Value);
			}

			Container = new CompositionContainer(aggregateCatalog);

			return setupContainer;
		}
}
}