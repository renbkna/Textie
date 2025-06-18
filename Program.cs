using System;
using System.Threading.Tasks;
using Textie.Core;

namespace Textie
{
    static class Program
    {
        [STAThread]
        static async Task Main(string[] args)
        {
            try
            {
                var app = new TextieApplication();
                await app.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
                Environment.Exit(1);
            }
        }
    }
}
