using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BallouBot.Logging
{
	public interface ILog
	{
		void Error(Exception e, string template = "", params object[] props);
		void Info(string message, params object[] props);
	}
}
