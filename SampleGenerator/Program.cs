using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QBFC15Lib;
using SessionFramework;
using CommandLine;
using System.Runtime.InteropServices;

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
        [Option('a', "attachdir", HelpText = "Attachment Directory", Required = true)]
        public string AttachDir { get; set; }
    }

    internal class Program
    {
        static int Main(string[] args)
        {
            return CommandLine.Parser.Default.ParseArguments<AuthOptions, GenerateOptions>(args)
                .MapResult(
                    (AuthOptions opts) => AuthorizeApp(opts),
                    (GenerateOptions opts) => GenerateSamples(opts),
                    errs => 1);
        }

        private static int AuthorizeApp(AuthOptions opts)
        {
            if (!System.IO.File.Exists(opts.CompanyFile))
            {
                Console.WriteLine($"Company File does not exist: {opts.CompanyFile}");
                return 1;
            }
            Console.WriteLine("Open Quickbooks Company file as admin in Multi-User Mode and press any key to continue...");
            Console.ReadKey();
            Console.WriteLine($"Connecting to {opts.CompanyFile}");
            using (SessionManager session = SessionManager.getInstance())
            {
                session.openConnection(ENConnectionType.ctLocalQBD);
                string message = "Connection Successful.";
                try
                {
                    session.beginSession(opts.CompanyFile, ENOpenMode.omDontCare);
                } catch (Exception ex)
                {
                    if (ex is COMException exception)
                    {
                        switch (exception.ErrorCode)
                        {
                            case unchecked((int)0x8004040A):
                                message = exception.Message;
                                break;
                            case unchecked((int)0x80040410):
                                message = "Quickbooks is currently open in Single User Mode.  Either close the company file, or re-open it in multi-user mode.";
                                break;
                            case unchecked((int)0x80040414):
                                message = "There is a window open in Quickbooks preventing QBConnector from accessing it.  Please close the window or the company in Quickbooks.";
                                break;
                            case unchecked((int)0x8004041B):
                                message = exception.Message;
                                break;
                            case unchecked((int)0x80040422):
                                message = exception.Message;
                                break;
                            default:
                                message = exception.Message;
                                break;
                        }
                    } else 
                    { 
                        message = ex.Message; 
                    }
                }
                Console.Write(message);
                session.endSession();
                session.closeConnection();
            }
            return 1;
        }

        private static int GenerateSamples(GenerateOptions opts)
        {
            if (!System.IO.File.Exists(opts.CompanyFile))
            {
                Console.WriteLine($"Company File does not exist: {opts.CompanyFile}");
                return 1;
            }
            using (SessionManager session = SessionManager.getInstance())
            {
                session.openConnection(ENConnectionType.ctLocalQBD);
                session.beginSession(opts.CompanyFile, ENOpenMode.omDontCare);

                session.endSession();
                session.closeConnection();
            }
            return 1;
        }
    }
}
