using System;
using System.ComponentModel;
using System.IO;

namespace Utilities
{
    public enum ErrorCode
    {
        [Description("Connect to QB: Fail")]
        ConnectQBFailed,
        [Description("Connect to QB: Success")]
        ConnectQBOK,
        [Description("Connect Success")]
        ConnectedOK,
        [Description("No Connection")]
        NoConnection,
        [Description("Download Success")]
        DownloadOK,
        [Description("Save to QB: Fail")]
        SaveToQBError,
        [Description("Save to QB: Success")]
        SavetoQBOK,
        [Description("True")]
        True,
        [Description("False")]
        False
    };

    public class Status
    {
        public string Message { get; set; }
        public ErrorCode Code { get; set; }
        public int Progress { get; set; }
        public Status()
        {
            Message = string.Empty;
            Code = ErrorCode.ConnectedOK;
            Progress = 0;
        }
        public Status(string message, ErrorCode code, int progress)
        {
            Message = message;
            Code = code;
            Progress = progress;
        }

    }

    public class Status<T> : Status
    {
        public Status(string message, ErrorCode code, int progress, T returnObject)
        {
            Message = message;
            Code = code;
            Progress = progress;
            ReturnObject = returnObject;
        }
        public T ReturnObject { get; set; }
    }

    public static class StatusExtensions
    {
        public static string GetFormattedMessage(this Status status)
        {
            if (status.Message.Length > 65)
            {
                StringReader reader = new StringReader(status.Message.Replace("\n", ""));
                StringWriter writer = new StringWriter();
                int wordcount = 0;
                int readchar;
                int sentencelen = 0;
                do
                {
                    readchar = reader.Read();
                    if (readchar != -1)
                    {
                        sentencelen++;
                        char thecharacter = Convert.ToChar(readchar);
                        writer.Write(thecharacter);
                        if (thecharacter == ' ')
                        {
                            wordcount++;
                            if (sentencelen >= 65)
                            {
                                writer.Write('\n');
                                sentencelen = 0;
                            }
                        }
                    }
                    else
                        break;

                } while (true);
                return writer.ToString();
            }
            else
                return status.Message;
        }

        public static string GetProgressMessage(this Status status)
        {
            return String.Format("{0} - {1}%", status.Message, status.Progress);
        }
    }

}
