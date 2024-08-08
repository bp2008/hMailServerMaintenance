using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace hMailServerMaintenance
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static int Main(string[] args)
		{
			try
			{
				if (args.Length != 3)
					return 1;

				// Copy the certificate and key if the files are different.
				FileInfo crt = new FileInfo(args[0]);
				FileInfo key = new FileInfo(args[1]);
				DirectoryInfo hMailServerCertDir = new DirectoryInfo(args[2]);
				if (!crt.Exists)
					return 2;
				if (!key.Exists)
					return 3;
				if (!hMailServerCertDir.Exists)
					return 4;

				string crtTarget = Path.Combine(hMailServerCertDir.FullName, "fullchain.pem");
				string keyTarget = Path.Combine(hMailServerCertDir.FullName, "privkey.pem");

				if (!FileContentEqual(crt.FullName, crtTarget) || !FileContentEqual(key.FullName, keyTarget))
				{
					// Sleep a moment to help ensure the source files are not still being updated.
					Thread.Sleep(1000);

					if (!FileContentEqual(crt.FullName, crtTarget) || !FileContentEqual(key.FullName, keyTarget))
					{
						if (File.Exists(crtTarget))
							File.Delete(crtTarget);
						if (File.Exists(keyTarget))
							File.Delete(keyTarget);

						crt.CopyTo(crtTarget);
						key.CopyTo(keyTarget);

						// Restart hMailServer
						using (ServiceController service = new ServiceController("hMailServer"))
						{
							service.Stop();
							service.WaitForStatus(ServiceControllerStatus.Stopped);
							service.Start();
							service.WaitForStatus(ServiceControllerStatus.Running);
						}
					}
				}

				return 0;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
				File.WriteAllText("hMailServerMaintenance-lastError.txt", DateTime.Now + Environment.NewLine + Environment.NewLine + ex.ToString());
				return 10;
			}
		}

		/// <summary>
		/// Returns true if the content of two files are equal.
		/// </summary>
		/// <param name="pathA">Path to a file.</param>
		/// <param name="pathB">Path to a file.</param>
		/// <returns></returns>
		private static bool FileContentEqual(string pathA, string pathB)
		{
			FileInfo a = new FileInfo(pathA);
			FileInfo b = new FileInfo(pathB);
			if (a.Exists != b.Exists)
				return false;

			if (!a.Exists)
				return true;

			if (a.Length != b.Length)
				return false;

			byte[] bufA = File.ReadAllBytes(pathA);
			byte[] bufB = File.ReadAllBytes(pathB);
			if (bufA.Length != bufB.Length)
				return false;

			for (int i = 0; i < bufA.Length; i++)
			{
				if (bufA[i] != bufB[i])
					return false;
			}
			return true;
		}
	}
}
