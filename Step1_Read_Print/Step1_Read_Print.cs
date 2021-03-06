using System;
using System.IO;
using Mal;
using MalVal = Mal.Types.MalVal;

namespace Mal
{
    class step1_read_print
    {
        // read
        static MalVal READ(string str)
        {
            return Reader.read_str(str);
        }

        // eval
        static MalVal EVAL(MalVal ast, string env)
        {
            return ast;
        }

        // print
        static string PRINT(MalVal exp)
        {
            return Printer._pr_str(exp, true);
        }

        // repl
        static void Main(string[] args)
        {
            Func<string, MalVal> RE = (string str) => EVAL(READ(str), "");

            if (args.Length > 0 && args[0] == "--raw")
            {
                Mal.ReadLine.mode = Mal.ReadLine.Mode.Raw;
            }

            // repl loop
            while (true)
            {
                string line;
                try
                {
                    line = Mal.ReadLine.Readline("user> ");
                    if (line == null) { break; }
                    if (line == "") { continue; }
                }
                catch (IOException e)
                {
                    Console.WriteLine("IOException: " + e.Message);
                    break;
                }
                try
                {
                    Console.WriteLine(PRINT(RE(line)));
                }
                catch (Mal.Types.MalContinue)
                {
                    continue;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: " + e.Message);
                    Console.WriteLine(e.StackTrace);
                    continue;
                }
            }
        }
    }
}
