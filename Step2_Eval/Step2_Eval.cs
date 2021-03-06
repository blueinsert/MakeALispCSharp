using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Mal;
using MalVal = Mal.Types.MalVal;
using MalSymbol = Mal.Types.MalSymbol;
using MalInt = Mal.Types.MalInt;
using MalList = Mal.Types.MalList;
using MalVector = Mal.Types.MalVector;
using MalHashMap = Mal.Types.MalHashMap;
using MalFunc = Mal.Types.MalFunc;

namespace Mal
{
    class step2_eval
    {
        // read
        static MalVal READ(string str)
        {
            return Reader.read_str(str);
        }

        // eval
        static MalVal eval_ast(MalVal ast, Dictionary<string, MalVal> env)
        {
            if (ast is MalSymbol)
            {
                MalSymbol sym = (MalSymbol)ast;
                return (MalVal)env[sym.getName()];
            }
            else if (ast is MalList)
            {
                MalList old_lst = (MalList)ast;
                MalList new_lst = ast.ListQ() ? new MalList()
                                            : (MalList)new MalVector();
                foreach (MalVal mv in old_lst.getValue())
                {
                    new_lst.conj_BANG(EVAL(mv, env));
                }
                return new_lst;
            }
            else if (ast is MalHashMap)
            {
                var new_dict = new Dictionary<string, MalVal>();
                foreach (var entry in ((MalHashMap)ast).getValue())
                {
                    new_dict.Add(entry.Key, EVAL((MalVal)entry.Value, env));
                }
                return new MalHashMap(new_dict);
            }
            else
            {
                return ast;
            }
        }


        static MalVal EVAL(MalVal orig_ast, Dictionary<string, MalVal> env)
        {
            MalVal a0;
            //Console.WriteLine("EVAL: " + printer._pr_str(orig_ast, true));
            if (!orig_ast.ListQ())
            {
                return eval_ast(orig_ast, env);
            }

            // apply list
            MalList ast = (MalList)orig_ast;
            if (ast.size() == 0) { return ast; }
            a0 = ast[0];
            if (!(a0 is MalSymbol))
            {
                throw new Mal.Types.MalError("attempt to apply on non-symbol '"
                        + Mal.Printer._pr_str(a0, true) + "'");
            }
            var el = (MalList)eval_ast(ast, env);
            var f = (MalFunc)el[0];
            return f.apply(el.rest());

        }

        // print
        static string PRINT(MalVal exp)
        {
            return Printer._pr_str(exp, true);
        }

        // repl
        static void Main(string[] args)
        {
            var repl_env = new Dictionary<string, MalVal> {
                {"+", new MalFunc(a => (MalInt)a[0] + (MalInt)a[1]) },
                {"-", new MalFunc(a => (MalInt)a[0] - (MalInt)a[1]) },
                {"*", new MalFunc(a => (MalInt)a[0] * (MalInt)a[1]) },
                {"/", new MalFunc(a => (MalInt)a[0] / (MalInt)a[1]) },
            };
            Func<string, MalVal> RE = (string str) => EVAL(READ(str), repl_env);

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
