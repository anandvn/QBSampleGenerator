using CommandLine;
using log4net.Repository.Hierarchy;
using SampleGenerator.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Utilities;

namespace SampleGenerator
{
    internal class Options
    {
        public Options() { }
        [Option('c', "companyfile", Required = true, HelpText = "Set Company file to access")]
        public string CompanyFile { get; set; }
    }

    [Verb("authorize", HelpText = "Open Company File to authorize connection")]
    internal class AuthOptions : Options
    {
        public AuthOptions() { }
    }

    [Verb("generate", HelpText = "Generate Samples")]
    internal class GenerateOptions : Options
    {
        public GenerateOptions() { }
        [Option('o', "output", HelpText = "Output CSV File", Required = true)]
        public string Output { get; set; }
        [Option('d', "start", HelpText = "Start Date", Required = true)]
        public DateTime Start { get; set; }
    }

    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            return await CommandLine.Parser.Default.ParseArguments<AuthOptions, GenerateOptions>(args)
                .MapResult(
                    (AuthOptions opts) => AuthorizeApp(opts),
                    (GenerateOptions opts) => GenerateSamples(opts),
                    errs => Task.FromResult(-1));
        }

        private static async Task<int> AuthorizeApp(AuthOptions opts)
        {
            if (!System.IO.File.Exists(opts.CompanyFile))
            {
                Console.WriteLine($"Company File does not exist: {opts.CompanyFile}");
                return 1;
            }
            Console.WriteLine("Open Quickbooks Company file as admin in Multi-User Mode and press any key to continue...");
            Console.ReadKey();
            Console.WriteLine($"Connecting to {opts.CompanyFile}");
            Status status;
            using(QBSDKWrapper qbconnector = new QBSDKWrapper())
            {
                status = await qbconnector.ConnectAsync(opts.CompanyFile);
                qbconnector.Disconnect();
            }
            return status?.Code == ErrorCode.ConnectQBOK ? 0 : 1;
        }

        private static async Task<int> GenerateSamples(GenerateOptions opts)
        {
            double counter = 0;
            if (!System.IO.File.Exists(opts.CompanyFile))
            {
                Console.WriteLine($"Company File does not exist: {opts.CompanyFile}");
                return -1;
            }
            if (opts.Output == string.Empty)
            {
                Console.WriteLine("Specify an output file");
                return -1; 
            }
            if (opts.Start == null)
            {
                Console.WriteLine("Invalid Start Date");
                return -1;
            }
            using (QBSDKWrapper qbconnector = new QBSDKWrapper())
            using (StreamWriter sw = new StreamWriter(opts.Output))
            {
                Status status = await qbconnector.ConnectAsync(opts.CompanyFile);
                Console.WriteLine(status.GetFormattedMessage());
                if (status.Code != ErrorCode.ConnectQBOK) 
                    return -1;

                ICollection<InventoryTransfer> batch;
                using (ProgressBar pbar = new ProgressBar())
                {
                    while ((batch = await qbconnector.GetBillsAsync(opts.Start, 100)) != null)
                    {
                        foreach (InventoryTransfer item in batch)
                        {
                            counter++;
                            sw.WriteLine($"{item.Date.ToShortDateString()},{item.ReferenceNum},{item.DueDate.ToShortDateString()},{item.Items.Sum(x => x.Quantity * x.Price) + item.Expenses.Sum(x => x.Amount)}");
                            pbar.Report(counter / (double)qbconnector.ItemCount);
                        }
                    }
                }
                qbconnector.Disconnect();
            }
            return 0;
        }

    }
}
