using System;
using System.Collections.Generic;
using System.Text;

namespace MonoWorker.Core.SimpleInstanceService
{
    public class CSVDeserializer
    {
        public static void Deserialize(string message, string Prefix, Queue<Action<string>> fieldParserQueue)
        {
            if (!message.StartsWith(Prefix))
            {
                throw new FormatException($"Unexpected start of message, expected {Prefix}");
            }
            var body = message.Substring(0, Prefix.Length) + ":";
            var sb = new StringBuilder(body.Length);
            var lastChar = ' ';

            foreach (var chr in body)
            {
                if (lastChar != '\\' && chr == ':')
                {
                    var fieldValue = sb.ToString();
                    try
                    {
                        fieldParserQueue.Dequeue()(fieldValue);
                    }
                    catch (Exception e)
                    {
                        throw new FormatException($"Error when parsing field value '{fieldValue}' message prefixed {Prefix}", e);
                    }

                    if (fieldParserQueue.Count == 0)
                    {
                        return;
                    }

                    sb.Clear();
                }
                else
                {
                    sb.Append(chr);
                    lastChar = chr;
                }
            }

            throw new FormatException($"Unexpected end of message prefixed {Prefix}");
        }
        
    }
}
