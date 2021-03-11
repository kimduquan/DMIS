using System;
using System.ServiceModel;


namespace FileService
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceHost host = null;
            try
            {
                host = new ServiceHost(typeof(FileServiceLibrary.FileService));
                host.Faulted += host_Faulted;
                host.Open();
                FileServiceLibrary.FileService.InitializeSettingValues();
                Console.WriteLine("\nPress any key to stop the service.");
                Console.ReadKey();
            }
            finally
            {
                if (host.State == CommunicationState.Faulted)
                {
                    host.Abort();
                }
                else
                {
                    host.Close();
                }
            }     
        }

        static void host_Faulted(object sender, EventArgs e)
        {
            Console.WriteLine("The File Service host has faulted.");
        }
    }
}
