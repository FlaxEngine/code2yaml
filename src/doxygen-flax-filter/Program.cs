// Copyright (c) 2012-2021 Wojciech Figat. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Flax
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Invalid arguments. Usage: doxygen-flax-filter <file>");
                return -1;
            }
            if (!File.Exists(args[0]))
            {
                Console.Error.WriteLine("Missing file " + args[0]);
                return -2;
            }

            var tags = new string[]
            {
                "API_STRUCT",
                "API_CLASS",
                "API_INTERFACE",
                "API_PROPERTY",
                "API_FIELD",
                "API_FUNCTION",
                "API_EVENT",
                "API_PARAM",
                "API_ENUM",
                "API_AUTO_SERIALIZATION",
                "API_INJECT_CPP_CODE",

                "DECLARE_SCRIPTING_TYPE_MINIMAL",
                "DECLARE_SCRIPTING_TYPE",
                "DECLARE_SCRIPTING_TYPE_NO_SPAWN",
                "DECLARE_SCRIPTING_TYPE_WITH_CONSTRUCTOR_IMPL",
                "NON_COPYABLE",
            };
            var defines = new string[]
            {
                "DEPRECATED",
            };

            var contents = File.ReadAllText(args[0]);
            var sb = new StringBuilder();
            int pos = 0;
            while (pos < contents.Length)
            {
                // Filter tags
                for (int i = 0; i < tags.Length; i++)
                {
                    var tag = tags[i];
                    if (tag.Length < contents.Length - pos && string.CompareOrdinal(contents, pos, tag, 0, tag.Length) == 0)
                    {
                        pos += tag.Length;
                        while (pos < contents.Length && contents[pos] != '(')
                            pos++;
                        pos++;
                        int parenthesis = 1;
                        while (pos < contents.Length && parenthesis != 0)
                        {
                            var c = contents[pos];
                            if (c == '(')
                                parenthesis++;
                            else if (c == ')')
                                parenthesis--;
                            else if (c == '\n' || c == '\r')
                                sb.Append(c);
                            pos++;
                        }
                        pos++;
                        break;
                    }
                }

                // Filter defines
                for (int i = 0; i < defines.Length; i++)
                {
                    var define = defines[i];
                    if (define.Length + 2 < contents.Length - pos && char.IsWhiteSpace(contents[pos]) && char.IsWhiteSpace(contents[pos + define.Length + 1]) && string.CompareOrdinal(contents, pos + 1, define, 0, define.Length) == 0)
                    {
                        pos += define.Length + 1;
                        break;
                    }
                }

                // Move forward
                sb.Append(contents[pos++]);
            }
            contents = sb.ToString();

            Console.Write(contents);
            return 0;
        }
    }
}
