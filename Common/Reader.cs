using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Mal;
using MalVal = Mal.Types.MalVal;
using MalSymbol = Mal.Types.MalSymbol;
using MalList = Mal.Types.MalList;
using MalVector = Mal.Types.MalVector;
using MalHashMap = Mal.Types.MalHashMap;
using MalThrowable = Mal.Types.MalThrowable;
using MalContinue = Mal.Types.MalContinue;

namespace Mal
{
    public class Reader
    {
        public class ParseError : MalThrowable
        {
            public ParseError(string msg) : base(msg) { }
        }

        public class Reader_
        {
            List<string> tokens;
            int position;
            public Reader_(List<string> t)
            {
                tokens = t;
                position = 0;
            }

            public string peek()
            {
                if (position >= tokens.Count)
                {
                    return null;
                }
                else
                {
                    return tokens[position];
                }
            }
            public string next()
            {
                return tokens[position++];
            }
        }

        public static List<string> tokenize(string str)
        {
            List<string> tokens = new List<string>();
            /* 断句
            [\s ,]*(~@
                     |[\[\]{}()'`~@]
                     |""(?:[\\].|[^\\""])*""?
                     |;.*
                     |[^\s \[\]{}()'""`~@,;]*
                    )
             */
            string pattern = @"[\s ,]*(~@|[\[\]{}()'`~@]|""(?:[\\].|[^\\""])*""?|;.*|[^\s \[\]{}()'""`~@,;]*)";
            Regex regex = new Regex(pattern);
            Console.WriteLine("str: " + str);
            foreach (Match match in regex.Matches(str))
            {
                string token = match.Groups[1].Value;
                if ((token != null) && !(token == "") && !(token[0] == ';'))
                {
                    Console.WriteLine("match: " + match.Groups[1]);
                    tokens.Add(token);
                }
            }
            return tokens;
        }

        public static MalVal read_atom(Reader_ rdr)
        {
            string token = rdr.next();
            ///
            /// (^-?[0-9]+$) 整数
            /// |(^-?[0-9][0-9.]*$) 
            /// |(^nil$)
            /// |(^true$)
            /// |(^false$)
            /// |(^""(?:[\\].|[^\\""])*""$)
            /// |(^"".*$)
            /// |:(.*)
            /// |(^[^""]*$) 符号
            ///
            string pattern = @"(^-?[0-9]+$)|(^-?[0-9][0-9.]*$)|(^nil$)|(^true$)|(^false$)|(^""(?:[\\].|[^\\""])*""$)|(^"".*$)|:(.*)|(^[^""]*$)";
            Regex regex = new Regex(pattern);
            Match match = regex.Match(token);
            //Console.WriteLine("token: ^" + token + "$");
            if (!match.Success)
            {
                throw new ParseError("unrecognized token '" + token + "'");
            }
            if (match.Groups[1].Value != String.Empty)
            {
                return new Mal.Types.MalInt(int.Parse(match.Groups[1].Value));
            }
            else if (match.Groups[3].Value != String.Empty)
            {
                return Mal.Types.Nil;
            }
            else if (match.Groups[4].Value != String.Empty)
            {
                return Mal.Types.True;
            }
            else if (match.Groups[5].Value != String.Empty)
            {
                return Mal.Types.False;
            }
            else if (match.Groups[6].Value != String.Empty)
            {
                string str = match.Groups[6].Value;
                str = str.Substring(1, str.Length - 2)
                    .Replace("\\\\", "\u029e")
                    .Replace("\\\"", "\"")
                    .Replace("\\n", "\n")
                    .Replace("\u029e", "\\");
                return new Mal.Types.MalString(str);
            }
            else if (match.Groups[7].Value != String.Empty)
            {
                throw new ParseError("expected '\"', got EOF");
            }
            else if (match.Groups[8].Value != String.Empty)
            {
                return new Mal.Types.MalString("\u029e" + match.Groups[8].Value);
            }
            else if (match.Groups[9].Value != String.Empty)
            {
                return new Mal.Types.MalSymbol(match.Groups[9].Value);
            }
            else
            {
                throw new ParseError("unrecognized '" + match.Groups[0] + "'");
            }
        }

        public static MalVal read_list(Reader_ rdr, MalList lst, char start, char end)
        {
            string token = rdr.next();
            if (token[0] != start)
            {
                throw new ParseError("expected '" + start + "'");
            }

            while ((token = rdr.peek()) != null && token[0] != end)
            {
                lst.conj_BANG(read_form(rdr));
            }

            if (token == null)
            {
                throw new ParseError("expected '" + end + "', got EOF");
            }
            rdr.next();

            return lst;
        }

        public static MalVal read_hash_map(Reader_ rdr)
        {
            MalList lst = (MalList)read_list(rdr, new MalList(), '{', '}');
            return new MalHashMap(lst);
        }


        public static MalVal read_form(Reader_ rdr)
        {
            string token = rdr.peek();
            if (token == null) { throw new MalContinue(); }
            MalVal form = null;

            switch (token)
            {
                case "'":
                    rdr.next();
                    return new MalList(new MalSymbol("quote"),
                                       read_form(rdr));
                case "`":
                    rdr.next();
                    return new MalList(new MalSymbol("quasiquote"),
                                       read_form(rdr));
                case "~":
                    rdr.next();
                    return new MalList(new MalSymbol("unquote"),
                                       read_form(rdr));
                case "~@":
                    rdr.next();
                    return new MalList(new MalSymbol("splice-unquote"),
                                       read_form(rdr));
                case "^":
                    rdr.next();
                    MalVal meta = read_form(rdr);
                    return new MalList(new MalSymbol("with-meta"),
                                       read_form(rdr),
                                       meta);
                case "@":
                    rdr.next();
                    return new MalList(new MalSymbol("deref"),
                                       read_form(rdr));

                case "(": form = read_list(rdr, new MalList(), '(', ')'); break;
                case ")": throw new ParseError("unexpected ')'");
                case "[": form = read_list(rdr, new MalVector(), '[', ']'); break;
                case "]": throw new ParseError("unexpected ']'");
                case "{": form = read_hash_map(rdr); break;
                case "}": throw new ParseError("unexpected '}'");
                default: form = read_atom(rdr); break;
            }
            return form;
        }


        public static MalVal read_str(string str)
        {
            return read_form(new Reader_(tokenize(str)));
        }
    }
}
